using System.ComponentModel.DataAnnotations;

namespace Staj2.Domain.Entities;

public class AgentMetric
{
    public int Id { get; set; }

    [Required]
    public string AgentId { get; set; } // Hangi bilgisayar?
    public string MachineName { get; set; }

    public double CpuPercent { get; set; }
    public double AvailableRamMb { get; set; }
    public double TotalRamMb { get; set; }

    public DateTime CreatedAt { get; set; } // Kayıt zamanı

    // İlişki: Bir ölçümün birden fazla diski olabilir
    public List<DiskMetric> Disks { get; set; } = new();
}