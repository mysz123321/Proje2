namespace Staj2.Services.Interfaces
{
    public interface IEndpointPermissionService
    {
        Task<string> GetRequiredPermissionAsync(string controller, string action);
    }
}