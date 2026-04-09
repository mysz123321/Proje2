using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;
using Staj2.Services.Models;

namespace Staj2.Services.Services;

// YENİ: BaseService'den miras alıyoruz
public class RegistrationService : BaseService, IRegistrationService
{
    private readonly IConfiguration _config;

    // YENİ: AppDbContext'i base (BaseService) sınıfa gönderiyoruz
    public RegistrationService(AppDbContext db, IConfiguration config) : base(db)
    {
        _config = config;
    }

    // YAZMA İŞLEMİ (Yeni kayıt talebi ekleniyor) - Sarmalandı
    public Task<ServiceResult<(int RequestId, string Email, string Username)>> CreateRegistrationAsync(CreateRegistrationRequest request)
    {
        // Tuple dönüş tipine uygun olarak Generic Wrapper kullanıyoruz
        return ExecuteWithDbHandlingAsync<(int RequestId, string Email, string Username)>(async () =>
        {
            if (request == null)
                return ServiceResult<(int RequestId, string Email, string Username)>.Failure("Geçersiz istek.");

            var username = (request.Username ?? "").Trim();
            var email = (request.Email ?? "").Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
                return ServiceResult<(int RequestId, string Email, string Username)>.Failure("Kullanıcı adı ve email zorunludur.");

            // 1) Kullanıcı zaten USERS tablosunda var mı?
            var userExists = await _db.Users.AnyAsync(x => x.Email == email || x.Username == username);
            if (userExists)
                return ServiceResult<(int RequestId, string Email, string Username)>.Failure("Bu email veya kullanıcı adı zaten kayıtlı. Giriş yapabilirsiniz.");

            // 2) Sadece BEKLEYEN (Pending) bir istek var mı?
            var pendingRequest = await _db.RegistrationRequests.FirstOrDefaultAsync(x =>
                (x.Email == email || x.Username == username) &&
                x.Status == RegistrationStatus.Pending);

            if (pendingRequest != null)
            {
                return ServiceResult<(int RequestId, string Email, string Username)>.Failure("Bu hesap için zaten onay bekleyen bir talep var. Lütfen admin onayını bekleyiniz.");
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

            // ID'nin oluşması için SaveChanges yapıyoruz, Transaction sayesinde güvenli
            await _db.SaveChangesAsync();

            // Başarılı dönüş: Tuple tipinde Data yolluyoruz
            return ServiceResult<(int RequestId, string Email, string Username)>.Success((rr.Id, rr.Email, rr.Username));

        }, "Kayıt Talebi",DbOperation.Create);
    }
}