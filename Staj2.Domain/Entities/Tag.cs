namespace Staj2.Domain.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = null!; // Etiket adı (Örn: "Kritik", "Ofis-1")
    public bool IsDeleted { get; set; } = false;
    // Many-to-Many ilişki için Computer listesi
    public List<Computer> Computers { get; set; } = new();
    public ICollection<UserTagAccess> UserAccesses { get; set; } = new List<UserTagAccess>();
}