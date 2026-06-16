namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// Sự kiện của Member (sinh nhật, kỷ niệm thành viên, v.v.) - dùng để cấp deal.
/// Bảng phụ trợ - ERD V4.0 không có nhưng MemberService cần dùng.
/// </summary>
public class MemberEvent
{
    public int EventId { get; set; }
    public int MemberId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal? DiscountPct { get; set; }
    public bool IsProcessed { get; set; } = false;
}
