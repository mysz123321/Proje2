using Staj2.Services.Models;
using Staj2.Services.Models.Auth;

namespace Staj2.Services.Interfaces;

public interface IAuthService
{
    // 1. Kullanıcı Girişi
    // Eskiden: Task<(bool IsSuccess, string? ErrorMessage, object? Data)>
    Task<ServiceResult<object>> LoginAsync(LoginRequest request);

    // 2. Şifre Belirleme
    // Eskiden: Task<(bool IsSuccess, string? ErrorMessage, bool isConflict)>
    // (isConflict durumunu servisteki mesajdan controller'da yakalayabiliriz)
    Task<ServiceResult> SetPasswordAsync(SetPasswordRequest request);

    // 3. Kullanıcının Yetkilerini Getir
    // Eskiden: Task<(bool IsSuccess, string? ErrorMessage, List<string>? Permissions)>
    Task<ServiceResult<List<string>>> GetMyPermissionsAsync(int userId);

    // 4. Token Yenileme
    // Eskiden: Task<(bool IsSuccess, string? Token, string? RefreshToken)>
    // Token ve RefreshToken'ı Data içinde anonymous object (veya DTO) olarak döneceğiz.
    Task<ServiceResult<object>> RefreshTokenAsync(string token);
}