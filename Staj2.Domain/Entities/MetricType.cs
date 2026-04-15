using System.ComponentModel.DataAnnotations;

namespace Staj2.Domain.Entities
{
    public class MetricType
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } // "CPU", "RAM", "Disk" vb.
    }
}