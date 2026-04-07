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
    private readonly IAdminService _adminService;
    private readonly IConfiguration _config;

    public AdminController(IAdminService adminService, IConfiguration config)
    {
        _adminService = adminService;
        _config = config;
    }

    // --- YARDIMCI METOT ---
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;
        if (int.TryParse(userIdClaim, out int parsedId)) return parsedId;
        return null;
    }

    // --- KULLANICI YÖNETİMİ ---

    [HttpGet("users")]
    [HasPermission(AppPermissions.User_Read, AppPermissions.User_ManageRoles, AppPermissions.User_ManageComputers, AppPermissions.User_ManageTags)]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _adminService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpDelete("users/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _adminService.DeleteUserAsync(id, GetCurrentUserId());

        if (!result.isSuccess)
            return NotFound(new { message = result.message });

        return Ok(new { message = result.message });
    }

    [HttpPut("users/{userId}/change-roles")]
    [HasPermission(AppPermissions.User_ManageRoles)]
    public async Task<IActionResult> ChangeUserRoles(int userId, [FromBody] ChangeRolesRequest request)
    {
        var result = await _adminService.ChangeUserRolesAsync(userId, request);

        if (!result.isSuccess)
        {
            if (result.message.Contains("bulunamadı")) return NotFound(new { message = result.message });
            return BadRequest(new { message = result.message });
        }

        return Ok(new { message = result.message });
    }

    // --- KAYIT İSTEKLERİ YÖNETİMİ ---

    [HttpGet("requests")]
    [HasPermission(AppPermissions.User_Manage)]
    public async Task<IActionResult> PendingRequests()
    {
        var requests = await _adminService.GetPendingRequestsAsync();
        return Ok(requests);
    }

    [HttpPost("requests/reject")]
    [HasPermission(AppPermissions.User_Manage)]
    public async Task<IActionResult> RejectRequest([FromBody] RejectRegistrationRequest request)
    {
        var result = await _adminService.RejectRequestAsync(request, GetCurrentUserId());

        if (!result.IsSuccess)
        {
            if (result.message.Contains("bulunamadı")) return NotFound(new { message = result.message });
            return BadRequest(new { message = result.message });
        }

        return Ok(new { message = result.message });
    }

    [HttpPost("requests/approve/{id}")]
    [HasPermission(AppPermissions.User_Manage)]
    public async Task<IActionResult> ApproveRequest(int id, [FromBody] ChangeRoleRequest? req)
    {
        var result = await _adminService.ApproveRequestAsync(id, req, GetCurrentUserId());

        if (!result.IsSuccess)
        {
            if (result.message.Contains("bulunamadı")) return NotFound(new { message = result.message });
            return BadRequest(new { message = result.message });
        }

        return Ok(new { message = result.message });
    }

    // --- ETİKET YÖNETİMİ ---

    [HttpGet("tags")]
    [HasPermission(AppPermissions.None)]
    public async Task<IActionResult> GetTags()
    {
        var tags = await _adminService.GetTagsAsync();
        return Ok(tags);
    }

    [HttpPost("tags")]
    [HasPermission(AppPermissions.Tag_Manage)]
    public async Task<IActionResult> CreateTag([FromBody] TagCreateRequest request)
    {
        var result = await _adminService.CreateTagAsync(request, GetCurrentUserId());

        if (!result.IsSuccess)
            return BadRequest(new { message = result.message });

        // BURASI DÜZELTİLDİ: Artık UI'ın beklediği "message" parametresini de dönüyoruz
        return Ok(new { message = result.message, data = result.CreatedTag });
    }

    [HttpDelete("tags/{id:int}")]
    [HasPermission(AppPermissions.Tag_Manage)]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var result = await _adminService.DeleteTagAsync(id, GetCurrentUserId());

        if (!result.isSuccess)
            return NotFound(new { message = result.message });

        return Ok(new { message = result.message });
    }

    [HttpPost("tags/{tagId:int}/assign-computers")]
    [HasPermission(AppPermissions.Tag_Manage)]
    public async Task<IActionResult> AssignComputersToTag(int tagId, [FromBody] AssignComputersToTagRequest req)
    {
        var result = await _adminService.AssignComputersToTagAsync(tagId, req);

        if (!result.isSuccess)
            return NotFound(new { message = result.message });

        return Ok(new { message = result.message });
    }

    [HttpGet("tags/{tagId:int}/assigned-computer-ids")]
    [HasPermission(AppPermissions.Tag_Manage)]
    public async Task<IActionResult> GetTagAssignedComputerIds(int tagId)
    {
        var assignedIds = await _adminService.GetTagAssignedComputerIdsAsync(tagId);
        return Ok(assignedIds);
    }

    // --- ROL VE YETKİ YÖNETİMİ ---

    [HttpGet("roles")]
    [HasPermission(AppPermissions.None)]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _adminService.GetRolesAsync();
        return Ok(roles);
    }

    [HttpGet("permissions")]
    [HasPermission(AppPermissions.Role_Manage)]
    public async Task<IActionResult> GetAllPermissions()
    {
        var permissions = await _adminService.GetAllPermissionsAsync();
        return Ok(permissions);
    }

    [HttpGet("roles/{roleId:int}/permissions")]
    [HasPermission(AppPermissions.Role_Manage)]
    public async Task<IActionResult> GetRolePermissions(int roleId)
    {
        var permissionIds = await _adminService.GetRolePermissionsAsync(roleId);
        return Ok(permissionIds);
    }

    [HttpPost("roles/{roleId:int}/permissions")]
    [HasPermission(AppPermissions.Role_Manage)]
    public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] UpdateRolePermissionsRequest request)
    {
        var result = await _adminService.UpdateRolePermissionsAsync(roleId, request, GetCurrentUserId());

        if (!result.isSuccess)
        {
            if (result.message.Contains("bulunamadı")) return NotFound(new { message = result.message });
            return BadRequest(new { message = result.message });
        }

        return Ok(new { message = result.message });
    }

    [HttpPost("roles")]
    [HasPermission(AppPermissions.Role_Manage)]
    public async Task<IActionResult> CreateRole([FromBody] Staj2.Services.Models.CreateRoleRequest request)
    {
        var result = await _adminService.CreateRoleAsync(request, GetCurrentUserId());

        if (!result.isSuccess)
            return BadRequest(new { message = result.message });

        return Ok(new { message = result.message });
    }

    [HttpDelete("roles/{id:int}")]
    [HasPermission(AppPermissions.Role_Manage)]
    public async Task<IActionResult> DeleteRole(int id)
    {
        var result = await _adminService.DeleteRoleAsync(id, GetCurrentUserId());

        if (!result.isSuccess)
        {
            if (result.message.Contains("bulunamadı")) return NotFound(new { message = result.message });
            return BadRequest(new { message = result.message });
        }

        return Ok(new { message = result.message });
    }

    // --- KULLANICI CİHAZ VE ETİKET ATAMA YÖNETİMİ ---

    [HttpGet("users/{userId:int}/access")]
    [HasPermission(AppPermissions.User_ManageComputers, AppPermissions.User_ManageTags)]
    public async Task<IActionResult> GetUserAccess(int userId)
    {
        var result = await _adminService.GetUserAccessAsync(userId);

        if (result == null) return NotFound(new { message = "Kullanıcı bulunamadı." });

        return Ok(result);
    }

    [HttpPost("users/{userId:int}/assign-computers")]
    [HasPermission(AppPermissions.User_ManageComputers)]
    public async Task<IActionResult> AssignComputers(int userId, [FromBody] AssignComputersRequest req)
    {
        var result = await _adminService.AssignComputersAsync(userId, req);

        if (!result.isSuccess)
            return NotFound(new { message = result.message });

        return Ok(new { message = result.message });
    }

    [HttpPost("users/{userId:int}/assign-tags")]
    [HasPermission(AppPermissions.User_ManageTags)]
    public async Task<IActionResult> AssignTags(int userId, [FromBody] AssignTagsRequest req)
    {
        var result = await _adminService.AssignTagsAsync(userId, req);

        if (!result.isSuccess)
            return NotFound(new { message = result.message });

        return Ok(new { message = result.message });
    }

    [HttpGet("computers/all")]
    [HasPermission(AppPermissions.User_ManageComputers, AppPermissions.Tag_Manage)]
    public async Task<IActionResult> GetAllComputersForAssignment()
    {
        var computers = await _adminService.GetAllComputersForAssignmentAsync();
        return Ok(computers);
    }
}