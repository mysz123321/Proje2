namespace STAJ2.Models.Agent;

public sealed class AgentTelemetryDto
{
    public DateTimeOffset Ts { get; set; }
    public string MachineName { get; set; } = "";
    public string Ip { get; set; } = "";
    public double? CpuPercent { get; set; }
    public double? AvailableRamMb { get; set; }
    public string AgentId { get; set; } = "";
    public List<DiskDto> Disks { get; set; } = new();

    public sealed class DiskDto
    {
        public string Name { get; set; } = "";      // "C:\"
        public long TotalBytes { get; set; }
        public long FreeBytes { get; set; }
    }
}
