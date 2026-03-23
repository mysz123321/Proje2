using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Staj2.Infrastructure.Data;
using System.Security.Claims;

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
        var actionDescriptor = context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
        if (actionDescriptor == null) return;

        string controllerName = actionDescriptor.ControllerName;
        string actionName = actionDescriptor.ActionName;

        // 1. Registry'den bu endpoint için gereken yetkileri string dizisi olarak al
        string[]? requiredPermissions = EndpointPermissionRegistry.GetRequiredPermissions(controllerName, actionName);

        // Eğer bu endpoint için bir yetki tanımlanmamışsa geçişe izin ver
        if (requiredPermissions == null || requiredPermissions.Length == 0)
        {
            return;
        }

        // 2. Kullanıcı Giriş Kontrolü
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult(); // 401
            return;
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ??
                          user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // 3. Kullanıcının Yetkisini DB'den Kontrol Et (Dizideki yetkilerden HERHANGİ BİRİNE sahip olması yeterli)
        bool hasPermission = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .SelectMany(u => u.Roles)
            .SelectMany(r => r.RolePermissions)
            .AnyAsync(rp => requiredPermissions.Contains(rp.Permission.Name));

        if (!hasPermission)
        {
            context.Result = new ForbidResult(); // 403
        }
    }
}