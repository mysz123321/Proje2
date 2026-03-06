using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Infrastructure.Data;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Sadece giriş yapmış kullanıcılar menüleri çekebilir
public class UiController : ControllerBase
{
    private readonly AppDbContext _db;

    public UiController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("sidebar-items")]
    public async Task<IActionResult> GetSidebarItems()
    {
        // 1. Token'dan giriş yapmış kullanıcının ID'sini alıyoruz
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                          User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized("Kullanıcı kimliği doğrulanamadı.");

        // 2. Kullanıcının en güncel yetkilerini veritabanından çekiyoruz
        var user = await _db.Users
            .AsNoTracking()
            .Include(x => x.Roles)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            return NotFound("Kullanıcı bulunamadı.");

        var userPermissions = user.Roles
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        // 3. Veritabanından tüm Sidebar (Menü) elemanlarını sırasına (OrderIndex) göre çekiyoruz
        var allSidebarItems = await _db.SidebarItems
            .AsNoTracking()
            .OrderBy(x => x.OrderIndex)
            .ToListAsync();

        // 4. FİLTRELEME: Sadece kullanıcının görmeye yetkisi olduğu menüleri seçiyoruz
        var authorizedItems = allSidebarItems.Where(item =>
            string.IsNullOrEmpty(item.RequiredPermission) || // RequiredPermission null/boş ise herkese açıktır
            userPermissions.Contains(item.RequiredPermission) // Doluysa kullanıcının o yetkiye sahip olması gerekir
        ).ToList();

        // 5. Filtrelenmiş listeyi frontend'e gönderiyoruz
        return Ok(authorizedItems);
    }

    [HttpGet("my-permissions")]
    public async Task<IActionResult> GetMyPermissions()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                          User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized("Kullanıcı kimliği doğrulanamadı.");

        var user = await _db.Users
            .AsNoTracking()
            .Include(x => x.Roles)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            return NotFound("Kullanıcı bulunamadı.");

        // Kullanıcının anlık, canlı yetkilerini çekiyoruz
        var livePermissions = user.Roles
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        return Ok(livePermissions);
    }
}