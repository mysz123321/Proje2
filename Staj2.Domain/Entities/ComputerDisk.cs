using Staj2.Domain.Entities; // Gerekli using

namespace Staj2.Domain.Entities // EKSİK OLAN KISIM
{
    public class ComputerDisk
    {
        public int Id { get; set; }
        public int ComputerId { get; set; }
        public Computer Computer { get; set; } = null!;

        public string DiskName { get; set; } = null!; // Örn: "C:"
        public double TotalSizeGb { get; set; }
        public double? ThresholdPercent { get; set; } // null olabilir

        // --- YENİ EKLENEN: Her diskin kendi bildirim süresi ---
        public DateTime? LastNotifyTime { get; set; }

        public List<DiskMetric> DiskMetrics { get; set; } = new();
    }
}