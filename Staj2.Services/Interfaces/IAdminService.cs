using Staj2.Services.Models;

namespace Staj2.Services.Interfaces;

public interface IAdminService
{
    Task<object> GetRolesAsync();
    Task<object> GetAllPermissionsAsync();
    Task<List<int>> GetRolePermissionsAsync(int roleId);
    Task<string?> CreateRoleAsync(CreateRoleRequest request, int? currentUserId);
    Task<string?> UpdateRolePermissionsAsync(int roleId, UpdateRolePermissionsRequest request, int? currentUserId);
    Task<string?> DeleteRoleAsync(int id, int? currentUserId);

    Task<object> GetAllUsersAsync();
    Task<string?> DeleteUserAsync(int id);
    Task<string?> ChangeUserRolesAsync(int userId, ChangeRolesRequest request);

    // --- KAYIT İSTEKLERİ YÖNETİMİ ---
    Task<object> GetPendingRequestsAsync();

    // DÖNÜŞ TİPİ SADELEŞTİRİLDİ: Sadece başarı durumu ve hata mesajı dönüyor.
    Task<(bool IsSuccess, string? ErrorMessage)> RejectRequestAsync(RejectRegistrationRequest request, int? adminId);

    // DÖNÜŞ TİPİ SADELEŞTİRİLDİ: Sadece başarı durumu ve hata mesajı dönüyor.
    Task<(bool IsSuccess, string? ErrorMessage)> ApproveRequestAsync(int id, ChangeRoleRequest? req, int? adminId);

    // --- KULLANICI CİHAZ VE ETİKET ATAMA YÖNETİMİ ---
    Task<object?> GetUserAccessAsync(int userId);
    Task<string?> AssignComputersAsync(int userId, AssignComputersRequest req);
    Task<string?> AssignTagsAsync(int userId, AssignTagsRequest req);
    Task<object> GetAllComputersForAssignmentAsync();

    // --- ETİKET YÖNETİMİ ---
    Task<object> GetTagsAsync();
    Task<(bool IsSuccess, string? ErrorMessage, object? CreatedTag)> CreateTagAsync(TagCreateRequest request, int? userId);
    Task<string?> DeleteTagAsync(int id);
    Task<string?> AssignComputersToTagAsync(int tagId, AssignComputersToTagRequest req);
    Task<List<int>> GetTagAssignedComputerIdsAsync(int tagId);
}