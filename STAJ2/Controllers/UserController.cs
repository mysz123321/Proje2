// STAJ2/Controllers/UserController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Infrastructure.Data;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var list = await _db.Users.Include(u => u.Role).ToListAsync();
        return Ok(list);
    }

    // --- BU METODU EKLE (404 Hatasını çözer) ---
    [HttpGet("tags")]
    public async Task<IActionResult> GetTags()
    {
        return Ok(await _db.Tags.OrderBy(t => t.Name).Select(t => new { t.Id, t.Name }).ToListAsync());
    }
}