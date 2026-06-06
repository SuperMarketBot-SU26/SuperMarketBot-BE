using System.Buffers;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Robots;
using SmartMarketBot.Application.Services;
using SmartMarketBot.Domain.Common;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Options;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class MqttClientService(
    IServiceScopeFactory scopeFactory,
    IOptions<MqttOptions> mqttOptions,
    ILogger<MqttClientService> logger) : IHostedService, IRobotCommandPublisher
{
    private readonly MqttOptions _mqttOptions = mqttOptions.Value;
    private readonly IMqttClient _mqttClient = new MqttClientFactory().CreateMqttClient();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;
        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;

        // Chạy kết nối trong background để tránh block tiến trình khởi động Web API trên Azure App Service khi không có MQTT Broker
        _ = Task.Run(async () =>
        {
            try
            {
                var options = BuildClientOptions();
                await _mqttClient.ConnectAsync(options, CancellationToken.None);
                await SubscribeTopicsAsync(CancellationToken.None);
                logger.LogInformation("Successfully connected to MQTT broker.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to MQTT broker during startup. API remains fully functional.");
            }
        });

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptions(), cancellationToken);
        }
    }

    public async Task PublishCommandAsync(string robotCode, string command, string? payload, CancellationToken cancellationToken = default)
    {
        var topic = $"smartmarketbot/robot/{robotCode}/command";
        var body = JsonSerializer.Serialize(new
        {
            command,
            payload,
            timestamp = VnDateTime.Now
        });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(body)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        if (!_mqttClient.IsConnected)
        {
            var options = BuildClientOptions();
            await _mqttClient.ConnectAsync(options, cancellationToken);
            await SubscribeTopicsAsync(cancellationToken);
        }

        await _mqttClient.PublishAsync(message, cancellationToken);
    }

    private MqttClientOptions BuildClientOptions()
    {
        var builder = new MqttClientOptionsBuilder()
            .WithClientId(_mqttOptions.ClientId)
            .WithTcpServer(_mqttOptions.Host, _mqttOptions.Port);

        if (!string.IsNullOrWhiteSpace(_mqttOptions.Username))
        {
            builder.WithCredentials(_mqttOptions.Username, _mqttOptions.Password);
        }

        if (_mqttOptions.UseTls)
        {
            builder.WithTlsOptions(tls =>
            {
                if (_mqttOptions.AllowUntrustedCertificates)
                {
                    tls.WithCertificateValidationHandler(_ => true);
                }
            });
        }

        return builder.Build();
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs _)
    {
        logger.LogWarning("MQTT client disconnected.");
        return Task.CompletedTask;
    }

    private async Task SubscribeTopicsAsync(CancellationToken cancellationToken)
    {
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter("smartmarketbot/robot/+/status")
            .WithTopicFilter("smartmarketbot/robot/+/telemetry")
            .Build();

        await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
    }

    private async Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        try
        {
            var topic = eventArgs.ApplicationMessage.Topic ?? string.Empty;
            if (string.IsNullOrWhiteSpace(topic))
            {
                return;
            }

            var payloadSequence = eventArgs.ApplicationMessage.Payload;
            var payloadBytes = payloadSequence.IsSingleSegment
                ? payloadSequence.First.ToArray()
                : CopySequenceToArray(payloadSequence);
            // Strip UTF-8 BOM nếu có (EF BB BF) — một số tool/client gửi BOM
            if (payloadBytes.Length >= 3 && payloadBytes[0] == 0xEF && payloadBytes[1] == 0xBB && payloadBytes[2] == 0xBF)
            {
                payloadBytes = payloadBytes[3..];
            }
            var payloadJson = payloadBytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(payloadBytes);
            var topicParts = topic.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (topicParts.Length < 4)
            {
                return;
            }

            var robotCode = topicParts[2];
            var eventType = topicParts[3];

            logger.LogInformation("MQTT recv [{Topic}] {Bytes}B: {Payload}", topic, payloadJson.Length, payloadJson);
            var payload = JsonSerializer.Deserialize<IncomingRobotPayload>(payloadJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new IncomingRobotPayload();
            var timestamp = payload.Timestamp ?? VnDateTime.Now;

            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notifier = scope.ServiceProvider.GetRequiredService<IRobotHubNotifier>();

            var robot = await dbContext.Robots.FirstOrDefaultAsync(x => x.RobotCode == robotCode);
            var log = new RobotLog
            {
                RobotID = robot?.RobotID,
                battery = payload.Battery,
                location = payload.Location,
                status = payload.Status,
                timestamp = timestamp,
                CurrentNodeID = payload.CurrentNodeId,
                Mode = payload.Mode,
                IsOnline = payload.IsOnline,
                XCoord = payload.XCoord,
                YCoord = payload.YCoord,
                HeadingRad = payload.HeadingRad
            };

            dbContext.RobotLogs.Add(log);

            if (robot is not null)
            {
                if (payload.Battery.HasValue)
                {
                    robot.BatteryPct = payload.Battery.Value;
                }

                if (!string.IsNullOrWhiteSpace(payload.Mode))
                {
                    robot.Mode = payload.Mode;
                }

                if (payload.IsOnline.HasValue)
                {
                    robot.IsOnline = payload.IsOnline.Value;
                }

                if (payload.CurrentNodeId.HasValue)
                {
                    robot.CurrentNodeID = payload.CurrentNodeId.Value;
                }

                robot.LastSeenAt = timestamp;
            }

            await dbContext.SaveChangesAsync();

            if (eventType.Equals("telemetry", StringComparison.OrdinalIgnoreCase))
            {
                var telemetry = new RobotTelemetryDto(
                    robotCode,
                    payload.Battery,
                    payload.Location,
                    payload.Status,
                    payload.CurrentNodeId,
                    payload.Mode,
                    payload.IsOnline,
                    payload.XCoord,
                    payload.YCoord,
                    timestamp,
                    LidarFront: payload.LidarFront,
                    LidarRear: payload.LidarRear,
                    RpmFL: payload.RpmFL,
                    RpmFR: payload.RpmFR,
                    RpmRL: payload.RpmRL,
                    RpmRR: payload.RpmRR,
                    NavState: payload.NavState,
                    Estop: payload.Estop);

                await notifier.NotifyTelemetryAsync(telemetry);
            }
            else if (eventType.Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                var status = new RobotStatusDto(
                    robotCode,
                    payload.Battery,
                    payload.Location,
                    payload.Status,
                    payload.Mode,
                    payload.IsOnline,
                    timestamp);

                await notifier.NotifyStatusAsync(status);

                /* ── Phase 3.5: Auto-dock khi pin yếu ───────────────── */
                if ("low_battery".Equals(payload.Status, StringComparison.OrdinalIgnoreCase))
                {
                    await HandleAutoDockAsync(scope, robotCode, robot);
                }

                /* ── Phase 3.5: Reroute khi robot bị kẹt ────────────── */
                if ("reroute_needed".Equals(payload.Status, StringComparison.OrdinalIgnoreCase)
                    && robot is not null && robot.CurrentNodeID.HasValue)
                {
                    await HandleRerouteAsync(scope, robotCode, robot);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process MQTT robot message.");
        }
    }

    /// <summary>Phase 3.5 — Điều hướng robot về trạm sạc (DOCK_NODE_ID = 2).</summary>
    private async Task HandleAutoDockAsync(IServiceScope scope, string robotCode, Domain.Entities.Robot? robot)
    {
        try
        {
            var navService = scope.ServiceProvider.GetRequiredService<INavigationService>();
            var startNode  = robot?.CurrentNodeID ?? 1;
            const int dockNodeId = 2; // Trạm sạc — khớp seed data + Config.h DOCK_NODE_ID

            logger.LogWarning("Robot {RobotCode} battery low — auto-navigating to dock node {DockNode}.",
                              robotCode, dockNodeId);

            var route = await navService.PlanRouteAsync(
                new Application.Models.Navigation.RoutePlanRequestDto(startNode, dockNodeId),
                CancellationToken.None);

            if (route.Nodes.Count == 0)
            {
                logger.LogError("Auto-dock: no route found from node {Start} to dock {Dock}.",
                                startNode, dockNodeId);
                return;
            }

            var waypoints = route.Nodes
                .Select(n => new { x = n.X, y = n.Y, nodeId = n.NodeId })
                .ToList();
            var payload = System.Text.Json.JsonSerializer.Serialize(new { waypoints });

            await _mqttClient.PublishAsync(
                new MqttApplicationMessageBuilder()
                    .WithTopic($"smartmarketbot/robot/{robotCode}/command")
                    .WithPayload(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        command = "navigate",
                        payload,
                        timestamp = VnDateTime.Now
                    }))
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build());

            logger.LogInformation("Auto-dock command sent to {RobotCode}: {NodeCount} waypoints to dock.",
                                  robotCode, route.Nodes.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to auto-dock robot {RobotCode}.", robotCode);
        }
    }

    /// <summary>Phase 3.5 — Tính lại route và gửi khi robot gặp vật cản không tránh được.</summary>
    private async Task HandleRerouteAsync(IServiceScope scope, string robotCode, Domain.Entities.Robot robot)
    {
        try
        {
            var navCommandService = scope.ServiceProvider.GetRequiredService<Application.Services.NavigationCommandService>();

            /* Lấy destination từ log gần nhất — tạm dùng waypoint cuối cùng trong route */
            var dbCtx = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();
            var destNode = await dbCtx.RobotLogs
                .AsNoTracking()
                .Where(l => l.RobotID == robot.RobotID && l.CurrentNodeID.HasValue)
                .OrderByDescending(l => l.timestamp)
                .Select(l => l.CurrentNodeID)
                .FirstOrDefaultAsync();

            /* Fallback: tính lại từ node hiện tại về node tiếp theo đã biết */
            int startNode = robot.CurrentNodeID ?? 1;
            int endNode   = destNode ?? 5; // Default fallback — cần improve Phase 4

            logger.LogWarning("Rerouting robot {RobotCode} from node {Start} to {End}.",
                              robotCode, startNode, endNode);

            await navCommandService.RerouteAsync(
                new Application.Models.Navigation.RerouteRequestDto(
                    robotCode, startNode, endNode, null),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reroute robot {RobotCode}.", robotCode);
        }
    }

    private sealed class IncomingRobotPayload
    {
        public int? Battery { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
        public int? CurrentNodeId { get; set; }
        public string? Mode { get; set; }
        public bool? IsOnline { get; set; }
        public double? XCoord { get; set; }
        public double? YCoord { get; set; }
        public DateTime? Timestamp { get; set; }

        // Phase 1 — sensor telemetry từ ESP32-S3
        public int? LidarFront { get; set; }
        public int? LidarRear { get; set; }
        public double? RpmFL { get; set; }
        public double? RpmFR { get; set; }
        public double? RpmRL { get; set; }
        public double? RpmRR { get; set; }
        public string? NavState { get; set; }
        public bool? Estop { get; set; }
        // Phase 2 — Dead Reckoning
        public double? HeadingRad { get; set; }
    }

    private static byte[] CopySequenceToArray(in ReadOnlySequence<byte> sequence)
    {
        if (sequence.IsEmpty)
        {
            return [];
        }

        var bytes = new byte[(int)sequence.Length];
        sequence.CopyTo(bytes);
        return bytes;
    }
}
