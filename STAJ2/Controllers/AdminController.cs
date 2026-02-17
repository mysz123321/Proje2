using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using STAJ2.Models; // Modelleri görmek için bunu ekledik

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Yönetici")] // Sadece Yönetici
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) { _db = db; }

    // --- KULLANICI YÖNETİMİ ---

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers() =>
        Ok(await _db.Users.Include(u => u.Roles)
                          .OrderBy(u => u.Username)
                          .Select(u => new { u.Id, u.Username, u.Email, Roles = u.Roles.Select(r => r.Name).ToList() })
                          .ToListAsync());

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user != null) { _db.Users.Remove(user); await _db.SaveChangesAsync(); }
        return Ok();
    }

    [HttpPut("users/{userId}/change-roles")]
    public async Task<IActionResult> ChangeUserRoles(int userId, [FromBody] ChangeRolesRequest request)
    {
        var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return NotFound();

        var newRoles = await _db.Roles.Where(r => request.NewRoleIds.Contains(r.Id)).ToListAsync();
        if (newRoles.Count == 0) return BadRequest("En az bir geçerli rol seçilmelidir.");

        user.Roles.Clear();
        foreach (var role in newRoles) user.Roles.Add(role);

        await _db.SaveChangesAsync();
        return Ok();
    }

    // --- KAYIT İSTEKLERİ ---

    [HttpGet("requests")]
    public async Task<IActionResult> PendingRequests() =>
        Ok(await _db.RegistrationRequests.Where(x => x.Status == RegistrationStatus.Pending).ToListAsync());

    // Onaylama (Approve) metodu buraya eklenebilir.
    [HttpPost("approve/{id}")]
    public async Task<IActionResult> ApproveRequest(int id, [FromBody] ChangeRoleRequest req)
    {
        // Buraya onaylama mantığı (User oluşturma vb.) gelecek.
        // Eğer RegistrationController'da varsa buraya gerek yok, ama admin panelinden çağrılıyorsa burada olmalı.
        return Ok();
    }

    // --- ETİKET YÖNETİMİ (Sadece Oluşturma ve Silme) ---

    [HttpGet("tags")]
    [Authorize(Roles = "Yönetici,Görüntüleyici,Denetleyici")] // Herkes etiketleri görebilir
    public async Task<IActionResult> GetTags() => Ok(await _db.Tags.ToListAsync());

    [HttpPost("tags")]
    public async Task<IActionResult> CreateTag([FromBody] TagCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("İsim boş olamaz.");
        if (await _db.Tags.AnyAsync(t => t.Name == request.Name)) return BadRequest("Bu etiket zaten var.");

        var tag = new Tag { Name = request.Name.Trim() };
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();
        return Ok(tag);
    }

    [HttpDelete("tags/{id:int}")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var tag = await _db.Tags.FindAsync(id);
        if (tag == null) return NotFound();
        _db.Tags.Remove(tag);
        await _db.SaveChangesAsync();
        return Ok();
    }
}