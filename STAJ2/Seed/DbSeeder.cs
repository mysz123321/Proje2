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
            { "User.ManageRoles", "users" },
            { "User.ManageComputers", "users" },
            { "User.ManageTags", "users" },
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
        // (Yorum satırında bırakılmış)


        //// ======================================================================
        //// --- 9. YENİ: ID=8 BİLGİSAYARI İÇİN MART AYI RAPORLAMA TEST VERİSİ ---
        //// ======================================================================

        //int targetId = 8;
        //var testComp = await context.Computers.FindAsync(targetId);

        //if (testComp != null)
        //{
        //    // Tekrar tekrar çalıştırıldığında verilerin üst üste binmemesi için eski test verilerini temizliyoruz
        //    var oldHistories = context.ComputerThresholdHistories.Where(h => h.ComputerId == targetId);
        //    context.ComputerThresholdHistories.RemoveRange(oldHistories);

        //    var oldMetrics = context.ComputerMetrics.Where(m => m.ComputerId == targetId && m.CreatedAt >= new DateTime(2026, 3, 1) && m.CreatedAt <= new DateTime(2026, 3, 31, 23, 59, 59));
        //    context.ComputerMetrics.RemoveRange(oldMetrics);

        //    await context.SaveChangesAsync();

        //    // 1. Eşik Değeri Geçiş Senaryosunu Giriyoruz
        //    var marchHistories = new List<ComputerThresholdHistory>
        //    {
        //        new ComputerThresholdHistory { ComputerId = targetId, CpuThreshold = 70, RamThreshold = 80, ActiveFrom = new DateTime(2026, 3, 1, 0, 0, 0), CreatedAt = DateTime.Now },
        //        new ComputerThresholdHistory { ComputerId = targetId, CpuThreshold = 80, RamThreshold = 80, ActiveFrom = new DateTime(2026, 3, 15, 0, 0, 0), CreatedAt = DateTime.Now },
        //        new ComputerThresholdHistory { ComputerId = targetId, CpuThreshold = 45, RamThreshold = 80, ActiveFrom = new DateTime(2026, 3, 25, 0, 0, 0), CreatedAt = DateTime.Now },
        //        // Güncel (Şu Anki) Değerler - 14 Nisan
        //        new ComputerThresholdHistory { ComputerId = targetId, CpuThreshold = 50, RamThreshold = 75, ActiveFrom = new DateTime(2026, 4, 14, 12, 0, 0), CreatedAt = DateTime.Now }
        //    };
        //    context.ComputerThresholdHistories.AddRange(marchHistories);

        //    // Cihazın kendi güncel değerlerini de 14 Nisan değerleriyle güncelliyoruz
        //    testComp.CpuThreshold = 50;
        //    testComp.RamThreshold = 75;

        //    // 2. Metrik Verilerini Giriyoruz (Mart ayı boyunca saat başı veri üretecek)
        //    DateTime current = new DateTime(2026, 3, 1, 0, 0, 0);
        //    DateTime end = new DateTime(2026, 3, 31, 23, 59, 59);
        //    var testMetrics = new List<ComputerMetric>();

        //    while (current <= end)
        //    {
        //        double cpuValue = 0;

        //        // Senin Senaryon:
        //        // 1-15 Mart (Eşik 70) -> CPU'yu 65 yap (Eşik altı, başarılı)
        //        if (current < new DateTime(2026, 3, 15)) cpuValue = 65;

        //        // 15-25 Mart (Eşik 80) -> CPU'yu 85 yap (Eşik ÜSTÜ, sorunlu)
        //        else if (current < new DateTime(2026, 3, 25)) cpuValue = 85;

        //        // 25-31 Mart (Eşik 45) -> CPU'yu 40 yap (Eşik altı, başarılı)
        //        else cpuValue = 40;

        //        testMetrics.Add(new ComputerMetric
        //        {
        //            ComputerId = targetId,
        //            CpuUsage = cpuValue,
        //            RamUsage = 60, // RAM'i sabit bıraktık, test için CPU odaklı gidiyoruz
        //            CreatedAt = current
        //        });

        //        current = current.AddMinutes(1); // YENİ HALİ (Dakikada bir veri üretsin)
        //    }

        //    context.ComputerMetrics.AddRange(testMetrics);
        //    await context.SaveChangesAsync();

        //    Console.WriteLine(">>> ID=8 için Tarihsel Eşik Değeri Analizi test verileri oluşturuldu.");
        //}
    }
}