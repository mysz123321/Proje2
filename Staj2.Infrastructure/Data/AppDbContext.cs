using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;

namespace Staj2.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Auth Tabloları
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRegistrationRequest> UserRegistrationRequests => Set<UserRegistrationRequest>();
    public DbSet<PasswordSetupToken> PasswordSetupTokens => Set<PasswordSetupToken>();

    // --- YENİ EKLENENLER ---
    public DbSet<Computer> Computers { get; set; }
    public DbSet<ComputerMetric> ComputerMetrics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Computer & Metric İlişkisi
        modelBuilder.Entity<ComputerMetric>()
            .HasOne(m => m.Computer)
            .WithMany(c => c.Metrics)
            .HasForeignKey(m => m.ComputerId)
            .OnDelete(DeleteBehavior.Cascade);

        // PasswordSetupToken
        modelBuilder.Entity<PasswordSetupToken>(e =>
        {
            e.Property(x => x.TokenHash).HasMaxLength(64);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasOne(x => x.RegistrationRequest)
             .WithMany(r => r.PasswordSetupTokens)
             .HasForeignKey(x => x.RegistrationRequestId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Role
        modelBuilder.Entity<Role>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(50);
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Username).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.PasswordHash).HasMaxLength(255);
            e.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        // RegistrationRequest
        modelBuilder.Entity<UserRegistrationRequest>(e =>
        {
            e.Property(x => x.Username).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(255);
            e.HasIndex(x => x.Email).IsUnique().HasFilter("[Status] = 0");
            e.HasIndex(x => x.Username).IsUnique().HasFilter("[Status] = 0");
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.RequestedRole).WithMany().HasForeignKey(x => x.RequestedRoleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ApprovedByUser).WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // Seed Roles
        var seedCreatedAt = new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Yönetici", CreatedAt = seedCreatedAt },
            new Role { Id = 2, Name = "Denetleyici", CreatedAt = seedCreatedAt },
            new Role { Id = 3, Name = "Görüntüleyici", CreatedAt = seedCreatedAt }
        );
    }
}