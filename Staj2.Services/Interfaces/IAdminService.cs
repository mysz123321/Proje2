using Staj2.Services.Models;

namespace Staj2.Services.Interfaces;

public interface IAdminService
{
    // --- Roller ve Yetkiler ---

    // Eskiden Task<object> dönenleri ServiceResult<object> yaptık
    Task<ServiceResult<object>> GetRolesAsync();
    Task<ServiceResult<object>> GetAllPermissionsAsync();

    // Eskiden Task<List<int>> dönen metot
    Task<ServiceResult<List<int>>> GetRolePermissionsAsync(int roleId);

    // Sadece başarılı/başarısız durumu bildirenler ServiceResult döner
    Task<ServiceResult> CreateRoleAsync(CreateRoleRequest request, int? currentUserId);
    Task<ServiceResult> UpdateRolePermissionsAsync(int roleId, UpdateRolePermissionsRequest request, int? currentUserId);
    Task<ServiceResult> DeleteRoleAsync(int id, int? currentUserId);

    // --- Kullanıcılar ---
    Task<ServiceResult<object>> GetAllUsersAsync();
    Task<ServiceResult> DeleteUserAsync(int id, int? currentUserId);
    Task<ServiceResult> ChangeUserRolesAsync(int userId, ChangeRolesRequest request);

    // --- Kayıt İstekleri ---
    Task<ServiceResult<object>> GetPendingRequestsAsync();
    Task<ServiceResult> RejectRequestAsync(RejectRegistrationRequest request, int? adminId);
    Task<ServiceResult> ApproveRequestAsync(int id, ChangeRoleRequest? req, int? adminId);

    // --- Erişimler (Cihaz/Etiket) ---
    // Task<object?> yerine ServiceResult<object> (Data propertysi null olabilir)
    Task<ServiceResult<object>> GetUserAccessAsync(int userId);
    Task<ServiceResult> AssignComputersAsync(int userId, AssignComputersRequest req);
    Task<ServiceResult> AssignTagsAsync(int userId, AssignTagsRequest req);
    Task<ServiceResult<object>> GetAllComputersForAssignmentAsync();

    // --- Etiketler ---
    Task<ServiceResult<object>> GetTagsAsync();

    // Eskiden: Task<(bool IsSuccess, string message, object? CreatedTag)>
    // Artık: ServiceResult<object> (Oluşturulan etiketi "Data" içinde döneceğiz)
    Task<ServiceResult<object>> CreateTagAsync(TagCreateRequest request, int? userId);

    Task<ServiceResult> DeleteTagAsync(int id, int? currentUserId);
    Task<ServiceResult> AssignComputersToTagAsync(int tagId, AssignComputersToTagRequest req);
    Task<ServiceResult<List<int>>> GetTagAssignedComputerIdsAsync(int tagId);
}