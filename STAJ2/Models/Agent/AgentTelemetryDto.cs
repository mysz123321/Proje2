namespace STAJ2.Models.Agent;

public class AgentTelemetryDto
{
    // 'required' anahtar kelimesi ile uyarıları siliyoruz
    public required string MacAddress { get; set; }
    public required string MachineName { get; set; }
    public string? Ip { get; set; }

    public string? CpuModel { get; set; }
    public double TotalRamMb { get; set; }
    public required string TotalDiskGb { get; set; }

    public double CpuUsage { get; set; }
    public double RamUsage { get; set; }
    public required string DiskUsage { get; set; }

    public DateTime Ts { get; set; }
}