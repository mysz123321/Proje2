public sealed class AgentTelemetryDto
{
    public DateTimeOffset Ts { get; set; }

    // Stable kimlik: aynı PC her zaman aynı id ile gelir
    public string AgentId { get; set; } = "";

    public string MachineName { get; set; } = "";
    public string Ip { get; set; } = "";

    public double? CpuPercent { get; set; }
    public double? AvailableRamMb { get; set; }

    public List<DiskDto> Disks { get; set; } = new();

    public sealed class DiskDto
    {
        public string Name { get; set; } = ""; // "C:\"
        public long TotalBytes { get; set; }
        public long FreeBytes { get; set; }
    }
}
