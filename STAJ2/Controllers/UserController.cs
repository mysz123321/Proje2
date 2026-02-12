using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Infrastructure.Data;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // giriş yapan herkes görebilir
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var list = await _db.Users
            .Include(u => u.Role)
            .OrderBy(u => u.Username)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                Role = u.Role.Name
            })
            .ToListAsync();

        return Ok(list);
    }
}
