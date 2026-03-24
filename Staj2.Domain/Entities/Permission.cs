// Staj2.Domain/Entities/Permission.cs
namespace Staj2.Domain.Entities;

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    // --- GERİ GELEN ALAN ---
    public string Description { get; set; } = null!;

    // Bu yetkinin sol menüde hangi sekmeyi görünür kılacağı bilgisi
    public int? SidebarItemId { get; set; }
    public SidebarItem? SidebarItem { get; set; }

    // İlişkiler
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}