using System.Text.Json;
using System.Text.Json.Serialization;
using SmartMarketBot.Application.Models.Admin;
using SmartMarketBot.Application.Models.AisleScans;
using SmartMarketBot.Application.Models.Auth;
using SmartMarketBot.Application.Models.Members;
using SmartMarketBot.Application.Models.Realtime;
using SmartMarketBot.Application.Models.Robots;
using SmartMarketBot.Application.Models.Staff;

namespace SmartMarketBot.Application;

/// <summary>
/// Source-generated serializer context cho OpenAPI schema generation (.NET 10).
/// Dùng JsonStringEnumConverter để enum serialize thành string thay vì số.
/// </summary>
[JsonSerializable(typeof(AuthResponseDto))]
[JsonSerializable(typeof(LoginRequestDto))]
[JsonSerializable(typeof(RefreshTokenRequestDto))]
[JsonSerializable(typeof(VerifyOtpDto))]
[JsonSerializable(typeof(ResendOtpDto))]
[JsonSerializable(typeof(ForgotPasswordDto))]
[JsonSerializable(typeof(ResetPasswordDto))]
[JsonSerializable(typeof(RegisterRequestOtpDto))]
[JsonSerializable(typeof(RegisterRequestDto))]
[JsonSerializable(typeof(FaceLoginRequestDto))]
[JsonSerializable(typeof(FaceLoginResponseDto))]
[JsonSerializable(typeof(FaceLoginMemberDto))]
[JsonSerializable(typeof(RobotTelemetryDto))]
[JsonSerializable(typeof(RobotStatusDto))]
[JsonSerializable(typeof(RobotPoseDto))]
[JsonSerializable(typeof(RobotDto))]
[JsonSerializable(typeof(PublishRobotCommandRequestDto))]
[JsonSerializable(typeof(NavigateRobotRequestDto))]
[JsonSerializable(typeof(ShelfScanDto))]
[JsonSerializable(typeof(CreateAisleScanRequestDto))]
[JsonSerializable(typeof(SetBudgetRequestDto))]
[JsonSerializable(typeof(SetBudgetResponseDto))]
[JsonSerializable(typeof(ScanItemRequestDto))]
[JsonSerializable(typeof(ScanItemResponseDto))]
[JsonSerializable(typeof(AlternativeProductDto))]
[JsonSerializable(typeof(MemberDealDto))]
[JsonSerializable(typeof(MemberDealsResponseDto))]
[JsonSerializable(typeof(MemberAlertDto))]
[JsonSerializable(typeof(MemberAlertsResponseDto))]
[JsonSerializable(typeof(MarkAlertsReadRequestDto))]
[JsonSerializable(typeof(MemberEventDto))]
[JsonSerializable(typeof(RestockTaskDto))]
[JsonSerializable(typeof(RestockTaskListResponseDto))]
[JsonSerializable(typeof(CompleteRestockRequestDto))]
[JsonSerializable(typeof(ReportOosRequestDto))]
[JsonSerializable(typeof(ReportOosResponseDto))]
[JsonSerializable(typeof(MemberRealtimeUpdateDto))]
[JsonSerializable(typeof(StaffRealtimeAlertDto))]
[JsonSerializable(typeof(AdCampaignDto))]
[JsonSerializable(typeof(CreateAdCampaignRequestDto))]
[JsonSerializable(typeof(UpdateAdCampaignRequestDto))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTime?))]
[JsonSerializable(typeof(JsonSerializerContext))]
internal partial class AppJsonContext : JsonSerializerContext
{
    private static JsonStringEnumConverter? _enumConverter;

    public static JsonSerializerOptions DefaultOptions =>
        new()
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters = { EnumConverter }
        };

    public static System.Text.Json.Serialization.JsonStringEnumConverter EnumConverter =>
        _enumConverter ??= new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase);
}
