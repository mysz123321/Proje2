namespace Staj2.Domain.Entities;

public class UserComputerAccess
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int ComputerId { get; set; }
    public Computer Computer { get; set; } = null!;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}