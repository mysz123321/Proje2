namespace Staj2.Services.Models;

/// <summary>
/// CPU ve RAM metriklerinin zaman kovası (bucket) verisi.
/// Nullable alanlar, cihazın kapalı olduğu (veri göndermediği) zaman dilimlerini temsil eder.
/// </summary>
public class CpuRamBucketDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime? MaxCreatedAt { get; set; }
    public double? CpuAvg { get; set; }
    public double? CpuMin { get; set; }
    public double? CpuMax { get; set; }
    public double? CpuOpen { get; set; }
    public double? CpuClose { get; set; }
    public double? RamAvg { get; set; }
    public double? RamMin { get; set; }
    public double? RamMax { get; set; }
    public double? RamOpen { get; set; }
    public double? RamClose { get; set; }
}
