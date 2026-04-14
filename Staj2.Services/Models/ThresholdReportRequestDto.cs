using System;

namespace Staj2.Services.Models
{
    public class ThresholdReportRequestDto
    {
        public int ComputerId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Eskiden burada olan CpuThreshold, RamThreshold gibi alanları TAMAMEN SİLİYORUZ.
    }
}