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
                new Role { Name = "Yönetici", CreatedAt = DateTime.UtcNow },
                new Role { Name = "Denetleyici", CreatedAt = DateTime.UtcNow },
                new Role { Name = "Görüntüleyici", CreatedAt = DateTime.UtcNow }
            });
            await context.SaveChangesAsync();
        }

        // 2. Admin Kullanıcısını Ekle
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
    }
}