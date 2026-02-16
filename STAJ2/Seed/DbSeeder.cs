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

        // 1. Veritabanı ve Tabloların Oluştuğundan Emin Ol
        await context.Database.MigrateAsync();

        // 2. Rolleri Kontrol Et ve Yoksa Ekle (ID: 1, 2, 3)
        if (!await context.Roles.AnyAsync())
        {
            var roles = new List<Role>
            {
                new Role { Name = "Yönetici", CreatedAt = DateTime.UtcNow },
                new Role { Name = "Denetleyici", CreatedAt = DateTime.UtcNow },
                new Role { Name = "Görüntüleyici", CreatedAt = DateTime.UtcNow }
            };
            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();
        }

        // 3. Admin Kullanıcısını Kontrol Et
        var adminEmail = "admin@sirket.com";
        var hasAdmin = await context.Users.AnyAsync(u => u.Email == adminEmail);

        if (!hasAdmin)
        {
            // AuthController'daki gibi BCrypt kullanıyoruz
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");

            var adminUser = new User
            {
                Username = "admin1",
                Email = adminEmail,
                PasswordHash = passwordHash,
                IsApproved = true, // Kayıt isteğini pas geçip direkt onaylıyoruz
                RoleId = 1,        // Yönetici rolü
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            Console.WriteLine(">>> SİSTEM: Admin kullanıcısı (admin / 123456) otomatik oluşturuldu.");
        }
    }
}