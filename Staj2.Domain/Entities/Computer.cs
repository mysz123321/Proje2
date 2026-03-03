using Staj2.Domain.Common;
using Staj2.Domain.Entities;

namespace Staj2.Domain.Entities
{
    public class Computer 
    {
        public int Id { get; set; }
        public string MacAddress { get; set; } = null!;
        public string MachineName { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? IpAddress { get; set; }
        public string? CpuModel { get; set; }
        public double TotalRamMb { get; set; }
        // Etiketler
        public List<Tag> Tags { get; set; } = new();
        public bool IsDeleted { get; set; } = false;
        // --- EŞİK DEĞERLERİ ---
        public double? CpuThreshold { get; set; }
        public double? RamThreshold { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CpuLastNotifyTime { get; set; } // YENİ: CPU için son bildirim
        public DateTime? RamLastNotifyTime { get; set; } // YENİ: RAM için son bildirim

        public DateTime LastSeen { get; set; }

        // İlişkiler
        public List<ComputerDisk> Disks { get; set; } = new();
        public List<ComputerMetric> Metrics { get; set; } = new();
        public ICollection<UserComputerAccess> UserAccesses { get; set; } = new List<UserComputerAccess>();
    }
}