using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Staj2.Services.Interfaces;

namespace STAJ2.Authorization
{
    public class DynamicPermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly IEndpointPermissionService _permissionService;

        public DynamicPermissionFilter(IEndpointPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // 1. İstek Hangi Controller ve Action'a (Fonksiyona) gidiyor bul
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            if (string.IsNullOrEmpty(controllerName) || string.IsNullOrEmpty(actionName))
                return;

            // 2. Bu fonksiyon için DB'de tanımlı bir izin var mı bak
            var requiredPermission = await _permissionService.GetRequiredPermissionAsync(controllerName, actionName);

            // Veritabanında kural yoksa serbest bırak (Herkes girebilir)
            if (string.IsNullOrEmpty(requiredPermission))
                return;

            // 3. Kullanıcı Login olmuş mu?
            var user = context.HttpContext.User;
            if (user == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult(); // 401 Giriş Yapılmamış
                return;
            }

            // 4. Kullanıcının Token'ından gelen claim'ler (yetkiler) arasında aranan yetki var mı?
            // Virgülle ayrılmış yetkileri listeye çevir (Örn: "User.ManageComputers", "Tag.Manage")
            var requiredPermissionsList = requiredPermission.Split(',')
                                                            .Select(p => p.Trim())
                                                            .ToList();

            // Kullanıcının claimleri (yetkileri) içinde, bu listedeki yetkilerden herhangi biri var mı bak
            var hasPermission = user.Claims.Any(c => requiredPermissionsList.Contains(c.Value));

            if (!hasPermission)
            {
                context.Result = new ForbidResult(); // 403 Yetki Yok
            }
        }
    }
}