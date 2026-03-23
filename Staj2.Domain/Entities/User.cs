using Staj2.Domain.Common;

namespace Staj2.Domain.Entities;

public class User : ISoftDeletableEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool IsApproved { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<UserComputerAccess> ComputerAccesses { get; set; } = new List<UserComputerAccess>();
    public ICollection<UserTagAccess> TagAccesses { get; set; } = new List<UserTagAccess>();
}