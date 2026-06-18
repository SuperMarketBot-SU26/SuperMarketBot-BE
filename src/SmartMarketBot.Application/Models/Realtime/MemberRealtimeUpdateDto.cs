namespace SmartMarketBot.Application.Models.Realtime;

/// <summary>Payload push xuống Member App: cart update, scan warning, budget warning, sponsored refresh.</summary>
public sealed record MemberRealtimeUpdateDto(
    int MemberId,
    string UpdateType,          // "ScanItem" | "BudgetWarning" | "AllergyAlert" | "SponsoredRefresh"
    string Title,
    string Message,
    object? Payload,            // Shape tùy UpdateType — FE tự parse
    DateTime Timestamp);