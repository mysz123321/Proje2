using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Staj2.Services.Interfaces;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Sadece giriş yapmış kullanıcılar menüleri çekebilir
public class UiController : ControllerBase
{
    private readonly IUiService _uiService;

    public UiController(IUiService uiService)
    {
        _uiService = uiService;
    }

    // DRY (Don't Repeat Yourself) prensibi için userId getirme işlemini metoda aldık
    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                          User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        return (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id)) ? id : 0;
    }

    [HttpGet("sidebar-items")]
    public async Task<IActionResult> GetSidebarItems()
    {
        int userId = GetUserId();
        if (userId == 0) return Unauthorized("Kullanıcı kimliği doğrulanamadı.");

        var result = await _uiService.GetSidebarItemsAsync(userId);

        if (!result.IsSuccess) return NotFound(result.ErrorMessage);

        return Ok(result.Data);
    }

    [HttpGet("my-permissions")]
    public async Task<IActionResult> GetMyPermissions()
    {
        int userId = GetUserId();
        if (userId == 0) return Unauthorized("Kullanıcı kimliği doğrulanamadı.");

        var result = await _uiService.GetMyPermissionsAsync(userId);

        if (!result.IsSuccess) return NotFound(result.ErrorMessage);

        return Ok(result.Permissions);
    }

    //[HttpGet("user-actions")]
    //[Authorize]
    //public async Task<IActionResult> GetUserActions()
    //{
    //    var actions = await _uiService.GetUserActionsAsync();
    //    return Ok(actions);
    //}
}