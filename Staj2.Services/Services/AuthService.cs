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
            .Include(x => x.Roles).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return (false, "Giriş bilgileri hatalı", null);

        var accessToken = CreateJwtToken(user);

        // 1. DÜZENLEME: Refresh Token gün sayısını config'den çekiyoruz
        int refreshTokenDays = _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.Now.AddDays(refreshTokenDays),
            UserId = user.Id
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return (true, null, new
        {
            token = accessToken,
            refreshToken = refreshToken.Token,
            user.Username,
            permissions = user.Roles.SelectMany(r => r.RolePermissions).Select(rp => rp.Permission.Name).Distinct().ToList()
        });
    }

    public async Task<(bool IsSuccess, string? ErrorMessage, bool isConflict)> SetPasswordAsync(SetPasswordRequest req)
    {
        var tokenHash = Sha256(req.Token);

        var tokenRow = await _db.PasswordSetupTokens
            .Include(x => x.RegistrationRequest)
            .FirstOrDefaultAsync(x =>
                x.TokenHash == tokenHash &&
                !x.IsUsed &&
                x.ExpiresAt > DateTime.Now);

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
        tokenRow.UsedAt = DateTime.Now;

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

    // --- HELPER METOTLAR ---
    private static string Sha256(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    public async Task<(bool IsSuccess, string? Token, string? RefreshToken)> RefreshTokenAsync(string token)
    {
        var storedToken = await _db.RefreshTokens
            .Include(x => x.User)
                .ThenInclude(u => u.Roles)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(x => x.Token == token && !x.IsRevoked && x.ExpiresAt > DateTime.Now);

        if (storedToken == null) return (false, null, null);

        storedToken.IsRevoked = true;

        var newAccessToken = CreateJwtToken(storedToken.User);
        var newRefreshToken = Guid.NewGuid().ToString("N");

        // 2. DÜZENLEME: Burada da yeni refresh token oluşturulurken sabit 7 gün yerine config kullanıyoruz.
        int refreshTokenDays = _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            ExpiresAt = DateTime.Now.AddDays(refreshTokenDays),
            UserId = storedToken.UserId
        });

        await _db.SaveChangesAsync();
        return (true, newAccessToken, newRefreshToken);
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

        // 3. DÜZENLEME: Access token geçerlilik süresini 15 dakika sabiti yerine config'den çekiyoruz.
        int accessTokenMinutes = _config.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(accessTokenMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}