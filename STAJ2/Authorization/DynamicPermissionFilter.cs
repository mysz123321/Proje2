// STAJ2/Authorization/DynamicPermissionFilter.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Staj2.Infrastructure.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace STAJ2.Authorization;

public class DynamicPermissionFilter : IAsyncAuthorizationFilter
{
    private readonly AppDbContext _db;

    public DynamicPermissionFilter(AppDbContext db)
    {
        _db = db;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // 1. İsteğin gittiği Endpoint'in üzerindeki HasPermission etiketini oku
        var endpoint = context.HttpContext.GetEndpoint();
        var permissionAttribute = endpoint?.Metadata.GetMetadata<HasPermissionAttribute>();

        // Eğer etiket yoksa veya AppPermissions.None verilmişse yetki kontrolüne gerek yok
        if (permissionAttribute == null || permissionAttribute.Permissions.Length == 0 || permissionAttribute.Permissions.Contains(AppPermissions.None))
        {
            return;
        }

        // 2. Enum değerlerini DB'deki string formatına çevir (Örn: User_Read -> "User.Read")
        string[] requiredPermissions = permissionAttribute.Permissions
            .Select(p => p.ToString().Replace("_", "."))
            .ToArray();

        // 3. Kullanıcı Giriş Kontrolü
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            // ESKİ KOD: context.Result = new UnauthorizedResult(); // 401
            // YENİ KOD: JSON formatında başlık ve mesaj dönüyoruz
            context.Result = new JsonResult(new
            {
                title = "Oturum Süresi Doldu",
                message = "Lütfen tekrar giriş yapınız."
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ??
                          user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            // ESKİ KOD: context.Result = new UnauthorizedResult();
            context.Result = new JsonResult(new
            {
                title = "Oturum Hatası",
                message = "Kullanıcı kimliği doğrulanamadı. Lütfen tekrar giriş yapınız."
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        // 4. Kullanıcının Yetkisini DB'den Kontrol Et (Dizideki yetkilerden HERHANGİ BİRİNE sahip olması yeterli)
        bool hasPermission = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .SelectMany(u => u.Roles)
            .SelectMany(r => r.RolePermissions)
            .AnyAsync(rp => requiredPermissions.Contains(rp.Permission.Name));

        if (!hasPermission)
        {
            // ESKİ KOD: context.Result = new ForbidResult(); // 403
            // YENİ KOD: JSON formatında başlık ve mesaj dönüyoruz
            context.Result = new JsonResult(new
            {
                title = "Yetkisiz Erişim",
                message = "Bu menüyü görüntülemek veya bu işlemi gerçekleştirmek için yetkiniz bulunmamaktadır."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}