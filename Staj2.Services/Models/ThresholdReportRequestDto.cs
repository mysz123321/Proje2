namespace Staj2.Services.Models;

public class ThresholdReportRequestDto
{
    public double CpuThreshold { get; set; }
    public double RamThreshold { get; set; }
    public Dictionary<string, double> DiskThresholds { get; set; } = new();
    // Yeni eklenen alanlar
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}