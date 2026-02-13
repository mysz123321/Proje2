namespace Staj2.Domain.Entities;

public class DiskMetric
{
    public int Id { get; set; }

    public string DiskName { get; set; }    // C:\
    public long TotalSpace { get; set; }    // Byte cinsinden
    public long FreeSpace { get; set; }     // Byte cinsinden

    // Bağlı olduğu ana ölçüm
    public int AgentMetricId { get; set; }
    public AgentMetric AgentMetric { get; set; }
}