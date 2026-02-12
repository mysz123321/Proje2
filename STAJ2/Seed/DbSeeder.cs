using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;

namespace STAJ2.Seed;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        // Rol ID 1 = Yönetici
        var adminExists = await db.Users.AnyAsync(u => u.Email == "admin@company.com");
        if (adminExists) return;

        // Geçici şifre: Admin123! (sonra değiştirirsin)
        var hash = BCrypt.Net.BCrypt.HashPassword("Admin123!");

        db.Users.Add(new User
        {
            Username = "admin",
            Email = "admin@company.com",
            PasswordHash = hash,
            IsApproved = true,
            RoleId = 1
        });

        await db.SaveChangesAsync();
    }
}
