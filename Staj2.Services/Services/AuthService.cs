using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;
using Staj2.Services.Models;
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

    public async Task<ServiceResult<object>> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .Include(x => x.Roles).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ServiceResult<object>.Failure("Giriş bilgileri hatalı");

        var accessToken = CreateJwtToken(user);

        // Refresh Token gün sayısını config'den çekiyoruz
        int refreshTokenDays = _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.Now.AddDays(refreshTokenDays),
            UserId = user.Id
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        var data = new
        {
            token = accessToken,
            refreshToken = refreshToken.Token,
            user.Username,
            permissions = user.Roles.SelectMany(r => r.RolePermissions).Select(rp => rp.Permission.Name).Distinct().ToList()
        };

        return ServiceResult<object>.Success(data, "Giriş başarılı.");
    }

    public async Task<ServiceResult> SetPasswordAsync(SetPasswordRequest req)
    {
        var tokenHash = Sha256(req.Token);

        var tokenRow = await _db.PasswordSetupTokens
            .Include(x => x.RegistrationRequest)
            .FirstOrDefaultAsync(x =>
                x.TokenHash == tokenHash &&
                !x.IsUsed &&
                x.ExpiresAt > DateTime.Now);

        if (tokenRow == null)
            return ServiceResult.Failure("Token geçersiz veya süresi dolmuş.");

        var rr = tokenRow.RegistrationRequest;

        if (!string.Equals(rr.Email, req.Email.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(rr.Username, req.Username.Trim(), StringComparison.Ordinal))
        {
            return ServiceResult.Failure("Email veya kullanıcı adı eşleşmiyor.");
        }

        var exists = await _db.Users.AnyAsync(u => u.Email == rr.Email || u.Username == rr.Username);
        if (exists)
            return ServiceResult.Failure("Kullanıcı zaten oluşturulmuş.");

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

        return ServiceResult.Success("Şifre başarıyla oluşturuldu ve hesabınız aktif edildi.");
    }

    public async Task<ServiceResult<List<string>>> GetMyPermissionsAsync(int userId)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(x => x.Roles)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            return ServiceResult<List<string>>.Failure("Kullanıcı bulunamadı.");

        var currentPermissions = user.Roles
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        return ServiceResult<List<string>>.Success(currentPermissions);
    }

    public async Task<ServiceResult<object>> RefreshTokenAsync(string token)
    {
        var storedToken = await _db.RefreshTokens
            .Include(x => x.User)
                .ThenInclude(u => u.Roles)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(x => x.Token == token && !x.IsRevoked && x.ExpiresAt > DateTime.Now);

        if (storedToken == null)
            return ServiceResult<object>.Failure("Geçersiz veya süresi dolmuş token.");

        storedToken.IsRevoked = true;

        var newAccessToken = CreateJwtToken(storedToken.User);
        var newRefreshToken = Guid.NewGuid().ToString("N");

        // Yeni refresh token oluşturulurken config kullanıyoruz.
        int refreshTokenDays = _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            ExpiresAt = DateTime.Now.AddDays(refreshTokenDays),
            UserId = storedToken.UserId
        });

        await _db.SaveChangesAsync();

        var data = new
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken
        };

        return ServiceResult<object>.Success(data, "Token başarıyla yenilendi.");
    }

    // --- HELPER METOTLAR ---
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

        // Access token geçerlilik süresini config'den çekiyoruz.
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