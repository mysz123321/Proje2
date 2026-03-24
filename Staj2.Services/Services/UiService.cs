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
        // 1. Kullanıcıyı ve rollerine bağlı yetkileri çekiyoruz
        var user = await _db.Users
            .AsNoTracking()
            .Include(x => x.Roles)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            return (false, "Kullanıcı bulunamadı.", null);

        // 2. Kullanıcının sahip olduğu yetkilerin açabildiği SidebarItem ID'lerini bir listeye alıyoruz
        var userAllowedSidebarItemIds = user.Roles
            .SelectMany(r => r.RolePermissions)
            .Where(rp => rp.Permission.SidebarItemId != null)
            .Select(rp => rp.Permission.SidebarItemId!.Value)
            .Distinct()
            .ToList();

        // 3. Veritabanındaki tüm korumalı (bir yetkiye bağlanmış) SidebarItem ID'lerini buluyoruz
        var allProtectedSidebarItemIds = await _db.Permissions
            .Where(p => p.SidebarItemId != null)
            .Select(p => p.SidebarItemId!.Value)
            .Distinct()
            .ToListAsync();

        // 4. Tüm menü ögelerini sırasına göre çekiyoruz
        var allSidebarItems = await _db.SidebarItems
            .AsNoTracking()
            .OrderBy(x => x.OrderIndex)
            .ToListAsync();

        // 5. Menüleri filtreliyoruz
        var authorizedItems = allSidebarItems.Where(item =>
            !allProtectedSidebarItemIds.Contains(item.Id) || // Herkese açık menüler
            userAllowedSidebarItemIds.Contains(item.Id)      // Kullanıcının yetkisinin olduğu menüler
        ).Select(item => new
        {
            item.Id,
            item.Title,
            item.Icon,
            item.TargetView,
            item.OrderIndex,
            // Eğer bu menünün ID'si korunan menüler listesindeyse true döner
            IsProtected = allProtectedSidebarItemIds.Contains(item.Id)
        }).ToList();

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