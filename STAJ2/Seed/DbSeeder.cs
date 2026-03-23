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

        await context.Database.MigrateAsync();

        // 1. Rolleri Ekle
        if (!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(new List<Role> {
                new Role { Name = "Yönetici", CreatedAt = DateTime.Now },
                new Role { Name = "Denetleyici", CreatedAt = DateTime.Now },
                new Role { Name = "Görüntüleyici", CreatedAt = DateTime.Now }
            });
            await context.SaveChangesAsync();
        }

        // --- 2. SİSTEM YETKİLERİNİ (PERMISSIONS) EKLE ---
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
    
            // --- KULLANICI YÖNETİMİ YETKİLERİ ---
            new Permission { Name = "User.Manage", Description = "Kullanıcı Kayıtlarını Onaylayabilir/Yönetebilir" }, // MEVCUT YETKİ - KORUNDU
            new Permission { Name = "User.Read", Description = "Kullanıcıları Listeler (Görüntüleme)" }, // YENİ EKLENDİ
            new Permission { Name = "User.ManageRoles", Description = "Kullanıcı Rollerini Değiştirebilir" }, // YENİ EKLENDİ
            new Permission { Name = "User.ManageComputers", Description = "Kullanıcının Cihaz Erişimlerini Değiştirebilir" }, // YENİ EKLENDİ
            new Permission { Name = "User.ManageTags", Description = "Kullanıcının Etiket Erişimlerini Değiştirebilir" } // YENİ EKLENDİ
        };

        foreach (var perm in defaultPermissions)
        {
            if (!await context.Permissions.AnyAsync(p => p.Name == perm.Name))
            {
                context.Permissions.Add(perm);
            }
        }
        await context.SaveChangesAsync(); // Yetkileri kaydet

        // 3. Admin Kullanıcısını Ekle
        if (!await context.Users.AnyAsync(u => u.Username == "admin"))
        {
            var adminRole = await context.Roles.FirstAsync(r => r.Name == "Yönetici");
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@staj2.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                IsApproved = true
            };

            adminUser.Roles.Add(adminRole); // Listeye ekliyoruz
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            Console.WriteLine(">>> Admin kullanıcısı (admin / Admin123!) oluşturuldu.");
        }

        // --- 4. YÖNETİCİ ROLÜNE TÜM YETKİLERİ OTOMATİK ATA ---
        var adminRoleForPerms = await context.Roles
            .Include(r => r.RolePermissions)
            .FirstAsync(r => r.Name == "Yönetici");

        var allPermissions = await context.Permissions.ToListAsync();

        bool permissionsAdded = false;
        foreach (var perm in allPermissions)
        {
            // Eğer Yönetici rolünde bu yetki yoksa, ekle
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

        if (!await context.SidebarItems.AnyAsync())
        {
            var sidebarItems = new List<SidebarItem>
            {
                // Herkese açık menüler (RequiredPermissionId = null)
                new SidebarItem { Title = "Canlı İzleme", Icon = "bi bi-activity text-success", TargetView = "computers", RequiredPermissionId = null, OrderIndex = 1 },
                new SidebarItem { Title = "Tüm Bilgisayarlar", Icon = "bi bi-pc-display", TargetView = "all-computers", RequiredPermissionId = null, OrderIndex = 2 },
    
                // YÖNETİM MENÜLERİ
                // Kayıt istekleri User.Manage yetkisinde kalıyor
                new SidebarItem { Title = "Kayıt İstekleri", Icon = "bi bi-envelope-paper", TargetView = "requests", RequiredPermissionId = allPermissions.FirstOrDefault(p => p.Name == "User.Manage")?.Id, OrderIndex = 3 },
    
                // Kullanıcılar menüsü artık sadece User.Read yetkisi ile listelenecek
                new SidebarItem { Title = "Kullanıcılar", Icon = "bi bi-people", TargetView = "users", RequiredPermissionId = allPermissions.FirstOrDefault(p => p.Name == "User.Read")?.Id, OrderIndex = 4 },

                new SidebarItem { Title = "Roller ve Yetkiler", Icon = "bi bi-shield-lock", TargetView = "roles", RequiredPermissionId = allPermissions.FirstOrDefault(p => p.Name == "Role.Manage")?.Id, OrderIndex = 5 },
                new SidebarItem { Title = "Etiketler", Icon = "bi bi-tags", TargetView = "tags", RequiredPermissionId = allPermissions.FirstOrDefault(p => p.Name == "Tag.Manage")?.Id, OrderIndex = 6 }
            };

            context.SidebarItems.AddRange(sidebarItems);
            await context.SaveChangesAsync();
            Console.WriteLine(">>> Dinamik Sidebar menü elemanları oluşturuldu.");
        }

        // --- SİDEBAR YETKİ DÜZELTMESİ (TÜM MENÜLER İÇİN GENEL KONTROL) ---
        var existingSidebarItems = await context.SidebarItems.ToListAsync();
        bool isSidebarUpdated = false;

        // Hangi hedefin (TargetView) hangi yetkiyi (Permission Name) gerektirdiğini eşleştiriyoruz
        var sidebarMappings = new Dictionary<string, string>
        {
            { "requests", "User.Manage" },
            { "users", "User.Read" },
            { "roles", "Role.Manage" },
            { "tags", "Tag.Manage" }
        };

        foreach (var mapping in sidebarMappings)
        {
            var sidebarItem = existingSidebarItems.FirstOrDefault(s => s.TargetView == mapping.Key);
            if (sidebarItem != null)
            {
                var expectedPermissionId = allPermissions.FirstOrDefault(p => p.Name == mapping.Value)?.Id;

                // Eğer veritabanındaki ID, olması gereken ID'den farklıysa (veya null ise) güncelle
                if (sidebarItem.RequiredPermissionId != expectedPermissionId)
                {
                    sidebarItem.RequiredPermissionId = expectedPermissionId;
                    isSidebarUpdated = true;
                }
            }
        }

        if (isSidebarUpdated)
        {
            await context.SaveChangesAsync();
            Console.WriteLine(">>> Tüm Sidebar menülerinin yetki ID'leri eşitlendi/güncellendi.");
        }
        // --- DİNAMİK KULLANICI TABLOSU BUTONLARI ---
        if (!await context.UserTableActions.AnyAsync())
        {
            context.UserTableActions.AddRange(new List<UserTableAction>
            {
                new UserTableAction { Title = "Roller", Icon = "bi bi-shield-check", ButtonClass = "btn-outline-primary", OnClickFunction = "ui.openUserRolesModal(USER_ID, 'USER_NAME')", RequiredPermission = "User.ManageRoles", OrderIndex = 1 },
                new UserTableAction { Title = "Cihazlar", Icon = "bi bi-pc-display", ButtonClass = "btn-outline-success", OnClickFunction = "ui.openUserComputerAccessModal(USER_ID, 'USER_NAME')", RequiredPermission = "User.ManageComputers", OrderIndex = 2 },
                new UserTableAction { Title = "Etiketler", Icon = "bi bi-tags", ButtonClass = "btn-outline-warning", OnClickFunction = "ui.openUserTagAccessModal(USER_ID, 'USER_NAME')", RequiredPermission = "User.ManageTags", OrderIndex = 3 },
                // HideFromAdmin kaldırıldı
                new UserTableAction { Title = "Sil", Icon = "bi bi-trash", ButtonClass = "btn-outline-danger", OnClickFunction = "ui.deleteUser(USER_ID)", RequiredPermission = "User.ManageRoles", OrderIndex = 4 }
            });
            await context.SaveChangesAsync();
            Console.WriteLine(">>> Dinamik Kullanıcı Tablosu Butonları oluşturuldu.");
        }
    }
}