namespace Staj2.Services.Models
{
    public class ThresholdAnalysisReportDto
    {
        public int ComputerId { get; set; }
        public string ComputerName { get; set; }

        // Saniye cinsinden toplam takip edilebilen aktif süre
        public double TotalActiveSeconds { get; set; }

        public MetricThresholdResult CpuResult { get; set; } = new();
        public MetricThresholdResult RamResult { get; set; } = new();
        public List<DiskThresholdResult> DiskResults { get; set; } = new();
    }

    public class MetricThresholdResult
    {
        public double ThresholdValue { get; set; }
        public double BelowThresholdSeconds { get; set; }
        public double BelowThresholdPercentage => TotalActiveSeconds > 0 ? (BelowThresholdSeconds / TotalActiveSeconds) * 100 : 0;
        public double TotalActiveSeconds { get; set; } // Referans için
    }

    public class DiskThresholdResult : MetricThresholdResult
    {
        public string DiskName { get; set; }
    }
}