using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using STAJ2.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using STAJ2.Models.Agent;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Yönetici")] // Yetki sorunu yaşarsan burayı geçici olarak yorum yap
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMailSender _mail;
    private readonly IConfiguration _config;

    public AdminController(AppDbContext db, IMailSender mail, IConfiguration config)
    {
        _db = db;
        _mail = mail;
        _config = config;
    }

    // --- 1. BİLGİSAYAR DETAYI (Ayarlar Modal'ı İçin) ---
    [HttpGet("computers/{id:int}")]
    public async Task<IActionResult> GetComputer(int id)
    {
        var computer = await _db.Computers.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (computer == null) return NotFound("Bilgisayar bulunamadı.");

        return Ok(new
        {
            computer.Id,
            computer.CpuThreshold,
            computer.RamThreshold,
            Tags = computer.Tags.Select(t => new { t.Id, t.Name })
        });
    }

    // --- 2. DİSKLERİ GETİRME ---
    [HttpGet("computers/{computerId:int}/disks")]
    public async Task<IActionResult> GetComputerDisks(int computerId)
    {
        var disks = await _db.ComputerDisks.Where(d => d.ComputerId == computerId).ToListAsync();
        return Ok(disks);
    }

    // --- 3. EŞİKLERİ GÜNCELLEME ---
    [HttpPut("update-thresholds/{computerId:int}")]
    public async Task<IActionResult> UpdateThresholds(int computerId, [FromBody] UpdateThresholdsRequest request)
    {
        var computer = await _db.Computers.Include(c => c.Disks).FirstOrDefaultAsync(c => c.Id == computerId);
        if (computer == null) return NotFound("Cihaz bulunamadı.");

        computer.CpuThreshold = request.CpuThreshold;
        computer.RamThreshold = request.RamThreshold;

        if (request.DiskThresholds != null)
        {
            foreach (var diskReq in request.DiskThresholds)
            {
                var disk = computer.Disks.FirstOrDefault(d => d.DiskName == diskReq.DiskName);
                if (disk != null) disk.ThresholdPercent = diskReq.ThresholdPercent;
            }
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = "Güncellendi." });
    }

    // --- 4. ETİKET YÖNETİMİ ---
    // STAJ2/Controllers/AdminController.cs içinde şu metodu bul ve değiştir:

    [HttpGet("tags")]
    [Authorize(Roles = "Yönetici,Görüntüleyici,Denetleyici")] // Yetkiyi genişlettik!
    public async Task<IActionResult> GetTags()
    {
        return Ok(await _db.Tags.ToListAsync());
    }

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

    [HttpPut("computers/{computerId:int}/tags")]
    public async Task<IActionResult> UpdateComputerTags(int computerId, [FromBody] List<int> tagIds)
    {
        var computer = await _db.Computers.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == computerId);
        if (computer == null) return NotFound();
        computer.Tags = await _db.Tags.Where(t => tagIds.Contains(t.Id)).ToListAsync();
        await _db.SaveChangesAsync();
        return Ok();
    }

    // --- DİĞER STANDART İŞLEMLER ---
    [HttpGet("requests")]
    public async Task<IActionResult> PendingRequests() => Ok(await _db.RegistrationRequests.Where(x => x.Status == RegistrationStatus.Pending).ToListAsync());

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers() => Ok(await _db.Users.Include(u => u.Role).Select(u => new { u.Id, u.Username, Role = u.Role.Name }).ToListAsync());

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user != null) { _db.Users.Remove(user); await _db.SaveChangesAsync(); }
        return Ok();
    }

    [HttpPut("users/{userId:int}/change-role")]
    public async Task<IActionResult> ChangeUserRole(int userId, [FromBody] ChangeRoleRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null) { user.RoleId = request.NewRoleId; await _db.SaveChangesAsync(); }
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

// --- TÜM DTO MODELLERİ ---
public class TagCreateRequest { public string Name { get; set; } = null!; }
public class UpdateComputerNameRequest { public int Id { get; set; } public string NewDisplayName { get; set; } = null!; }
public class ApproveRequest { public int RoleId { get; set; } }
public class ChangeRoleRequest { public int NewRoleId { get; set; } }
public class UpdateThresholdsRequest
{
    public double? CpuThreshold { get; set; }
    public double? RamThreshold { get; set; }
    public List<DiskThresholdItem>? DiskThresholds { get; set; }
}
public class DiskThresholdItem { public string DiskName { get; set; } = null!; public double? ThresholdPercent { get; set; } }