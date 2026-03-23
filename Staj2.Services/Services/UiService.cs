using Microsoft.EntityFrameworkCore;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;

namespace Staj2.Services.Services;

public class UiService : IUiService
{
    private readonly AppDbContext _db;

    public UiService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool IsSuccess, string? ErrorMessage, object? Data)> GetSidebarItemsAsync(int userId)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(x => x.Roles)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            return (false, "Kullanıcı bulunamadı.", null);

        var userPermissions = user.Roles
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        var allSidebarItems = await _db.SidebarItems
            .Include(x => x.RequiredPermission) // EKLENDİ: İlişkili tabloyu dahil ediyoruz
            .AsNoTracking()
            .OrderBy(x => x.OrderIndex)
            .ToListAsync();

        var authorizedItems = allSidebarItems.Where(item =>
            item.RequiredPermissionId == null || // GÜNCELLENDİ: Artık Null kontrolünü ID üzerinden yapıyoruz
            (item.RequiredPermission != null && userPermissions.Contains(item.RequiredPermission.Name)) // GÜNCELLENDİ: Nesnenin içindeki Name değerini kontrol ediyoruz
        ).ToList();

        return (true, null, authorizedItems);
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

        var livePermissions = user.Roles
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        return (true, null, livePermissions);
    }

    public async Task<object> GetUserActionsAsync()
    {
        return await _db.UserTableActions.OrderBy(a => a.OrderIndex).ToListAsync();
    }
}