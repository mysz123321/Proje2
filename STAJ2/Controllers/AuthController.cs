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
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null)
            return Unauthorized("Kullanıcı bulunamadı");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Şifre hatalı");

        // Token üret
        var token = CreateJwtToken(user);

        return Ok(new
        {
            token,
            user.Username,
            user.Email,
            role = user.Role.Name
        });
    }
    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest req)
    {
        // token hash
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

        // ekstra güvenlik: email + username eşleşsin
        if (!string.Equals(rr.Email, req.Email.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(rr.Username, req.Username.Trim(), StringComparison.Ordinal))
        {
            return BadRequest("Email veya kullanıcı adı eşleşmiyor.");
        }

        // Kullanıcı zaten var mı?
        var exists = await _db.Users.AnyAsync(u => u.Email == rr.Email || u.Username == rr.Username);
        if (exists) return Conflict("Kullanıcı zaten oluşturulmuş.");

        var hash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

        _db.Users.Add(new User
        {
            Username = rr.Username,
            Email = rr.Email,
            PasswordHash = hash,
            IsApproved = true,
            RoleId = rr.RequestedRoleId
        });

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

        var keyBytes = Encoding.UTF8.GetBytes(jwt["Key"]!);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.Name)
        };

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

