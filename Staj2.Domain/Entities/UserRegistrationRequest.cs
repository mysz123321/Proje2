namespace Staj2.Domain.Entities;

public class UserRegistrationRequest
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;

    public int RequestedRoleId { get; set; }
    public Role RequestedRole { get; set; } = null!;

    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;

    public int? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }

    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }

    public ICollection<PasswordSetupToken> PasswordSetupTokens { get; set; } = new List<PasswordSetupToken>();
}
