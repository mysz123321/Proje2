using System;
using Staj2.Domain.Common;

namespace Staj2.Domain.Entities
{
    public class ComputerThresholdHistory : ICreatableEntity
    {
        public int Id { get; set; }
        public int ComputerId { get; set; }
        public Computer Computer { get; set; } = null!;

        public double? CpuThreshold { get; set; }
        public double? RamThreshold { get; set; }

        // Bu eşik değerinin geçerli olmaya başladığı tarih
        public DateTime ActiveFrom { get; set; }

        // ICreatableEntity arayüzünden gelen log alanları
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
    }
}