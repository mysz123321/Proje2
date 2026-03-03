using System;
using System.Collections.Generic;
using Staj2.Domain.Common;

namespace Staj2.Domain.Entities
{
    public class ComputerDisk : IUpdatableEntity
    {
        public int Id { get; set; }
        public int ComputerId { get; set; }
        public string DiskName { get; set; } = null!;
        public double TotalSizeGb { get; set; }
        public DateTime? LastNotifyTime { get; set; }
        public double? ThresholdPercent { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        public Computer Computer { get; set; } = null!;
        public List<DiskMetric> DiskMetrics { get; set; } = new();
    }
}