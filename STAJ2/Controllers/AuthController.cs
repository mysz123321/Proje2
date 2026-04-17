using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Staj2.Services.Models.Auth;
using Staj2.Services.Interfaces;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.IsSuccess)
            return Unauthorized(new { message = result.Message });

        return Ok(result.Data);
    }

    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest req)
    {
        var result = await _authService.SetPasswordAsync(req);

        if (!result.IsSuccess)
        {
            // Eski isConflict yapısı yerine, mesaj içeriğinden durumu yakalıyoruz
            if (result.Message != null && result.Message.Contains("zaten"))
                return Conflict(new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }

        // Servisten gelen dinamik başarı mesajını dönüyoruz
        return Ok(new { message = result.Message });
    }

    [HttpGet("my-permissions")]
    [Authorize]
    public async Task<IActionResult> GetMyPermissions()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                          User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized(new { message = "Kullanıcı kimliği doğrulanamadı." });

        var result = await _authService.GetMyPermissionsAsync(userId);

        if (!result.IsSuccess)
            return NotFound(new { message = result.Message });

        return Ok(result.Data);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);

        if (!result.IsSuccess)
            return Unauthorized(new { message = result.Message });

        return Ok(result.Data);
    }

    public class RefreshRequest { public string RefreshToken { get; set; } = null!; }
}