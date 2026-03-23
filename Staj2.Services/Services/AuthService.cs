using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;
using Staj2.Services.Models.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Staj2.Services.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<(bool IsSuccess, string? ErrorMessage, object? Data)> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .Include(x => x.Roles)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return (false, "Giriş bilgileri hatalı", null);

        var token = CreateJwtToken(user);

        var userPermissions = user.Roles
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        var data = new
        {
            token,
            user.Username,
            roles = user.Roles.Select(r => r.Name).ToList(),
            permissions = userPermissions
        };

        return (true, null, data);
    }

    public async Task<(bool IsSuccess, string? ErrorMessage, bool isConflict)> SetPasswordAsync(SetPasswordRequest req)
    {
        var tokenHash = Sha256(req.Token);

        var tokenRow = await _db.PasswordSetupTokens
            .Include(x => x.RegistrationRequest)
            .FirstOrDefaultAsync(x =>
                x.TokenHash == tokenHash &&
                !x.IsUsed &&
                x.ExpiresAt > DateTime.UtcNow);

        if (tokenRow == null)
            return (false, "Token geçersiz veya süresi dolmuş.", false);

        var rr = tokenRow.RegistrationRequest;

        if (!string.Equals(rr.Email, req.Email.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(rr.Username, req.Username.Trim(), StringComparison.Ordinal))
        {
            return (false, "Email veya kullanıcı adı eşleşmiyor.", false);
        }

        var exists = await _db.Users.AnyAsync(u => u.Email == rr.Email || u.Username == rr.Username);
        if (exists) return (false, "Kullanıcı zaten oluşturulmuş.", true); // isConflict = true

        var hash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

        var newUser = new User
        {
            Username = rr.Username,
            Email = rr.Email,
            PasswordHash = hash,
            IsApproved = true
        };

        var requestedRole = await _db.Roles.FindAsync(rr.RequestedRoleId);
        if (requestedRole != null)
        {
            newUser.Roles.Add(requestedRole);
        }

        _db.Users.Add(newUser);

        tokenRow.IsUsed = true;
        tokenRow.UsedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, null, false);
    }

    public async Task<(bool IsSuccess, string? ErrorMessage, List<string>? Permissions)> GetMyPermissionsAsync(int userId)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(x => x.Roles)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            return (false, "Kullanıcı bulunamadı.", null);

        var currentPermissions = user.Roles
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        return (true, null, currentPermissions);
    }

    // --- HELPER METOTLAR (Sadece bu serviste kullanıldığı için private yapıldı) ---
    private static string Sha256(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(input));
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

        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role.Name));

        var userPermissions = user.Roles
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        foreach (var permission in userPermissions)
            claims.Add(new Claim("Permission", permission));

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