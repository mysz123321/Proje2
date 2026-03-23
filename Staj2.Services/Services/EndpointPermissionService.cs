using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;

namespace Staj2.Services.Services
{
    public class EndpointPermissionService : IEndpointPermissionService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;

        public EndpointPermissionService(IServiceScopeFactory scopeFactory, IMemoryCache cache)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
        }

        public async Task<string> GetRequiredPermissionAsync(string controller, string action)
        {
            // Veriler Cache'de yoksa veritabanından çek (Performans için)
            if (!_cache.TryGetValue("EndpointPermissionsCache", out List<EndpointPermission> permissions))
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                permissions = await dbContext.EndpointPermissions.ToListAsync();

                // Veriyi 1 saat boyunca RAM'de tut
                _cache.Set("EndpointPermissionsCache", permissions, TimeSpan.FromHours(1));
            }

            // Gelen Controller ve Action adına göre gerekli yetkiyi bul
            var endpoint = permissions.FirstOrDefault(p =>
                p.ControllerName.Equals(controller, StringComparison.OrdinalIgnoreCase) &&
                p.ActionName.Equals(action, StringComparison.OrdinalIgnoreCase));

            return endpoint?.RequiredPermission;
        }
    }
}