using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using STAJ2.Services;
using STAJ2.Models.Agent;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Yönetici")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) { _db = db; }

    [HttpGet("computers/{id:int}")]
    public async Task<IActionResult> GetComputer(int id)
    {
        var computer = await _db.Computers.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (computer == null) return NotFound();
        return Ok(new { computer.Id, computer.CpuThreshold, computer.RamThreshold, Tags = computer.Tags.Select(t => t.Name) });
    }

    [HttpGet("computers/{computerId:int}/disks")]
    public async Task<IActionResult> GetComputerDisks(int computerId) => Ok(await _db.ComputerDisks.Where(d => d.ComputerId == computerId).ToListAsync());

    [HttpPut("update-thresholds/{computerId:int}")]
    public async Task<IActionResult> UpdateThresholds(int computerId, [FromBody] UpdateThresholdsRequest request)
    {
        var computer = await _db.Computers.Include(c => c.Disks).FirstOrDefaultAsync(c => c.Id == computerId);
        if (computer == null) return NotFound();
        computer.CpuThreshold = request.CpuThreshold;
        computer.RamThreshold = request.RamThreshold;
        if (request.DiskThresholds != null)
        {
            foreach (var dReq in request.DiskThresholds)
            {
                var disk = computer.Disks.FirstOrDefault(d => d.DiskName == dReq.DiskName);
                if (disk != null) disk.ThresholdPercent = dReq.ThresholdPercent;
            }
        }
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("tags")]
    [Authorize(Roles = "Yönetici,Görüntüleyici,Denetleyici")]
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

    // --- CİHAZ ETİKETLERİNİ GÜNCELLEME ---
    [HttpPut("computers/{id}/tags")]
    public async Task<IActionResult> UpdateComputerTags(int id, [FromBody] UpdateComputerTagsRequest request)
    {
        var computer = await _db.Computers.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (computer == null) return NotFound();
        var newTags = await _db.Tags.Where(t => request.Tags.Contains(t.Name)).ToListAsync();
        computer.Tags.Clear();
        foreach (var tag in newTags) computer.Tags.Add(tag);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("requests")]
    public async Task<IActionResult> PendingRequests() => Ok(await _db.RegistrationRequests.Where(x => x.Status == RegistrationStatus.Pending).ToListAsync());

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers() => Ok(await _db.Users.Include(u => u.Roles).OrderBy(u => u.Username).Select(u => new { u.Id, u.Username, u.Email, Roles = u.Roles.Select(r => r.Name).ToList() }).ToListAsync());

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user != null) { _db.Users.Remove(user); await _db.SaveChangesAsync(); }
        return Ok();
    }

    [HttpPut("users/{userId}/change-role")]
    public async Task<IActionResult> ChangeUserRole(int userId, [FromBody] ChangeRoleRequest request)
    {
        var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return NotFound();
        var newRole = await _db.Roles.FindAsync(request.NewRoleId);
        if (newRole == null) return BadRequest();
        user.Roles.Clear();
        user.Roles.Add(newRole);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("update-display-name")]
    public async Task<IActionResult> UpdateDisplayName([FromBody] UpdateComputerNameRequest request)
    {
        var computer = await _db.Computers.FindAsync(request.Id);
        if (computer != null) { computer.DisplayName = request.NewDisplayName; await _db.SaveChangesAsync(); }
        return Ok();
    }
}

public class TagCreateRequest { public string Name { get; set; } = null!; }
public class UpdateComputerNameRequest { public int Id { get; set; } public string NewDisplayName { get; set; } = null!; }
public class ChangeRoleRequest { public int NewRoleId { get; set; } }
public class UpdateThresholdsRequest { public double? CpuThreshold { get; set; } public double? RamThreshold { get; set; } public List<DiskThresholdItem>? DiskThresholds { get; set; } }
public class DiskThresholdItem { public string DiskName { get; set; } = null!; public double? ThresholdPercent { get; set; } }
public class UpdateComputerTagsRequest { public List<string> Tags { get; set; } = new(); }