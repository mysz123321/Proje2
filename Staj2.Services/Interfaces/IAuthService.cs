using Staj2.Services.Models.Auth;

namespace Staj2.Services.Interfaces;

public interface IAuthService
{
    Task<(bool IsSuccess, string? ErrorMessage, object? Data)> LoginAsync(LoginRequest request);
    Task<(bool IsSuccess, string? ErrorMessage, bool isConflict)> SetPasswordAsync(SetPasswordRequest request);
    Task<(bool IsSuccess, string? ErrorMessage, List<string>? Permissions)> GetMyPermissionsAsync(int userId);
}