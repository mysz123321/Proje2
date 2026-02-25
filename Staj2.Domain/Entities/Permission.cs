namespace Staj2.Domain.Entities;

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = null!; // Örn: "Computer.Delete"
    public string Description { get; set; } = null!; // Örn: "Cihaz Silebilir"

    // İlişkiler
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}