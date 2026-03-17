namespace Staj2.Services.Models.Auth;

public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}
