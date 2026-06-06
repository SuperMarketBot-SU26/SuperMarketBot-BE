namespace SmartMarketBot.Domain.Entities;

public class Staff
{
    public int StaffID { get; set; }
    public int AccountID { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public virtual Account Account { get; set; } = null!;
}
