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

        // 1. Registry'den bu endpoint için gereken Enum yetkisini bul
        AppPermissions? requiredPermissionEnum = EndpointPermissionRegistry.GetRequiredPermission(controllerName, actionName);

        // Eğer bu endpoint için bir yetki tanımlanmamışsa veya None ise geçişe izin ver
        if (requiredPermissionEnum == null || requiredPermissionEnum == AppPermissions.None)
        {
            return;
        }

        // 2. Enum değerini veritabanındaki string formata çevir (Örn: Computer_Delete -> Computer.Delete)
        string requiredPermissionString = requiredPermissionEnum.ToString().Replace("_", ".");

        // 3. Kullanıcı Giriş Kontrolü
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

        // 4. Kullanıcının Yetkisini DB'den Kontrol Et
        bool hasPermission = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .SelectMany(u => u.Roles)
            .SelectMany(r => r.RolePermissions)
            .AnyAsync(rp => rp.Permission.Name == requiredPermissionString);

        if (!hasPermission)
        {
            context.Result = new ForbidResult(); // 403
        }
    }
}