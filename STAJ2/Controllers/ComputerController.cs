using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Infrastructure.Data;
using STAJ2.Models; // Request modellerini (DTO) görmek için

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Yönetici,Denetleyici")] // Hem Yönetici hem Denetleyici erişebilir
public class ComputerController : ControllerBase
{
    private readonly AppDbContext _db;
    public ComputerController(AppDbContext db) { _db = db; }

    // 1. Cihaz Detayı
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetComputer(int id)
    {
        var computer = await _db.Computers.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (computer == null) return NotFound();
        return Ok(new { computer.Id, computer.CpuThreshold, computer.RamThreshold, Tags = computer.Tags.Select(t => t.Name) });
    }

    // 2. Disk Listesi
    [HttpGet("{computerId:int}/disks")]
    public async Task<IActionResult> GetComputerDisks(int computerId)
    {
        return Ok(await _db.ComputerDisks.Where(d => d.ComputerId == computerId).ToListAsync());
    }

    // 3. Eşik Değerlerini Güncelle (0-100 Kontrolü Eklendi)
    [HttpPut("update-thresholds/{computerId:int}")]
    public async Task<IActionResult> UpdateThresholds(int computerId, [FromBody] UpdateThresholdsRequest request)
    {
        // --- VALIDATION BAŞLANGICI ---
        if (request.CpuThreshold.HasValue && (request.CpuThreshold < 0 || request.CpuThreshold > 100))
            return BadRequest("CPU eşik değeri 0 ile 100 arasında olmalıdır.");

        if (request.RamThreshold.HasValue && (request.RamThreshold < 0 || request.RamThreshold > 100))
            return BadRequest("RAM eşik değeri 0 ile 100 arasında olmalıdır.");

        if (request.DiskThresholds != null)
        {
            foreach (var disk in request.DiskThresholds)
            {
                if (disk.ThresholdPercent.HasValue && (disk.ThresholdPercent < 0 || disk.ThresholdPercent > 100))
                    return BadRequest($"'{disk.DiskName}' diski için eşik değeri 0-100 arasında olmalıdır.");
            }
        }
        // --- VALIDATION SONU ---

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

    // 4. Etiket Atama
    [HttpPut("{id}/tags")]
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
    
    // 5. İsim Değiştirme
    [HttpPut("update-display-name")]
    public async Task<IActionResult> UpdateDisplayName([FromBody] UpdateComputerNameRequest request)
    {
        // --- VALIDATION (JSON Formatlı) ---
        if (!string.IsNullOrEmpty(request.NewDisplayName) && request.NewDisplayName.Length > 200)
        {
            return BadRequest(new { message = "Görünen isim 200 karakterden uzun olamaz." });
        }
        // ----------------------------------

        var computer = await _db.Computers.FindAsync(request.Id);
        if (computer == null) return NotFound(new { message = "Bilgisayar bulunamadı." });

        computer.DisplayName = request.NewDisplayName;
        await _db.SaveChangesAsync();

        return Ok(new { message = "İsim başarıyla güncellendi." });
    }
}