using System.ComponentModel.DataAnnotations; // <--- BU SATIRI EKLEMEYİ UNUTMA

namespace STAJ2.Models // Namespace ekli değilse ekleyebilirsin, genelde vardır
{
    public class UpdateComputerNameRequest
    {
        public int Id { get; set; }

        public string NewDisplayName { get; set; }
    }
}