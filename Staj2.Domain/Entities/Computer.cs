using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Staj2.Domain.Entities;

[Index(nameof(MacAddress), IsUnique = true)] // MAC adresi benzersiz olacak
// ... diğer usingler
public class Computer
{
    public int Id { get; set; }
    public string MacAddress { get; set; }
    public string MachineName { get; set; }
    public string? DisplayName { get; set; }
    public string? IpAddress { get; set; }
    public string? CpuModel { get; set; } // Örn: Intel i7 @ 2.70 GHz
    public double TotalRamMb { get; set; }
    public DateTime? LastNotifyTime { get; set; } // En son uyarı ne zaman atıldı?
    // Yeni Hali: "C: 465, D: 931" gibi tüm disklerin toplam boyutları
    public string? TotalDiskGb { get; set; }

    public DateTime LastSeen { get; set; }
    public List<ComputerMetric> Metrics { get; set; } = new();
}