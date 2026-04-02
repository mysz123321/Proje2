namespace Staj2.Domain.Entities;

public class UserTagAccess
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}