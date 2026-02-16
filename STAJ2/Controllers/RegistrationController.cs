using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using STAJ2.Services; // Mail servisi için gerekli

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMailSender _mail; // <--- MAİL SERVİSİ GERİ GELDİ

    // Constructor'a IMailSender eklendi
    public RegistrationController(AppDbContext db, IMailSender mail)
    {
        _db = db;
        _mail = mail;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRegistrationRequest request)
    {
        if (request == null)
            return BadRequest("Geçersiz istek.");

        var username = (request.Username ?? "").Trim();
        var email = (request.Email ?? "").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            return BadRequest("Kullanıcı adı ve email zorunludur.");

        // 1) Kullanıcı zaten ana tabloda (Users) var mı?
        var userExists = await _db.Users.AnyAsync(x => x.Email == email || x.Username == username);
        if (userExists)
            return Conflict("Bu email veya kullanıcı adı zaten kayıtlı. Giriş yapabilirsiniz.");

        // 2) Mevcut bir kayıt isteği var mı?
        var existingRequest = await _db.RegistrationRequests.FirstOrDefaultAsync(x =>
            x.Email == email || x.Username == username);

        if (existingRequest != null)
        {
            // Eğer istek hala BEKLEYEN durumundaysa hata ver
            if (existingRequest.Status == RegistrationStatus.Pending)
            {
                return Conflict("Bu email veya kullanıcı adı için zaten bekleyen bir kayıt isteği var.");
            }

            // Eğer istek onaylanmış/reddedilmiş ama kullanıcı silinmişse, 
            // Unique Index hatası almamak için eski isteği siliyoruz.
            _db.RegistrationRequests.Remove(existingRequest);
        }

        // 3) Yeni kayıt isteğini oluştur (Varsayılan Rol: Görüntüleyici)
        const int viewerRoleId = 3;

        var rr = new UserRegistrationRequest
        {
            Username = username,
            Email = email,
            RequestedRoleId = viewerRoleId,
            Status = RegistrationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.RegistrationRequests.Add(rr);
        await _db.SaveChangesAsync();

        // 4) --- MAİL GÖNDERİMİ ---
        // Kayıt başarılı olduktan sonra kullanıcıya bilgi maili atıyoruz.
        try
        {
            await _mail.SendAsync(
                rr.Email,
                "Kayıt İsteğiniz Alındı",
                $"Merhaba {rr.Username},\n\nKayıt isteğiniz başarıyla sistemimize ulaştı. Yönetici onayından sonra şifrenizi belirleyebileceğiniz bir mail daha alacaksınız."
            );
        }
        catch (Exception ex)
        {
            // Mail gitmese bile kayıt başarılı olduğu için hata dönmüyoruz, sadece logluyoruz.
            Console.WriteLine($"Kayıt bildirim maili gönderilemedi: {ex.Message}");
        }

        return Ok(new { rr.Id, message = "Kayıt isteği alındı. Admin onayı bekleniyor." });
    }
}

public class CreateRegistrationRequest
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
}