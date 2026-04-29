namespace Staj2.Services.Models;

/// <summary>
/// Disk metriklerinin zaman kovası (bucket) verisi.
/// Nullable alanlar, cihazın kapalı olduğu (veri göndermediği) zaman dilimlerini temsil eder.
/// </summary>
public class DiskBucketDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime? MaxCreatedAt { get; set; }
    public double? UsedAvg { get; set; }
    public double? UsedMin { get; set; }
    public double? UsedMax { get; set; }
    public double? UsedOpen { get; set; }
    public double? UsedClose { get; set; }
    public string DiskName { get; set; } = string.Empty;
}
