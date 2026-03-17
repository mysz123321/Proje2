namespace Staj2.Services.Models;

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public List<int> PermissionIds { get; set; } = new();
}

public class UpdateRolePermissionsRequest
{
    public List<int> PermissionIds { get; set; } = new();
}