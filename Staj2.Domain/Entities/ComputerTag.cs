using System;
using Staj2.Domain.Common;

namespace Staj2.Domain.Entities
{
    // Cihaz ve Etiket arasındaki çoka-çok ilişkinin fiziksel tablosu
    public class ComputerTag : ICreatableEntity, ISoftDeletableEntity
    {
        public int Id { get; set; }
        public int ComputerId { get; set; }
        public Computer Computer { get; set; } = null!;

        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;

        // ICreatableEntity'den gelenler
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        // ISoftDeletableEntity'den gelenler
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
    }
}