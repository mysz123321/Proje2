using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Common;
using Staj2.Domain.Entities;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Staj2.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor = null) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
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
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<SidebarItem> SidebarItems { get; set; }
        public DbSet<UserTableAction> UserTableActions { get; set; }
        // Kullanıcı - Cihaz/Etiket Erişim Tabloları
        public DbSet<UserComputerAccess> UserComputerAccesses { get; set; }
        public DbSet<UserTagAccess> UserTagAccesses { get; set; }

        // YENİ: Fiziksel Ara Tablolar
        public DbSet<ComputerTag> ComputerTags { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        // ====================================================================
        // YENİ: ARAYÜZ (INTERFACE) TABANLI OTOMATİK AUDIT LOGGING
        // ====================================================================
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            int? currentUserId = null;
            var user = _httpContextAccessor?.HttpContext?.User;

            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user?.FindFirst("id")?.Value
                           ?? user?.FindFirst("UserId")?.Value;

            if (int.TryParse(userIdClaim, out int parsedId))
            {
                currentUserId = parsedId;
            }

            foreach (var entry in ChangeTracker.Entries())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        // Eğer tablo ICreatableEntity arayüzüne sahipse
                        if (entry.Entity is ICreatableEntity creatableEntity)
                        {
                            creatableEntity.CreatedAt = DateTime.UtcNow;
                            creatableEntity.CreatedBy = currentUserId;
                        }
                        break;

                    case EntityState.Modified:
                        // Senaryo A: Eğer tablo ISoftDeletableEntity ise VE IsDeleted manuel true yapıldıysa
                        if (entry.Entity is ISoftDeletableEntity softDeleteUpdate &&
                            softDeleteUpdate.IsDeleted &&
                            entry.Property(nameof(ISoftDeletableEntity.IsDeleted)).IsModified)
                        {
                            softDeleteUpdate.DeletedAt = DateTime.UtcNow;
                            softDeleteUpdate.DeletedBy = currentUserId;
                        }
                        // Senaryo B: Eğer tablo IUpdatableEntity ise ve silinmemişse
                        else if (entry.Entity is IUpdatableEntity updatableEntity)
                        {
                            bool isDeleted = (entry.Entity as ISoftDeletableEntity)?.IsDeleted ?? false;
                            if (!isDeleted)
                            {
                                updatableEntity.UpdatedAt = DateTime.UtcNow;
                                updatableEntity.UpdatedBy = currentUserId;
                            }
                        }
                        break;

                    case EntityState.Deleted:
                        // Eğer tablo ISoftDeletableEntity arayüzüne sahipse, gerçek silmeyi iptal edip Soft Delete'e çevir
                        if (entry.Entity is ISoftDeletableEntity softDeletableForDelete)
                        {
                            entry.State = EntityState.Modified;
                            softDeletableForDelete.IsDeleted = true;
                            softDeletableForDelete.DeletedAt = DateTime.UtcNow;
                            softDeletableForDelete.DeletedBy = currentUserId;
                        }
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================================
            // --- GLOBAL QUERY FILTERS (SOFT DELETE İÇİN) ---
            // ==========================================================
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
            modelBuilder.Entity<Computer>().HasQueryFilter(c => !c.IsDeleted);
            modelBuilder.Entity<Tag>().HasQueryFilter(t => !t.IsDeleted);
            modelBuilder.Entity<ComputerTag>().HasQueryFilter(ct => !ct.IsDeleted);
            modelBuilder.Entity<UserRole>().HasQueryFilter(ur => !ur.IsDeleted);
            modelBuilder.Entity<Role>().HasQueryFilter(r => !r.IsDeleted);
            // --- İLİŞKİLER VE ARA TABLOLAR ---

            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<UserComputerAccess>()
                .HasKey(uc => new { uc.UserId, uc.ComputerId });

            modelBuilder.Entity<UserTagAccess>()
                .HasKey(ut => new { ut.UserId, ut.TagId });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(200);
            });

            // YENİ: Tag - Computer (Fiziksel Sınıf ComputerTag Üzerinden Many-to-Many)
             modelBuilder.Entity<Computer>()
                .HasMany(c => c.Tags)
                .WithMany(t => t.Computers)
                .UsingEntity<ComputerTag>(
                    j => j.HasOne(pt => pt.Tag).WithMany().HasForeignKey(pt => pt.TagId),
                    j => j.HasOne(pt => pt.Computer).WithMany().HasForeignKey(pt => pt.ComputerId),
                    j =>
                    {
                        j.HasKey(t => t.Id); // DEĞİŞTİ: Artık anahtar sadece Id
                        j.ToTable("ComputerTags");
                    }
                );

            // YENİ: User - Role (Fiziksel Sınıf UserRole Üzerinden Many-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity<UserRole>(
                    j => j.HasOne(ur => ur.Role).WithMany().HasForeignKey(ur => ur.RoleId),
                    j => j.HasOne(ur => ur.User).WithMany().HasForeignKey(ur => ur.UserId),
                    j =>
                    {
                        j.HasKey(t => t.Id); // DEĞİŞTİ: Artık anahtar sadece Id
                        j.ToTable("UserRoles");
                    }
                );

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

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.PasswordHash).HasMaxLength(200);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(200);
            });

            modelBuilder.Entity<Computer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.MacAddress);
                entity.Property(e => e.MacAddress).IsRequired().HasMaxLength(50);
                entity.Property(e => e.MachineName).HasMaxLength(200);
                entity.Property(e => e.DisplayName).HasMaxLength(200);
                entity.Property(e => e.IpAddress).HasMaxLength(200);
                entity.Property(e => e.CpuModel).HasMaxLength(200);

                entity.HasMany(c => c.Metrics)
                      .WithOne(m => m.Computer)
                      .HasForeignKey(m => m.ComputerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ComputerDisk>(entity =>
            {
                entity.Property(e => e.DiskName).HasMaxLength(200);
            });

            modelBuilder.Entity<PasswordSetupToken>(entity =>
            {
                entity.Property(e => e.TokenHash).HasMaxLength(200);
            });

            modelBuilder.Entity<UserRegistrationRequest>(entity =>
            {
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.Username);
                entity.Property(e => e.RejectionReason).HasMaxLength(200);
            });

            modelBuilder.Entity<ComputerMetric>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CreatedAt);
            });
            modelBuilder.Entity<ComputerMetric>()
            .HasIndex(m => new { m.ComputerId, m.CreatedAt });

            modelBuilder.Entity<DiskMetric>()
                .HasIndex(m => m.CreatedAt);
        }
    }
}