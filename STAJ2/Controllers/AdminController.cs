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
        var result = await _adminService.GetAllUsersAsync();
        return Ok(result.Data);
    }

    [HttpDelete("users/{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _adminService.DeleteUserAsync(id, GetCurrentUserId());

        if (!result.IsSuccess)
            return NotFound(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpPut("users/{userId}/change-roles")]
    [HasPermission(AppPermissions.User_ManageRoles)]
    public async Task<IActionResult> ChangeUserRoles(int userId, [FromBody] ChangeRolesRequest request)
    {
        var result = await _adminService.ChangeUserRolesAsync(userId, request);

        if (!result.IsSuccess)
        {
            if (result.Message != null && result.Message.Contains("bulunamadı"))
                return NotFound(new { message = result.Message });
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    // --- KAYIT İSTEKLERİ YÖNETİMİ ---

    [HttpGet("requests")]
    [HasPermission(AppPermissions.User_Manage)]
    public async Task<IActionResult> PendingRequests()
    {
        var result = await _adminService.GetPendingRequestsAsync();
        return Ok(result.Data);
    }

    [HttpPost("requests/reject")]
    [HasPermission(AppPermissions.User_Manage)]
    public async Task<IActionResult> RejectRequest([FromBody] RejectRegistrationRequest request)
    {
        var result = await _adminService.RejectRequestAsync(request, GetCurrentUserId());

        if (!result.IsSuccess)
        {
            if (result.Message != null && result.Message.Contains("bulunamadı"))
                return NotFound(new { message = result.Message });
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpPost("requests/approve/{id}")]
    [HasPermission(AppPermissions.User_Manage)]
    public async Task<IActionResult> ApproveRequest(int id, [FromBody] ChangeRoleRequest? req)
    {
        var result = await _adminService.ApproveRequestAsync(id, req, GetCurrentUserId());

        if (!result.IsSuccess)
        {
            if (result.Message != null && result.Message.Contains("bulunamadı"))
                return NotFound(new { message = result.Message });
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    // --- ETİKET YÖNETİMİ ---

    [HttpGet("tags")]
    [HasPermission(AppPermissions.None)]
    public async Task<IActionResult> GetTags()
    {
        var result = await _adminService.GetTagsAsync();
        return Ok(result.Data);
    }

    [HttpPost("tags")]
    [HasPermission(AppPermissions.Tag_Manage)]
    public async Task<IActionResult> CreateTag([FromBody] TagCreateRequest request)
    {
        var result = await _adminService.CreateTagAsync(request, GetCurrentUserId());

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Message });

        // result.Data içinde { id = tag.Id, name = tag.Name } objesi var
        return Ok(new { message = result.Message, data = result.Data });
    }

    [HttpDelete("tags/{id:int}")]
    [HasPermission(AppPermissions.Tag_Manage)]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var result = await _adminService.DeleteTagAsync(id, GetCurrentUserId());

        if (!result.IsSuccess)
            return NotFound(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpPost("tags/{tagId:int}/assign-computers")]
    [HasPermission(AppPermissions.Tag_Manage)]
    public async Task<IActionResult> AssignComputersToTag(int tagId, [FromBody] AssignComputersToTagRequest req)
    {
        var result = await _adminService.AssignComputersToTagAsync(tagId, req);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpGet("tags/{tagId:int}/assigned-computer-ids")]
    [HasPermission(AppPermissions.Tag_Manage)]
    public async Task<IActionResult> GetTagAssignedComputerIds(int tagId)
    {
        var result = await _adminService.GetTagAssignedComputerIdsAsync(tagId);
        return Ok(result.Data);
    }

    // --- ROL VE YETKİ YÖNETİMİ ---

    [HttpGet("roles")]
    [HasPermission(AppPermissions.None)]
    public async Task<IActionResult> GetRoles()
    {
        var result = await _adminService.GetRolesAsync();
        return Ok(result.Data);
    }

    [HttpGet("permissions")]
    [HasPermission(AppPermissions.Role_Manage)]
    public async Task<IActionResult> GetAllPermissions()
    {
        var result = await _adminService.GetAllPermissionsAsync();
        return Ok(result.Data);
    }

    [HttpGet("roles/{roleId:int}/permissions")]
    [HasPermission(AppPermissions.Role_Manage)]
    public async Task<IActionResult> GetRolePermissions(int roleId)
    {
        var result = await _adminService.GetRolePermissionsAsync(roleId);
        return Ok(result.Data);
    }

    [HttpPost("roles/{roleId:int}/permissions")]
    [HasPermission(AppPermissions.Role_Manage)]
    public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] UpdateRolePermissionsRequest request)
    {
        var result = await _adminService.UpdateRolePermissionsAsync(roleId, request, GetCurrentUserId());

        if (!result.IsSuccess)
        {
            if (result.Message != null && result.Message.Contains("bulunamadı"))
                return NotFound(new { message = result.Message });
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpPost("roles")]
    [HasPermission(AppPermissions.Role_Manage)]
    public async Task<IActionResult> CreateRole([FromBody] Staj2.Services.Models.CreateRoleRequest request)
    {
        var result = await _adminService.CreateRoleAsync(request, GetCurrentUserId());

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpDelete("roles/{id:int}")]
    [HasPermission(AppPermissions.Role_Manage)]
    public async Task<IActionResult> DeleteRole(int id)
    {
        var result = await _adminService.DeleteRoleAsync(id, GetCurrentUserId());

        if (!result.IsSuccess)
        {
            if (result.Message != null && result.Message.Contains("bulunamadı"))
                return NotFound(new { message = result.Message });
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    // --- KULLANICI CİHAZ VE ETİKET ATAMA YÖNETİMİ ---

    [HttpGet("users/{userId:int}/access")]
    [HasPermission(AppPermissions.User_ManageComputers, AppPermissions.User_ManageTags)]
    public async Task<IActionResult> GetUserAccess(int userId)
    {
        var result = await _adminService.GetUserAccessAsync(userId);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Message });

        return Ok(result.Data);
    }

    [HttpPost("users/{userId:int}/assign-computers")]
    [HasPermission(AppPermissions.User_ManageComputers)]
    public async Task<IActionResult> AssignComputers(int userId, [FromBody] AssignComputersRequest req)
    {
        var result = await _adminService.AssignComputersAsync(userId, req);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpPost("users/{userId:int}/assign-tags")]
    [HasPermission(AppPermissions.User_ManageTags)]
    public async Task<IActionResult> AssignTags(int userId, [FromBody] AssignTagsRequest req)
    {
        var result = await _adminService.AssignTagsAsync(userId, req);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpGet("computers/all")]
    [HasPermission(AppPermissions.User_ManageComputers, AppPermissions.Tag_Manage)]
    public async Task<IActionResult> GetAllComputersForAssignment()
    {
        var result = await _adminService.GetAllComputersForAssignmentAsync();
        return Ok(result.Data);
    }
}