using Staj2.Domain.Entities;

public class Computer
{
    public int Id { get; set; }
    public string MacAddress { get; set; } = null!;
    public string MachineName { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? IpAddress { get; set; }
    public string? CpuModel { get; set; }
    public double TotalRamMb { get; set; }
    public List<Tag> Tags { get; set; } = new();
    // --- YENİ EKLENEN EŞİK DEĞERLERİ ---
    public double? CpuThreshold { get; set; } // null olabilir
    public double? RamThreshold { get; set; } // null olabilir

    public DateTime? LastNotifyTime { get; set; }
    public DateTime LastSeen { get; set; }

    // İlişkiler
    public List<ComputerDisk> Disks { get; set; } = new();
    public List<ComputerMetric> Metrics { get; set; } = new();
}