using System;
using System.Collections.Generic;
using Staj2.Domain.Common;

namespace Staj2.Domain.Entities
{
    public class Tag : ICreatableEntity, ISoftDeletableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        // --- ICreatableEntity ---
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        // --- ISoftDeletableEntity ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        public List<Computer> Computers { get; set; } = new();
    }
}