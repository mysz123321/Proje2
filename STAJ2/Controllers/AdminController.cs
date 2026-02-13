using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using STAJ2.Services;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Yönetici")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMailSender _mail;

    private readonly IConfiguration _config;

    public AdminController(AppDbContext db, IMailSender mail, IConfiguration config)
    {
        _db = db;
        _mail = mail;
        _config = config;
    }


    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { message = "Admin endpoint çalışıyor ✅" });

    [HttpGet("requests")]
    public async Task<IActionResult> PendingRequests()
    {
        var list = await _db.UserRegistrationRequests
            .Where(x => x.Status == RegistrationStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Username,
                x.Email,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost("approve/{id:int}")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveRequest body)
    {
        var rr = await _db.UserRegistrationRequests
            .FirstOrDefaultAsync(x => x.Id == id);

        if (rr == null) return NotFound("İstek bulunamadı.");
        if (rr.Status != RegistrationStatus.Pending) return BadRequest("Bu istek zaten işlenmiş.");

        // Rol doğrula (1/2/3)
        var roleExists = await _db.Roles.AnyAsync(r => r.Id == body.RoleId);
        if (!roleExists) return BadRequest("Geçersiz rol.");

        rr.RequestedRoleId = body.RoleId;
        rr.Status = RegistrationStatus.Approved;
        rr.ApprovedAt = DateTime.UtcNow;

        // Admin kim? (sub claim)
        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(adminIdStr, out var adminId))
            rr.ApprovedByUserId = adminId;


        // Token üret (raw) + hash sakla
        var rawToken = GenerateToken();
        var tokenHash = Sha256(rawToken);

        var tokenRow = new PasswordSetupToken
        {
            RegistrationRequestId = rr.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false
        };

        _db.PasswordSetupTokens.Add(tokenRow);
        await _db.SaveChangesAsync();
            
        var baseUrl = _config.GetSection("App")["FrontendBaseUrl"] ?? "https://localhost:7265";
        var link = $"{baseUrl}/set-password.html?token={rawToken}";


        await _mail.SendAsync(
            rr.Email,
            "Hesabınız onaylandı - Şifre belirleme",
            $"Merhaba ,\n\nŞifrenizi belirlemek için link:\n{link}\n\nNot: Link 24 saat geçerlidir."
        );

        return Ok(new { message = "Onaylandı ve şifre belirleme linki gönderildi (console).", expiresAt = tokenRow.ExpiresAt });
    }

    [HttpPost("reject/{id:int}")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectRequest body)
    {
        var rr = await _db.UserRegistrationRequests.FirstOrDefaultAsync(x => x.Id == id);

        if (rr == null) return NotFound("İstek bulunamadı.");
        if (rr.Status != RegistrationStatus.Pending) return BadRequest("Bu istek zaten işlenmiş.");

        rr.Status = RegistrationStatus.Rejected;
        rr.RejectedAt = DateTime.UtcNow;
        rr.RejectionReason = string.IsNullOrWhiteSpace(body.Reason) ? null : body.Reason.Trim();

        await _db.SaveChangesAsync();
        return Ok(new { message = "İstek reddedildi." });
    }
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var list = await _db.Users
            .Include(u => u.Role)
            .OrderBy(u => u.Username)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                Role = u.Role.Name
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound("Kullanıcı bulunamadı.");

        if (user.Role.Name == "Yönetici")
            return BadRequest("Admin kullanıcıları silinemez.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Kullanıcı silindi." });
    }

    private static string GenerateToken()
    {
        // 32 byte -> URL-safe token
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string Sha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes); // 64 hex
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}
//public class ComputerController
//{
//    [Authorize(Roles = "Yönetici")]
//    public Task<IActionResult> AddComputer()
//    {

//    }
//}
public class ApproveRequest
{
    public int RoleId { get; set; } // 1/2/3
}

public class RejectRequest
{
    public string? Reason { get; set; }
}
