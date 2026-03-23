namespace Staj2.Domain.Entities
{
    public class SidebarItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Icon { get; set; }

        public string TargetView { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        // --- YENİ EKLENEN İLİŞKİ ALANLARI ---

        // Veritabanında tutulacak Foreign Key kolonu
        public int? RequiredPermissionId { get; set; }

        // Kod tarafında ilişkili Permission nesnesine erişmek için Navigation Property
        public Permission? RequiredPermission { get; set; }
    }
}