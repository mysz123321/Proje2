namespace STAJ2.Models.Agent;

public class AgentTelemetryDto
{
    // --- Kimlik Bilgileri ---
    public string MacAddress { get; set; }
    public string MachineName { get; set; }
    public string? Ip { get; set; }

    // --- Donanım Bilgileri ---
    public string? CpuModel { get; set; }
    public double TotalRamMb { get; set; }

    // BURASI DEĞİŞTİ: Artık "C: 333 D: 256" gibi metin tutacak
    public string TotalDiskGb { get; set; }

    // --- Anlık Ölçümler ---
    public double CpuUsage { get; set; }
    public double RamUsage { get; set; }

    // BURASI DEĞİŞTİ: Artık "C: %40 D: %10" gibi metin tutacak
    public string DiskUsage { get; set; }

    public DateTime Ts { get; set; }
}