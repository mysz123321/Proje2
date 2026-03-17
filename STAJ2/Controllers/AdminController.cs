using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;
using Staj2.Services.Models;
using Staj2.Services.Services;
using STAJ2.Authorization;
using STAJ2.MailServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMailSender _mail;
    private readonly IAdminService _adminService;

    public AdminController(AppDbContext db, IMailSender mail, IAdminService adminService)
    {
        _db = db;
        _mail = mail;
        _adminService = adminService;
    }

    // --- KULLANICI YÖNETİMİ ---
    [HttpGet("users")]
    [HasPermission("User.Read")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _adminService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpDelete("users/{id:int}")]
    [Authorize(Roles = "Yönetici")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var errorMessage = await _adminService.DeleteUserAsync(id);

        if (errorMessage != null)
            return NotFound(new { message = errorMessage });

        return Ok(new { message = "Kullanıcı silindi." });
    }

    [HttpPut("users/{userId}/change-roles")]
    [HasPermission("User.ManageRoles")]
    public async Task<IActionResult> ChangeUserRoles(int userId, [FromBody] ChangeRolesRequest request)
    {
        var errorMessage = await _adminService.ChangeUserRolesAsync(userId, request);

        if (errorMessage == "Kullanıcı bulunamadı.")
            return NotFound(new { message = errorMessage });
        else if (errorMessage != null)
            return BadRequest(new { message = errorMessage });

        return Ok(new { message = "Roller güncellendi." });
    }

    // --- KAYIT İSTEKLERİ YÖNETİMİ ---

    [HttpGet("requests")]
    [HasPermission("User.Manage")]
    public async Task<IActionResult> PendingRequests()
    {
        var requests = await _adminService.GetPendingRequestsAsync();
        return Ok(requests);
    }

    // REDDETME İŞLEMİ
    [HttpPost("requests/reject")]
    [HasPermission("User.Manage")]
    public async Task<IActionResult> RejectRequest([FromBody] RejectRegistrationRequest request)
    {
        int? adminId = null;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;
        if (int.TryParse(userIdClaim, out int uid)) adminId = uid;

        var result = await _adminService.RejectRequestAsync(request, adminId);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage == "Talep bulunamadı.") return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        try
        {
            await _mail.SendAsync(
                result.Email!,
                "Kayıt Talebiniz Reddedildi",
                $"Merhaba {result.Username},\n\nTalebiniz maalesef onaylanmadı.\nSebep: {request.RejectionReason ?? "Belirtilmedi"}"
            );
        }
        catch (Exception ex)
        {
            // Hatayı terminale kırmızı renkli yazdıralım ki hemen fark edelim
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nMAIL GÖNDERİM HATASI: {ex.Message}\n");
            Console.ResetColor();
        }

        return Ok(new { message = "Talep reddedildi." });
    }

    // ONAYLAMA İŞLEMİ
    [HttpPost("requests/approve/{id}")]
    [HasPermission("User.Manage")]
    public async Task<IActionResult> ApproveRequest(int id, [FromBody] ChangeRoleRequest? req)
    {
        int? adminId = null;
        var adminIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(adminIdString, out int uid)) adminId = uid;

        var result = await _adminService.ApproveRequestAsync(id, req, adminId);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage == "Talep bulunamadı.") return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        var frontendLink = $"http://localhost:5267/set-password.html?token={result.Token}";
        try
        {
            await _mail.SendAsync(result.Email!, "Hoşgeldiniz", $"Hesabınız onaylandı. Şifrenizi belirlemek için tıklayın: {frontendLink}");
        }
        catch (Exception ex)
        {
            // Hatayı terminale kırmızı renkli yazdıralım ki hemen fark edelim
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nMAIL GÖNDERİM HATASI: {ex.Message}\n");
            Console.ResetColor();
        }

        return Ok(new { message = "Kayıt onaylandı, kullanıcı şifre belirleme mailini bekliyor." });
    }

    // --- ETİKET YÖNETİMİ ---
    [HttpGet("tags")]
    [Authorize]
    //[HasPermission("Tag.Manage")]
    public async Task<IActionResult> GetTags()
    {
        var tags = await _adminService.GetTagsAsync();
        return Ok(tags);
    }



    [HttpPost("tags")]
    [HasPermission("Tag.Manage")]
    public async Task<IActionResult> CreateTag([FromBody] TagCreateRequest request)
    {
        int? userId = null;
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdString, out int uid)) userId = uid;

        var result = await _adminService.CreateTagAsync(request, userId);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.CreatedTag);
    }

    [HttpDelete("tags/{id:int}")]
    [HasPermission("Tag.Manage")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var errorMessage = await _adminService.DeleteTagAsync(id);

        if (errorMessage != null)
            return NotFound(new { message = errorMessage });

        return Ok(new { message = "Etiket silindi." });
    }

    // --- ROL VE YETKİ YÖNETİMİ ---

    // 1. Sistemdeki tüm rolleri getir
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _adminService.GetRolesAsync();
        return Ok(roles);
    }

    // 2. Sistemdeki tüm olası yetkileri (Permissions) getir (Arayüzde checkbox listesi oluşturmak için)
    [HttpGet("permissions")]
    [HasPermission("Role.Manage")]
    public async Task<IActionResult> GetAllPermissions()
    {
        var permissions = await _adminService.GetAllPermissionsAsync();
        return Ok(permissions);
    }

    // 3. Belirli bir rolün sahip olduğu yetkilerin ID'lerini getir (Hangi checkbox'lar seçili olacak?)
    [HttpGet("roles/{roleId:int}/permissions")]
    [HasPermission("Role.Manage")]
    public async Task<IActionResult> GetRolePermissions(int roleId)
    {
        var permissionIds = await _adminService.GetRolePermissionsAsync(roleId);
        return Ok(permissionIds);
    }


    // 4. Checkbox'lardan gelen yeni yetkileri role kaydet
    [HttpPost("roles/{roleId:int}/permissions")]
    [HasPermission("Role.Manage")]
    public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] UpdateRolePermissionsRequest request)
    {
        int? currentUserId = null;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
            currentUserId = uid;

        var errorMessage = await _adminService.UpdateRolePermissionsAsync(roleId, request, currentUserId);

        if (errorMessage == "Rol bulunamadı.")
            return NotFound(new { message = errorMessage });
        else if (errorMessage != null)
            return BadRequest(new { message = errorMessage });

        return Ok(new { message = "Rol yetkileri başarıyla güncellendi." });
    }
    // --- KULLANICI CİHAZ VE ETİKET ATAMA YÖNETİMİ ---

    [HttpGet("users/{userId:int}/access")]
    [HasPermission("User.ManageComputers,User.ManageTags")]
    public async Task<IActionResult> GetUserAccess(int userId)
    {
        var result = await _adminService.GetUserAccessAsync(userId);

        if (result == null) return NotFound();

        return Ok(result);
    }

    // 2. Kullanıcıya doğrudan cihaz atama
    [HttpPost("users/{userId:int}/assign-computers")]
    [HasPermission("User.ManageComputers")]
    public async Task<IActionResult> AssignComputers(int userId, [FromBody] AssignComputersRequest req)
    {
        var errorMessage = await _adminService.AssignComputersAsync(userId, req);

        if (errorMessage != null)
            return NotFound(new { message = errorMessage });

        return Ok(new { message = "Cihaz atamaları güncellendi." });
    }

    // 3. Kullanıcıya etiket atama
    [HttpPost("users/{userId:int}/assign-tags")]
    [HasPermission("User.ManageTags")]
    public async Task<IActionResult> AssignTags(int userId, [FromBody] AssignTagsRequest req)
    {
        var errorMessage = await _adminService.AssignTagsAsync(userId, req);

        if (errorMessage != null)
            return NotFound(new { message = errorMessage });

        return Ok(new { message = "Etiket atamaları güncellendi." });
    }

    [HttpPost("tags/{tagId:int}/assign-computers")]
    [HasPermission("Tag.Manage")]
    public async Task<IActionResult> AssignComputersToTag(int tagId, [FromBody] AssignComputersToTagRequest req)
    {
        var errorMessage = await _adminService.AssignComputersToTagAsync(tagId, req);

        if (errorMessage != null)
            return NotFound(new { message = errorMessage });

        return Ok(new { message = "Cihaz atamaları başarıyla güncellendi." });
    }


    [HttpGet("computers/all")]
    [HasPermission("User.ManageComputers,Tag.Manage")]
    public async Task<IActionResult> GetAllComputersForAssignment()
    {
        var computers = await _adminService.GetAllComputersForAssignmentAsync();
        return Ok(computers);
    }

    [HttpGet("tags/{tagId:int}/assigned-computer-ids")]
    [HasPermission("Tag.Manage")]
    public async Task<IActionResult> GetTagAssignedComputerIds(int tagId)
    {
        var assignedIds = await _adminService.GetTagAssignedComputerIdsAsync(tagId);
        return Ok(assignedIds);
    }


    [HttpPost("roles")]
    [HasPermission("Role.Manage")]
    public async Task<IActionResult> CreateRole([FromBody] Staj2.Services.Models.CreateRoleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // İşlemi yapanın ID'sini alıyoruz
        int? currentUserId = null;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
            currentUserId = uid;

        // Servisi çağırıyoruz
        var errorMessage = await _adminService.CreateRoleAsync(request, currentUserId);

        if (errorMessage != null) // Eğer hata mesajı döndüyse (null değilse)
            return BadRequest(new { message = errorMessage });

        return Ok(new { message = "Rol başarıyla oluşturuldu." });
    }

    [HttpDelete("roles/{id:int}")]
    [HasPermission("Role.Manage")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        int? currentUserId = null;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
            currentUserId = uid;

        var errorMessage = await _adminService.DeleteRoleAsync(id, currentUserId);

        if (errorMessage == "Rol bulunamadı.")
            return NotFound(new { message = errorMessage });
        else if (errorMessage != null)
            return BadRequest(new { message = errorMessage });

        return Ok(new { message = "Rol başarıyla silindi." });
    }
}