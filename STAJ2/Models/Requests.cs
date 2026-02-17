namespace STAJ2.Models
{
    // Kullanıcı İşlemleri için
    public class ChangeRolesRequest { public List<int> NewRoleIds { get; set; } = new(); }
    public class ChangeRoleRequest { public int NewRoleId { get; set; } }

    // Etiket İşlemleri için
    public class TagCreateRequest { public string Name { get; set; } = null!; }

    // Cihaz İşlemleri için (ComputerController tarafından kullanılacak)
    public class UpdateComputerNameRequest { public int Id { get; set; } public string NewDisplayName { get; set; } = null!; }

    public class UpdateThresholdsRequest
    {
        public double? CpuThreshold { get; set; }
        public double? RamThreshold { get; set; }
        public List<DiskThresholdItem>? DiskThresholds { get; set; }
    }

    public class DiskThresholdItem
    {
        public string DiskName { get; set; } = null!;
        public double? ThresholdPercent { get; set; }
    }

    public class UpdateComputerTagsRequest { public List<string> Tags { get; set; } = new(); }
}