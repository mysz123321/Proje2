using Microsoft.AspNetCore.Authorization;

namespace STAJ2.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // Kullanıcının token'ındaki "Permission" claim'lerini kontrol et
        var hasPermission = context.User.Claims.Any(c =>
            c.Type == "Permission" &&
            c.Value == requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement); // Yetkisi var, işleme izin ver
        }

        return Task.CompletedTask;
    }
}