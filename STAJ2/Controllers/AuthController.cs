using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using STAJ2.Models.Auth;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users
            .Include(x => x.Roles) // Çoklu rol desteği için Roles eklendi
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Giriş bilgileri hatalı");

        var token = CreateJwtToken(user);

        return Ok(new
        {
            token,
            user.Username,
            // Frontend'e tüm rolleri liste olarak dönüyoruz
            roles = user.Roles.Select(r => r.Name).ToList()
        });
    }

    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest req)
    {
        var tokenHash = Sha256(req.Token);

        var tokenRow = await _db.PasswordSetupTokens
            .Include(x => x.RegistrationRequest)
            .FirstOrDefaultAsync(x =>
                x.TokenHash == tokenHash &&
                !x.IsUsed &&
                x.ExpiresAt > DateTime.UtcNow);

        if (tokenRow == null)
            return BadRequest("Token geçersiz veya süresi dolmuş.");

        var rr = tokenRow.RegistrationRequest;

        if (!string.Equals(rr.Email, req.Email.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(rr.Username, req.Username.Trim(), StringComparison.Ordinal))
        {
            return BadRequest("Email veya kullanıcı adı eşleşmiyor.");
        }

        var exists = await _db.Users.AnyAsync(u => u.Email == rr.Email || u.Username == rr.Username);
        if (exists) return Conflict("Kullanıcı zaten oluşturulmuş.");

        var hash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

        var newUser = new User
        {
            Username = rr.Username,
            Email = rr.Email,
            PasswordHash = hash,
            IsApproved = true
        };

        // Kayıt isteğindeki rolü bulup listeye ekliyoruz
        var requestedRole = await _db.Roles.FindAsync(rr.RequestedRoleId);
        if (requestedRole != null)
        {
            newUser.Roles.Add(requestedRole);
        }

        _db.Users.Add(newUser);
        // -------------------------------

        tokenRow.IsUsed = true;
        tokenRow.UsedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Şifre oluşturuldu. Giriş yapabilirsiniz." });
    }

    private static string Sha256(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private string CreateJwtToken(User user)
    {
        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username)
        };

        // Tüm rolleri token içerisine ClaimTypes.Role olarak ekle
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
        }

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}