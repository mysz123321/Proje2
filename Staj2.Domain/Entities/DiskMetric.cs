using Staj2.Domain.Entities;

namespace Staj2.Domain.Entities
{
    public class DiskMetric
    {
        public long Id { get; set; }
        public int ComputerDiskId { get; set; }
        public ComputerDisk ComputerDisk { get; set; } = null!;

        public double UsedPercent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}