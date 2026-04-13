namespace Staj2.Services.Models
{
    public class ThresholdReportRequestDto
    {
        public double CpuThreshold { get; set; }
        public double RamThreshold { get; set; }
        // Disk Adı ve Eşik Değeri eşleşmesi için
        public Dictionary<string, double> DiskThresholds { get; set; } = new();
    }
}