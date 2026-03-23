using Staj2.Services.Models;

namespace Staj2.Services.Interfaces;

// DİKKAT: Başına public ekledik!
public interface IRegistrationService
{
    Task<(bool IsBadRequest, bool IsConflict, string? ErrorMessage, int? RequestId, string? Email, string? Username)> CreateRegistrationAsync(CreateRegistrationRequest request);
}