using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using STAJ2.Models;
using STAJ2.Services;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography; // <--- BU SATIR EKSİKTİ, EKLENDİ

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Yönetici")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMailSender _mail;

    public AdminController(AppDbContext db, IMailSender mail)
    {
        _db = db;
        _mail = mail;
    }

    // --- KULLANICI YÖNETİMİ ---
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers() =>
        Ok(await _db.Users.Include(u => u.Roles)
                          .OrderBy(u => u.Username)
                          .Select(u => new { u.Id, u.Username, u.Email, Roles = u.Roles.Select(r => r.Name).ToList() })
                          .ToListAsync());

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user != null) { _db.Users.Remove(user); await _db.SaveChangesAsync(); }
        return Ok(new { message = "Kullanıcı silindi." });
    }

    [HttpPut("users/{userId}/change-roles")]
    public async Task<IActionResult> ChangeUserRoles(int userId, [FromBody] ChangeRolesRequest request)
    {
        var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return NotFound(new { message = "Kullanıcı bulunamadı." });

        var newRoles = await _db.Roles.Where(r => request.NewRoleIds.Contains(r.Id)).ToListAsync();
        if (newRoles.Count == 0) return BadRequest(new { message = "En az bir geçerli rol seçilmelidir." });

        user.Roles.Clear();
        foreach (var role in newRoles) user.Roles.Add(role);

        await _db.SaveChangesAsync();
        return Ok(new { message = "Roller güncellendi." });
    }

    // --- KAYIT İSTEKLERİ YÖNETİMİ ---

    [HttpGet("requests")]
    public async Task<IActionResult> PendingRequests() =>
        Ok(await _db.RegistrationRequests.Where(x => x.Status == RegistrationStatus.Pending).ToListAsync());

    // REDDETME İŞLEMİ
    [HttpPost("requests/reject")]
    public async Task<IActionResult> RejectRequest([FromBody] RejectRegistrationRequest request)
    {
        if (!string.IsNullOrEmpty(request.RejectionReason) && request.RejectionReason.Length > 200)
        {
            return BadRequest(new { message = "Ret gerekçesi 200 karakterden uzun olamaz." });
        }

        var registration = await _db.RegistrationRequests.FindAsync(request.RequestId);
        if (registration == null) return NotFound(new { message = "Talep bulunamadı." });

        if (registration.Status != RegistrationStatus.Pending)
            return BadRequest(new { message = "Bu talep zaten işleme alınmış." });

        registration.Status = RegistrationStatus.Rejected;
        registration.RejectedAt = DateTime.UtcNow;
        registration.RejectionReason = request.RejectionReason;

        await _db.SaveChangesAsync();

        try
        {
            await _mail.SendAsync(
                registration.Email,
                "Kayıt Talebiniz Reddedildi",
                $"Merhaba {registration.Username},\n\nTalebiniz maalesef onaylanmadı.\nSebep: {registration.RejectionReason ?? "Belirtilmedi"}"
            );
        }
        catch { }

        return Ok(new { message = "Talep reddedildi." });
    }

    // ONAYLAMA İŞLEMİ (DÜZELTİLMİŞ HALİ)
    [HttpPost("requests/approve/{id}")]
    public async Task<IActionResult> ApproveRequest(int id, [FromBody] ChangeRoleRequest? req)
    {
        var request = await _db.RegistrationRequests.FindAsync(id);
        if (request == null) return NotFound(new { message = "Talep bulunamadı." });

        if (request.Status != RegistrationStatus.Pending)
            return BadRequest(new { message = "Bu talep zaten işlenmiş." });

        // 1. Kullanıcı Kontrolü (Zaten var mı?)
        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = $"Bu kullanıcı zaten sistemde kayıtlı! (Kullanıcı: {existingUser.Username})" });
        }

        // 2. Onaylayan Admin ID'sini bul
        var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int adminId = 0;
        if (!int.TryParse(adminIdString, out adminId))
        {
            var firstUser = await _db.Users.FirstOrDefaultAsync();
            if (firstUser != null) adminId = firstUser.Id;
        }
        request.ApprovedByUserId = adminId;

        // 3. ROL GÜNCELLEME (ÖNEMLİ)
        // Eğer admin onaylarken rolü değiştirdiyse, talep tablosundaki rolü güncelliyoruz.
        // Böylece AuthController (şifre belirleme ekranı) kullanıcıyı oluştururken bu yeni rolü kullanacak.
        if (req != null && req.NewRoleId > 0)
        {
            request.RequestedRoleId = req.NewRoleId;
        }

        // --- KULLANICI OLUŞTURMA KISMI SİLİNDİ ---
        // Kullanıcı artık burada değil, şifresini belirlediği an AuthController'da oluşacak.

        // 4. Token Oluştur ve Hashle
        var token = Guid.NewGuid().ToString("N");

        string tokenHash;
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            tokenHash = Convert.ToHexString(bytes);
        }

        var setupToken = new PasswordSetupToken
        {
            RegistrationRequestId = request.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false
        };
        _db.PasswordSetupTokens.Add(setupToken);

        // 5. Talebi Güncelle
        request.Status = RegistrationStatus.Approved;
        request.ApprovedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // 6. Mail Gönder
        var frontendLink = $"http://localhost:5267/set-password.html?token={token}";
        try
        {
            // user.Email yerine request.Email kullanıyoruz
            await _mail.SendAsync(request.Email, "Hoşgeldiniz", $"Hesabınız onaylandı. Şifrenizi belirlemek için tıklayın: {frontendLink}");
        }
        catch { }

        return Ok(new { message = "Kayıt onaylandı, kullanıcı şifre belirleme mailini bekliyor." });
    }

    // --- ETİKET YÖNETİMİ ---
    [HttpGet("tags")]
    [Authorize(Roles = "Yönetici,Görüntüleyici,Denetleyici")]
    public async Task<IActionResult> GetTags() => Ok(await _db.Tags.ToListAsync());

    [HttpPost("tags")]
    public async Task<IActionResult> CreateTag([FromBody] TagCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "İsim boş olamaz." });

        if (request.Name.Length > 200)
        {
            return BadRequest(new { message = "Etiket ismi 200 karakterden uzun olamaz." });
        }

        if (await _db.Tags.AnyAsync(t => t.Name == request.Name))
            return BadRequest(new { message = "Bu etiket zaten var." });

        var tag = new Tag { Name = request.Name.Trim() };
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();
        return Ok(tag);
    }

    [HttpDelete("tags/{id:int}")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var tag = await _db.Tags.FindAsync(id);
        if (tag == null) return NotFound(new { message = "Etiket bulunamadı." });

        _db.Tags.Remove(tag);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Etiket silindi." });
    }
}