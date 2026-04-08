using Staj2.Services.Models;

namespace Staj2.Services.Interfaces;

public interface IUiService
{
    // Eskiden: Task<(bool IsSuccess, string? ErrorMessage, object? Data)>
    Task<ServiceResult<object>> GetSidebarItemsAsync(int userId);

    // Eskiden: Task<(bool IsSuccess, string? ErrorMessage, List<string>? Permissions)>
    Task<ServiceResult<List<string>>> GetMyPermissionsAsync(int userId);

    // İleride açmak istersen bu da böyle olmalı:
    // Task<ServiceResult<object>> GetUserActionsAsync();
}