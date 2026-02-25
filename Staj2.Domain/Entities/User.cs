namespace Staj2.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool IsApproved { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Tekil RoleId ve Role kalktı, liste geldi:
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<UserComputerAccess> ComputerAccesses { get; set; } = new List<UserComputerAccess>();
    public ICollection<UserTagAccess> TagAccesses { get; set; } = new List<UserTagAccess>();
}