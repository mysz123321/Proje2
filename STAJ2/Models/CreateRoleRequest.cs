using System.ComponentModel.DataAnnotations;

namespace STAJ2.Models
{
    public class CreateRoleRequest
    {
        [Required(ErrorMessage = "Rol adı zorunludur.")]
        [MaxLength(200, ErrorMessage = "Rol adı en fazla 200 karakter olabilir.")]
        public string Name { get; set; }

        // Seçilen yetkilerin ID listesi
        public List<int> PermissionIds { get; set; } = new List<int>();
    }
}