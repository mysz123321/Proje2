using System;
using Staj2.Domain.Common;

namespace Staj2.Domain.Entities
{
    public class DiskThresholdHistory : ICreatableEntity
    {
        public int Id { get; set; }
        public int ComputerDiskId { get; set; }
        public ComputerDisk ComputerDisk { get; set; } = null!;

        public double? ThresholdPercent { get; set; }

        public DateTime ActiveFrom { get; set; }

        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
    }
}