using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;
using Staj2.Services.Models;

namespace Staj2.Services.Services;

public class RegistrationService : IRegistrationService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    public RegistrationService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<(bool IsBadRequest, bool IsConflict, string? ErrorMessage, int? RequestId, string? Email, string? Username)> CreateRegistrationAsync(CreateRegistrationRequest request)
    {
        if (request == null)
            return (true, false, "Geçersiz istek.", null, null, null);

        var username = (request.Username ?? "").Trim();
        var email = (request.Email ?? "").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            return (true, false, "Kullanıcı adı ve email zorunludur.", null, null, null);

        // 1) Kullanıcı zaten USERS tablosunda var mı?
        var userExists = await _db.Users.AnyAsync(x => x.Email == email || x.Username == username);
        if (userExists)
            return (false, true, "Bu email veya kullanıcı adı zaten kayıtlı. Giriş yapabilirsiniz.", null, null, null);

        // 2) Sadece BEKLEYEN (Pending) bir istek var mı?
        var pendingRequest = await _db.RegistrationRequests.FirstOrDefaultAsync(x =>
            (x.Email == email || x.Username == username) &&
            x.Status == RegistrationStatus.Pending);

        if (pendingRequest != null)
        {
            return (false, true, "Bu hesap için zaten onay bekleyen bir talep var. Lütfen admin onayını bekleyiniz.", null, null, null);
        }

        // 3) Yeni kayıt isteğini oluştur
        int viewerRoleId = _config.GetValue<int>("DefaultRoles:ViewerRoleId", 3);

        var rr = new UserRegistrationRequest
        {
            Username = username,
            Email = email,
            RequestedRoleId = viewerRoleId,
            Status = RegistrationStatus.Pending,
            CreatedAt = DateTime.Now
        };

        _db.RegistrationRequests.Add(rr);
        await _db.SaveChangesAsync();

        // Başarılı dönüş: Hata yok, ID'yi ve Mail bilgilerini Controller'a yolla
        return (false, false, null, rr.Id, rr.Email, rr.Username);
    }
}