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
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRegistrationRequest> RegistrationRequests { get; set; }
        public DbSet<Computer> Computers { get; set; }
        public DbSet<ComputerMetric> ComputerMetrics { get; set; }
        public DbSet<PasswordSetupToken> PasswordSetupTokens { get; set; }
        public DbSet<ComputerDisk> ComputerDisks { get; set; }
        public DbSet<DiskMetric> DiskMetrics { get; set; }
        public DbSet<Tag> Tags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- İLİŞKİLER VE ARA TABLOLAR ---

            // Tag - Computer (Many-to-Many)
            modelBuilder.Entity<Computer>()
                .HasMany(c => c.Tags)
                .WithMany(t => t.Computers)
                .UsingEntity(j => j.ToTable("ComputerTags"));

            // User - Role (Many-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity(j => j.ToTable("UserRoles"));

            // Computer -> ComputerDisk (One-to-Many)
            modelBuilder.Entity<ComputerDisk>()
                .HasOne(d => d.Computer)
                .WithMany(c => c.Disks)
                .HasForeignKey(d => d.ComputerId);

            // ComputerDisk -> DiskMetric (One-to-Many)
            modelBuilder.Entity<DiskMetric>()
                .HasOne(m => m.ComputerDisk)
                .WithMany(d => d.DiskMetrics)
                .HasForeignKey(m => m.ComputerDiskId);


            // --- ALAN KISITLAMALARI (MAX LENGTH 200) ---

            // 1. USER
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();

                // Kısıtlamalar
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.PasswordHash).HasMaxLength(200);
            });

            // 2. ROLE
            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(200);
            });

            // 3. TAG
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(200);
            });

            // 4. COMPUTER
            modelBuilder.Entity<Computer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.MacAddress).IsUnique();
                entity.Property(e => e.MacAddress).IsRequired().HasMaxLength(50); // Mac genelde kısadır ama 50 kalsın

                // Kısıtlamalar
                entity.Property(e => e.MachineName).HasMaxLength(200);
                entity.Property(e => e.DisplayName).HasMaxLength(200);
                entity.Property(e => e.IpAddress).HasMaxLength(200);
                entity.Property(e => e.CpuModel).HasMaxLength(200);

                // Cascade Delete
                entity.HasMany(c => c.Metrics)
                      .WithOne(m => m.Computer)
                      .HasForeignKey(m => m.ComputerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 5. COMPUTER DISK
            modelBuilder.Entity<ComputerDisk>(entity =>
            {
                entity.Property(e => e.DiskName).HasMaxLength(200);
            });

            // 6. PASSWORD SETUP TOKEN
            modelBuilder.Entity<PasswordSetupToken>(entity =>
            {
                entity.Property(e => e.TokenHash).HasMaxLength(200);
            });

            // AppDbContext.cs içindeki ilgili bloğu bul ve bu şekilde değiştir:

            // 7. REGISTRATION REQUEST
            modelBuilder.Entity<UserRegistrationRequest>(entity =>
            {
                // ESKİ HALİ: entity.HasIndex(e => e.Email).IsUnique();
                // YENİ HALİ (Unique YOK):
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.Username);

                // Kısıtlamalar
                entity.Property(e => e.RejectionReason).HasMaxLength(200);
            });

            // 8. COMPUTER METRIC
            modelBuilder.Entity<ComputerMetric>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}