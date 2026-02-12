using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly AppDbContext _db;

    public RegistrationController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRegistrationRequest request)
    {
        if (request == null)
            return BadRequest("Geçersiz istek.");

        var username = (request.Username ?? "").Trim();
        var email = (request.Email ?? "").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            return BadRequest("Kullanıcı adı ve email zorunludur.");

        // 1) Kullanıcı zaten var mı? (gerçek kaynak: Users)
        var userExists = await _db.Users.AnyAsync(x => x.Email == email || x.Username == username);
        if (userExists)
            return Conflict("Bu email veya kullanıcı adı zaten kayıtlı. Giriş yapabilirsiniz.");

        // 2) Sadece bekleyen (Pending) request var mı?
        var pendingExists = await _db.UserRegistrationRequests.AnyAsync(x =>
            x.Status == RegistrationStatus.Pending &&
            (x.Email == email || x.Username == username));

        if (pendingExists)
            return Conflict("Bu email veya kullanıcı adı için zaten bekleyen bir kayıt isteği var.");

        // 3) Varsayılan rol: Görüntüleyici (Id=3)
        const int viewerRoleId = 3;

        var rr = new UserRegistrationRequest
        {
            Username = username,
            Email = email,
            RequestedRoleId = viewerRoleId,
            Status = RegistrationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.UserRegistrationRequests.Add(rr);
        await _db.SaveChangesAsync();

        return Ok(new { rr.Id, message = "Kayıt isteği alındı. Admin onayı bekleniyor." });
    }
}

public class CreateRegistrationRequest
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
}
