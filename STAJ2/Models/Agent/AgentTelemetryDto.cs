namespace STAJ2.Models.Agent;

public class AgentTelemetryDto
{
    public string MachineName { get; set; }
    public string? AgentId { get; set; }
    public string? Ip { get; set; }

    public double CpuPercent { get; set; } // Anlık Yük

    // RAM Oranını bulmak için: (Total - Available) / Total
    public double AvailableRamMb { get; set; } // Boş RAM
    public double TotalRamMb { get; set; }     // EKLENDİ: Toplam RAM

    public DateTime Ts { get; set; }

    public List<DiskDto> Disks { get; set; } = new();

    public class DiskDto
    {
        public string Name { get; set; }       // C:\
        public long TotalBytes { get; set; }   // EKLENDİ (Zaten kodunda vardı ama DTO'da emin olalım)
        public long FreeBytes { get; set; }    // Boş alan
    }
}