using Staj2.Services.Models;

namespace Staj2.Services.Interfaces;

public interface IAdminService
{
    // Roller ve Yetkiler
    Task<object> GetRolesAsync();
    Task<object> GetAllPermissionsAsync();
    Task<List<int>> GetRolePermissionsAsync(int roleId);

    // YENİ DÖNÜŞ TİPİ: (bool isSuccess, string message)
    Task<(bool isSuccess, string message)> CreateRoleAsync(CreateRoleRequest request, int? currentUserId);
    Task<(bool isSuccess, string message)> UpdateRolePermissionsAsync(int roleId, UpdateRolePermissionsRequest request, int? currentUserId);
    Task<(bool isSuccess, string message)> DeleteRoleAsync(int id, int? currentUserId);

    // Kullanıcılar
    Task<object> GetAllUsersAsync();
    Task<(bool isSuccess, string message)> DeleteUserAsync(int id, int? currentUserId);
    Task<(bool isSuccess, string message)> ChangeUserRolesAsync(int userId, ChangeRolesRequest request);

    // Kayıt İstekleri
    Task<object> GetPendingRequestsAsync();
    Task<(bool IsSuccess, string message)> RejectRequestAsync(RejectRegistrationRequest request, int? adminId);
    Task<(bool IsSuccess, string message)> ApproveRequestAsync(int id, ChangeRoleRequest? req, int? adminId);

    // Erişimler (Cihaz/Etiket)
    Task<object?> GetUserAccessAsync(int userId);
    Task<(bool isSuccess, string message)> AssignComputersAsync(int userId, AssignComputersRequest req);
    Task<(bool isSuccess, string message)> AssignTagsAsync(int userId, AssignTagsRequest req);
    Task<object> GetAllComputersForAssignmentAsync();

    // Etiketler
    Task<object> GetTagsAsync();
    Task<(bool IsSuccess, string message, object? CreatedTag)> CreateTagAsync(TagCreateRequest request, int? userId);
    Task<(bool isSuccess, string message)> DeleteTagAsync(int id, int? currentUserId);
    Task<(bool isSuccess, string message)> AssignComputersToTagAsync(int tagId, AssignComputersToTagRequest req);
    Task<List<int>> GetTagAssignedComputerIdsAsync(int tagId);
}