namespace Staj2.Domain.Entities
{
    public class SidebarItem
    {
        public int Id { get; set; }

        // Uyarıyı gidermek için = string.Empty; ekledik
        public string Title { get; set; } = string.Empty;

        public string? Icon { get; set; }

        // Uyarıyı gidermek için = string.Empty; ekledik
        public string TargetView { get; set; } = string.Empty;

        public string? RequiredPermission { get; set; }

        public int OrderIndex { get; set; }
    }
}