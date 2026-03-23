using System;

namespace Staj2.Domain.Entities
{
    public class UserRegistrationRequest
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;

        // --- Controller'ların çalışması için gereken temel alanlar ---
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Kayıt tarihi
        public string? RejectionReason { get; set; }               // Red sebebi
        public int? RequestedRoleId { get; set; }                  // İstenen rol
        public int? ApprovedByUserId { get; set; }                 // Onaylayan
        public DateTime? ApprovedAt { get; set; }                  // Onay tarihi
        public DateTime? RejectedAt { get; set; }                  // Red tarihi

        // --- SENİN ÖZEL İSTEĞİN (TEK LOG ALANI) ---
        public int? RejectedBy { get; set; }
    }
}