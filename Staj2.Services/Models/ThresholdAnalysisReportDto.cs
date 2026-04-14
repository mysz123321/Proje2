namespace Staj2.Services.Models
{
    public class ThresholdAnalysisReportDto
    {
        public int ComputerId { get; set; }
        public string ComputerName { get; set; }
        public double TotalActiveSeconds { get; set; }

        public MetricThresholdResult CpuResult { get; set; } = new();
        public MetricThresholdResult RamResult { get; set; } = new();
        public List<DiskThresholdResult> DiskResults { get; set; } = new();
    }

    public class MetricThresholdResult
    {
        // ThresholdValue KALDIRILDI! Artık tarihe göre dinamik hesaplanıyor.
        public double BelowThresholdSeconds { get; set; }
        public double TotalActiveSeconds { get; set; }
        // Yüzde hesabı tam senin istediğin gibi toplam saniyeye oranlanıyor
        public double BelowThresholdPercentage => TotalActiveSeconds > 0 ? (BelowThresholdSeconds / TotalActiveSeconds) * 100 : 0;
    }

    public class DiskThresholdResult : MetricThresholdResult
    {
        public string DiskName { get; set; }
    }
}