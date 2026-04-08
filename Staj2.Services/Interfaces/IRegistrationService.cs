using Staj2.Services.Models;

namespace Staj2.Services.Interfaces;

public interface IRegistrationService
{
    // Eski karmaşık tuple yerine ServiceResult kullanıyoruz
    Task<ServiceResult<(int RequestId, string Email, string Username)>> CreateRegistrationAsync(CreateRegistrationRequest request);
}