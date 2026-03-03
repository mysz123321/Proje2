using System;
using Staj2.Domain.Common;

namespace Staj2.Domain.Entities
{
    // Kullanıcı ve Rol arasındaki çoka-çok ilişkinin fiziksel tablosu
    public class UserRole : ICreatableEntity, ISoftDeletableEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        // ICreatableEntity'den gelenler
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        // ISoftDeletableEntity'den gelenler
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
    }
}