using Staj2.Services.Models;

namespace Staj2.Services.Interfaces;

public interface IAdminService
{
    Task<object> GetRolesAsync();
    Task<object> GetAllPermissionsAsync();
    Task<List<int>> GetRolePermissionsAsync(int roleId);

    // CreateRoleAsync artık başarısızlık durumunda bir hata mesajı dönecek (string?), başarılıysa null.
    Task<string?> CreateRoleAsync(CreateRoleRequest request, int? currentUserId);

    // UpdateRolePermissionsAsync başarısızlık durumunda hata mesajı, başarılıysa null dönecek.
    Task<string?> UpdateRolePermissionsAsync(int roleId, UpdateRolePermissionsRequest request, int? currentUserId);

    // DeleteRoleAsync de başarısızlıkta hata mesajı, başarılıysa null dönecek.
    Task<string?> DeleteRoleAsync(int id, int? currentUserId);
    
    Task<object> GetAllUsersAsync();
    Task<string?> DeleteUserAsync(int id);
    Task<string?> ChangeUserRolesAsync(int userId, ChangeRolesRequest request);
    // --- KAYIT İSTEKLERİ YÖNETİMİ ---
    Task<object> GetPendingRequestsAsync();

    // İşlem sonucu, Hata Mesajı, Kullanıcı Maili ve Kullanıcı Adını dönecek
    Task<(bool IsSuccess, string? ErrorMessage, string? Email, string? Username)> RejectRequestAsync(RejectRegistrationRequest request, int? adminId);

    // İşlem sonucu, Hata Mesajı, Kullanıcı Maili ve Oluşturulan Token'ı dönecek
    Task<(bool IsSuccess, string? ErrorMessage, string? Email, string? Token)> ApproveRequestAsync(int id, ChangeRoleRequest? req, int? adminId);
    // --- KULLANICI CİHAZ VE ETİKET ATAMA YÖNETİMİ ---
    Task<object?> GetUserAccessAsync(int userId);
    Task<string?> AssignComputersAsync(int userId, AssignComputersRequest req);
    Task<string?> AssignTagsAsync(int userId, AssignTagsRequest req);
    Task<object> GetAllComputersForAssignmentAsync();
    // --- ETİKET YÖNETİMİ ---
    Task<object> GetTagsAsync();

    // İşlem başarılıysa CreatedTag nesnesini, başarısızsa ErrorMessage döneceğiz
    Task<(bool IsSuccess, string? ErrorMessage, object? CreatedTag)> CreateTagAsync(TagCreateRequest request, int? userId);

    Task<string?> DeleteTagAsync(int id);
    Task<string?> AssignComputersToTagAsync(int tagId, AssignComputersToTagRequest req);
    Task<List<int>> GetTagAssignedComputerIdsAsync(int tagId);
}