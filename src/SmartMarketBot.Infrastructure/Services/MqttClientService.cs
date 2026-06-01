using System.Buffers;
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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;
        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;

        var options = BuildClientOptions();
        await _mqttClient.ConnectAsync(options, cancellationToken);
        await SubscribeTopicsAsync(cancellationToken);
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
            timestamp = DateTime.UtcNow
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
            var payloadJson = payloadBytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(payloadBytes);
            var topicParts = topic.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (topicParts.Length < 4)
            {
                return;
            }

            var robotCode = topicParts[2];
            var eventType = topicParts[3];

            var payload = JsonSerializer.Deserialize<IncomingRobotPayload>(payloadJson) ?? new IncomingRobotPayload();
            var timestamp = payload.Timestamp ?? DateTime.UtcNow;

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
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process MQTT robot message.");
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
