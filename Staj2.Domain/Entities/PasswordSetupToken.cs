namespace Staj2.Domain.Entities;

public class PasswordSetupToken
{
    public int Id { get; set; }

    public int RegistrationRequestId { get; set; }
    public UserRegistrationRequest RegistrationRequest { get; set; } = null!;

    // Güvenlik: DB’de raw token değil, hash saklayacağız
    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
