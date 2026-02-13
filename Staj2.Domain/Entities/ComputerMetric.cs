using System.ComponentModel.DataAnnotations.Schema;

namespace Staj2.Domain.Entities;

public class ComputerMetric
{
    public long Id { get; set; }
    public int ComputerId { get; set; }
    public Computer Computer { get; set; }

    public double CpuUsage { get; set; }
    public double RamUsage { get; set; }

    // Yeni Hali: "C: %40, D: %10" gibi her diskin anlık doluluk oranı
    public string DiskUsage { get; set; }

    public DateTime CreatedAt { get; set; }
}