namespace Staj2.Services.Models.Auth;

public class SetPasswordRequest
{
    public string Token { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
