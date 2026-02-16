using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;

namespace Staj2.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // TABLOLAR
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; } // <--- EKSİK OLAN SATIR buydu
        public DbSet<UserRegistrationRequest> RegistrationRequests { get; set; }
        public DbSet<Computer> Computers { get; set; }
        public DbSet<ComputerMetric> ComputerMetrics { get; set; }
        public DbSet<PasswordSetupToken> PasswordSetupTokens { get; set; }
        public DbSet<ComputerDisk> ComputerDisks { get; set; }
        public DbSet<DiskMetric> DiskMetrics { get; set; }

        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- 1. USER (Kullanıcı) ---
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // --- 2. COMPUTER (Bilgisayar) ---
            modelBuilder.Entity<Computer>(entity =>
            {
                entity.HasKey(e => e.Id);

                // MacAddress Eşsiz ve Zorunlu
                entity.HasIndex(e => e.MacAddress).IsUnique();
                entity.Property(e => e.MacAddress).IsRequired().HasMaxLength(50);

                // Cascade Delete: Bilgisayar silinirse metrikleri de silinsin
                entity.HasMany(c => c.Metrics)
                      .WithOne(m => m.Computer)
                      .HasForeignKey(m => m.ComputerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // --- 3. COMPUTER METRIC ---
            modelBuilder.Entity<ComputerMetric>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CreatedAt); // Tarih sorguları için hızlandırıcı
            });
            // Computer -> ComputerDisk (Bire-Çok)
            modelBuilder.Entity<ComputerDisk>()
                .HasOne(d => d.Computer)
                .WithMany(c => c.Disks)
                .HasForeignKey(d => d.ComputerId);

            // ComputerDisk -> DiskMetric (Bire-Çok)
            modelBuilder.Entity<DiskMetric>()
                .HasOne(m => m.ComputerDisk)
                .WithMany(d => d.DiskMetrics)
                .HasForeignKey(m => m.ComputerDiskId);
            // --- 4. REGISTRATION REQUEST ---
            modelBuilder.Entity<UserRegistrationRequest>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
            });
        }
    }
}