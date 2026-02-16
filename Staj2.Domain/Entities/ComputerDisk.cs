public class ComputerDisk
{
    public int Id { get; set; }
    public int ComputerId { get; set; }
    public Computer Computer { get; set; } = null!;

    public string DiskName { get; set; } = null!; // Örn: "C:"
    public double TotalSizeGb { get; set; }
    public double? ThresholdPercent { get; set; } // null olabilir

    public List<DiskMetric> DiskMetrics { get; set; } = new();
}