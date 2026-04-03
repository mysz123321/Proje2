using System.Collections.Generic;

namespace Staj2.Services.Models
{
    public class PerformanceReportDto
    {
        public double GlobalAverageCpu { get; set; }
        public double GlobalAverageRam { get; set; }
        public List<DevicePerformanceDto> Devices { get; set; } = new();
    }

    public class DevicePerformanceDto
    {
        public int ComputerId { get; set; }
        public string ComputerName { get; set; }
        public double AverageCpu { get; set; }
        public double AverageRam { get; set; }
        public string CpuStatus { get; set; } 
        public string RamStatus { get; set; } 
    }
}