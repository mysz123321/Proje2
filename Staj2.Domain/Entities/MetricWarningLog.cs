using System;
using System.ComponentModel.DataAnnotations;

namespace Staj2.Domain.Entities
{
    public class MetricWarningLog
    {
        public long Id { get; set; }

        public int ComputerId { get; set; }
        public Computer Computer { get; set; }

        [Required]
        [MaxLength(50)]
        public string MetricType { get; set; } // Örn: "CPU", "RAM", "Disk"

        [MaxLength(200)]
        public string DiskName { get; set; } // Sadece MetricType "Disk" ise dolu olur (Örn: "C:\")

        public double MetricValue { get; set; } // O an ölçülen değer
        public double ThresholdValue { get; set; } // O an aşılan eşik değeri

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}