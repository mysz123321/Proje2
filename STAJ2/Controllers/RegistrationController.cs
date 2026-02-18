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
        if (request == null) return BadRequest("Geçersiz istek.");

        var username = (request.Username ?? "").Trim();
        var email = (request.Email ?? "").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            return BadRequest("Kullanıcı adı ve email zorunludur.");

        // 1) Kullanıcı zaten USERS tablosunda (aktif kullanıcı) var mı?
        var userExists = await _db.Users.AnyAsync(x => x.Email == email || x.Username == username);
        if (userExists)
            return Conflict("Bu email veya kullanıcı adı zaten kayıtlı. Giriş yapabilirsiniz.");

        // 2) Sadece BEKLEYEN (Pending) bir istek var mı diye bakıyoruz.
        // (Onaylanmış veya Reddedilmiş eski istekler bizi ilgilendirmiyor, yeni talep açabiliriz)
        var pendingRequest = await _db.RegistrationRequests.FirstOrDefaultAsync(x =>
            (x.Email == email || x.Username == username) &&
            x.Status == RegistrationStatus.Pending);

        if (pendingRequest != null)
        {
            return Conflict("Bu hesap için zaten onay bekleyen bir talep var. Lütfen admin onayını bekleyiniz.");
        }

        // 3) Yeni kayıt isteğini oluştur (INSERT)
        // Eski kayıtları silmiyoruz, veritabanına yeni bir satır ekliyoruz.
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

        // 4) Mail Gönderimi
        try
        {
            await _mail.SendAsync(
                rr.Email,
                "Kayıt İsteğiniz Alındı",
                $"Merhaba {rr.Username},\n\nKayıt isteğiniz alındı. Yönetici onayından sonra bilgilendirileceksiniz."
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Mail hatası: {ex.Message}");
        }

        return Ok(new { rr.Id, message = "Kayıt isteği alındı. Admin onayı bekleniyor." });
    }
}

public class CreateRegistrationRequest
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
}