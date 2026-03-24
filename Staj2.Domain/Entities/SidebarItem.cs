// Staj2.Domain/Entities/SidebarItem.cs
namespace Staj2.Domain.Entities
{
    public class SidebarItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string TargetView { get; set; } = string.Empty;
        public int OrderIndex { get; set; }

        // RequiredPermissionId ve RequiredPermission özellikleri SİLİNDİ.
    }
}