// STAJ2/Seed/DbSeeder.cs
using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;

namespace STAJ2.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var adminRoleName = config["AppDefaults:AdminRoleName"] ?? "Yönetici";

        await context.Database.MigrateAsync();

        // 1. Rolleri Ekle
        if (!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(new List<Role> {
                new Role { Name = adminRoleName, CreatedAt = DateTime.Now }, // Değişti
                new Role { Name = "Denetleyici", CreatedAt = DateTime.Now },
                new Role { Name = "Görüntüleyici", CreatedAt = DateTime.Now }
            });
            await context.SaveChangesAsync();
        }

        // --- 2. SİSTEM YETKİLERİNİ (PERMISSIONS) EKLE (Description KALDIRILDI) ---
        var defaultPermissions = new List<Permission>
        {
            new Permission { Name = "Computer.Read", Description = "Cihazları Görüntüleyebilir" },
            new Permission { Name = "Computer.Delete", Description = "Cihaz Silebilir" },
            new Permission { Name = "Computer.Rename", Description = "Cihaz İsmi Değiştirebilir" },
            new Permission { Name = "Computer.SetThreshold", Description = "Cihaz Eşik Değeri Belirleyebilir" },
            new Permission { Name = "Computer.AssignTag", Description = "Cihazlara Etiket Ekleyebilir ve Çıkarabilir" },
            new Permission { Name = "Computer.Filter", Description = "Cihazları ve Metrikleri Filtreleyebilir" },
            new Permission { Name = "Role.Manage", Description = "Sistem Rollerini ve Yetkilerini Yönetebilir" },
            new Permission { Name = "Tag.Manage", Description = "Etiketleri Yönetebilir" },
            new Permission { Name = "User.Manage", Description = "Kullanıcı Kayıtlarını Onaylayabilir/Yönetebilir" },
            new Permission { Name = "User.Read", Description = "Kullanıcıları Listeler (Görüntüleme)" },
            new Permission { Name = "User.ManageRoles", Description = "Kullanıcı Rollerini Değiştirebilir" },
            new Permission { Name = "User.ManageComputers", Description = "Kullanıcının Cihaz Erişimlerini Değiştirebilir" },
            new Permission { Name = "User.ManageTags", Description = "Kullanıcının Etiket Erişimlerini Değiştirebilir" }
        };

        foreach (var perm in defaultPermissions)
        {
            if (!await context.Permissions.AnyAsync(p => p.Name == perm.Name))
            {
                context.Permissions.Add(perm);
            }
        }
        await context.SaveChangesAsync(); // Yetkileri kaydet

        //// 3. Admin Kullanıcısını Ekle
        //if (!await context.Users.AnyAsync(u => u.Username == "admin"))
        //{
        //    var adminRole = await context.Roles.FirstAsync(r => r.Name == adminRoleName); // Değişti
        //    var adminUser = new User
        //    {
        //        Username = "admin",
        //        Email = "admin@staj2.com",
        //        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
        //        IsApproved = true
        //    };

        //    adminUser.Roles.Add(adminRole);
        //    context.Users.Add(adminUser);
        //    await context.SaveChangesAsync();
        //    Console.WriteLine(">>> Admin kullanıcısı (admin / Admin123!) oluşturuldu.");
        //}

        // --- 4. YÖNETİCİ ROLÜNE TÜM YETKİLERİ OTOMATİK ATA ---
        var adminRoleForPerms = await context.Roles
     .Include(r => r.RolePermissions)
     .FirstAsync(r => r.Name == adminRoleName);

        var allPermissions = await context.Permissions.ToListAsync();

        bool permissionsAdded = false;
        foreach (var perm in allPermissions)
        {
            if (!adminRoleForPerms.RolePermissions.Any(rp => rp.PermissionId == perm.Id))
            {
                adminRoleForPerms.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRoleForPerms.Id,
                    PermissionId = perm.Id
                });
                permissionsAdded = true;
            }
        }

        if (permissionsAdded)
        {
            await context.SaveChangesAsync();
            Console.WriteLine(">>> Yönetici rolüne tüm sistem yetkileri (Permissions) atandı.");
        }

        // --- 5. MENÜLERİ OLUŞTUR (RequiredPermissionId ALANI SİLİNDİ) ---
        if (!await context.SidebarItems.AnyAsync())
        {
            var sidebarItems = new List<SidebarItem>
            {
                new SidebarItem { Title = "Canlı İzleme", Icon = "bi bi-activity text-success", TargetView = "computers", OrderIndex = 1 },
                new SidebarItem { Title = "Tüm Bilgisayarlar", Icon = "bi bi-pc-display", TargetView = "all-computers", OrderIndex = 2 },
                new SidebarItem { Title = "Kayıt İstekleri", Icon = "bi bi-envelope-paper", TargetView = "requests", OrderIndex = 3 },
                new SidebarItem { Title = "Kullanıcılar", Icon = "bi bi-people", TargetView = "users", OrderIndex = 4 },
                new SidebarItem { Title = "Roller ve Yetkiler", Icon = "bi bi-shield-lock", TargetView = "roles", OrderIndex = 5 },
                new SidebarItem { Title = "Etiketler", Icon = "bi bi-tags", TargetView = "tags", OrderIndex = 6 },
                new SidebarItem { Title = "Raporlar", Icon = "bi bi-graph-up-arrow text-info",TargetView = "reports", OrderIndex = 7}
            };

            context.SidebarItems.AddRange(sidebarItems);
            await context.SaveChangesAsync();
            Console.WriteLine(">>> Dinamik Sidebar menü elemanları oluşturuldu.");
        }

        // --- 6. TERSİNE İLİŞKİ: HANGİ YETKİ HANGİ MENÜYÜ AÇAR? ---
        var existingSidebarItems = await context.SidebarItems.ToListAsync();
        bool isSidebarUpdated = false;

        // Yetki Adı -> Açacağı Menünün TargetView'i
        var permissionToSidebarMappings = new Dictionary<string, string>
{
    { "User.Manage", "requests" },
    
    // --- KULLANICILAR MENÜSÜNÜ AÇACAK YETKİLER ---
    { "User.Read", "users" },
    { "User.ManageRoles", "users" },      // YENİ EKLENDİ
    { "User.ManageComputers", "users" },  // YENİ EKLENDİ
    { "User.ManageTags", "users" },       // YENİ EKLENDİ

    { "Role.Manage", "roles" },
    { "Tag.Manage", "tags" }
};

        foreach (var mapping in permissionToSidebarMappings)
        {
            var permission = allPermissions.FirstOrDefault(p => p.Name == mapping.Key);
            var targetMenu = existingSidebarItems.FirstOrDefault(s => s.TargetView == mapping.Value);

            if (permission != null && targetMenu != null)
            {
                if (permission.SidebarItemId != targetMenu.Id)
                {
                    permission.SidebarItemId = targetMenu.Id;
                    isSidebarUpdated = true;
                }
            }
        }

        if (isSidebarUpdated)
        {
            await context.SaveChangesAsync();
            Console.WriteLine(">>> Yetkiler (Permissions) başarıyla ilgili Sidebar menülerine bağlandı.");
        }

        // --- 7. DİNAMİK KULLANICI TABLOSU BUTONLARI ---
        //if (!await context.UserTableActions.AnyAsync())
        //{
        //    context.UserTableActions.AddRange(new List<UserTableAction>
        //    {
        //        new UserTableAction { Title = "Roller", Icon = "bi bi-shield-check", ButtonClass = "btn-outline-primary", OnClickFunction = "ui.openUserRolesModal(USER_ID, 'USER_NAME')", RequiredPermission = "User.ManageRoles", OrderIndex = 1 },
        //        new UserTableAction { Title = "Cihazlar", Icon = "bi bi-pc-display", ButtonClass = "btn-outline-success", OnClickFunction = "ui.openUserComputerAccessModal(USER_ID, 'USER_NAME')", RequiredPermission = "User.ManageComputers", OrderIndex = 2 },
        //        new UserTableAction { Title = "Etiketler", Icon = "bi bi-tags", ButtonClass = "btn-outline-warning", OnClickFunction = "ui.openUserTagAccessModal(USER_ID, 'USER_NAME')", RequiredPermission = "User.ManageTags", OrderIndex = 3 },
        //        new UserTableAction { Title = "Sil", Icon = "bi bi-trash", ButtonClass = "btn-outline-danger", OnClickFunction = "ui.deleteUser(USER_ID)", RequiredPermission = "User.ManageRoles", OrderIndex = 4 }
        //    });
        //    await context.SaveChangesAsync();
        //    Console.WriteLine(">>> Dinamik Kullanıcı Tablosu Butonları oluşturuldu.");
        //}
    }
}