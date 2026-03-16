namespace Staj2.Domain.Entities;

public class UserTableAction
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public string ButtonClass { get; set; } = null!;

    public string OnClickFunction { get; set; } = null!;

    public string? RequiredPermission { get; set; }

    public int OrderIndex { get; set; } // Sıralama
}