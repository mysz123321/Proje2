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
            new Permission { Name = "User.Manage", Description = "Kullanıcı Kayıtlarını Onaylayabilir/Yönetebilir" },
            new Permission { Name = "Tag.Manage", Description = "Etiketleri Yönetebilir" }
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
    }
}