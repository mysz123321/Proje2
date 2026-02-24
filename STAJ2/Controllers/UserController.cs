using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Infrastructure.Data;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Giriş yapan herkes erişebilir
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var list = await _db.Users.Include(u => u.Roles).OrderBy(u => u.Username)
            .Select(u => new { u.Id, u.Username, u.Email, Roles = u.Roles.Select(r => r.Name).ToList() })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("tags")]
    public async Task<IActionResult> GetTags()
    {
        return Ok(await _db.Tags.ToListAsync());
    }
}