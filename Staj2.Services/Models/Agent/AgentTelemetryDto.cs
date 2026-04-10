namespace Staj2.Services.Models.Agent;

public class AgentTelemetryDto
{
    public int ComputerId { get; set; }
    public string? DisplayName { get; set; }
    public required string MacAddress { get; set; }
    public required string MachineName { get; set; }
    public string? Ip { get; set; }

    public string? CpuModel { get; set; }
    public double TotalRamMb { get; set; }
    public required string TotalDiskGb { get; set; }
    public double? CpuThreshold { get; set; }
    public double? RamThreshold { get; set; }
    public double CpuUsage { get; set; }
    public double RamUsage { get; set; }
    public required string DiskUsage { get; set; }

    // EKLENEN KISIM: Disk isimlerine karşılık gelen sınır değerlerini tutacak sözlük
    public Dictionary<string, int>? DiskThresholds { get; set; }

    public List<string> Tags { get; set; } = new();
    public DateTime Ts { get; set; }
}