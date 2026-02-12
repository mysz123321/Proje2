namespace Staj2.Domain.Entities;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsApproved { get; set; } = true;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
