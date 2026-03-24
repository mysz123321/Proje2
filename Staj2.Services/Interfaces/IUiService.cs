namespace Staj2.Services.Interfaces;

public interface IUiService
{
    Task<(bool IsSuccess, string? ErrorMessage, object? Data)> GetSidebarItemsAsync(int userId);
    Task<(bool IsSuccess, string? ErrorMessage, List<string>? Permissions)> GetMyPermissionsAsync(int userId);
    //Task<object> GetUserActionsAsync();
}   