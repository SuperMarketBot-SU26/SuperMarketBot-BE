namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// Cảnh báo cho member (ví dụ: gần đạt budget, có deal mới, v.v.).
/// Bảng phụ trợ - ERD V4.0 không có nhưng MemberService cần dùng.
/// </summary>
public class MemberAlert
{
    public int AlertId { get; set; }
    public int MemberId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string AlertMessage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;

    public virtual Member? Member { get; set; }
}
