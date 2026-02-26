using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Infrastructure.Data;
using STAJ2.Authorization;
using STAJ2.Models; // Request modellerini (DTO) görmek için
using System.Globalization;
using System.Security.Claims;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComputerController : ControllerBase
{
    private readonly AppDbContext _db;
    public ComputerController(AppDbContext db) { _db = db; }

    // 1. Cihaz Detayı
    [HttpGet("{id:int}")]
    [HasPermission("Computer.Read")]
    public async Task<IActionResult> GetComputer(int id)
    {
        if (!await CheckComputerAccessAsync(id)) return Forbid(); // GÜVENLİK KONTROLÜ

        var computer = await _db.Computers.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (computer == null) return NotFound();
        return Ok(new { computer.Id, computer.CpuThreshold, computer.RamThreshold, Tags = computer.Tags.Select(t => t.Name) });
    }

    // 2. Disk Listesi
    [HttpGet("{computerId:int}/disks")]
    [HasPermission("Computer.Read")]
    public async Task<IActionResult> GetComputerDisks(int computerId)
    {
        if (!await CheckComputerAccessAsync(computerId)) return Forbid(); // GÜVENLİK KONTROLÜ

        return Ok(await _db.ComputerDisks.Where(d => d.ComputerId == computerId).ToListAsync());
    }

    // 3. Eşik Değerlerini Güncelle (0-100 Kontrolü Eklendi)
    [HttpPut("update-thresholds/{computerId:int}")]
    [HasPermission("Computer.SetThreshold")]
    public async Task<IActionResult> UpdateThresholds(int computerId, [FromBody] UpdateThresholdsRequest request)
    {
        if (!await CheckComputerAccessAsync(computerId)) return Forbid();
        
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
    [HasPermission("Computer.AssignTag")]
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
    [HasPermission("Computer.Rename")]
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
    // 6. Belirli bir tarih aralığındaki metrik geçmişini getir
    [HttpGet("{id:int}/metrics-history")]
    [HasPermission("Computer.Filter")]
    public async Task<IActionResult> GetMetricsHistory(int id, [FromQuery] string start, [FromQuery] string end)
    {
        // Tarih formatı: "yyyy-MM-ddTHH:mm" (HTML5 datetime-local formatı)
        if (!DateTime.TryParse(start, out DateTime startTime) || !DateTime.TryParse(end, out DateTime endTime))
        {
            return BadRequest("Geçersiz tarih formatı.");
        }

        // 1. CPU ve RAM metriklerini çek
        var cpuRamMetrics = await _db.ComputerMetrics
            .Where(m => m.ComputerId == id && m.CreatedAt >= startTime && m.CreatedAt <= endTime)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new { m.CreatedAt, m.CpuUsage, m.RamUsage })
            .ToListAsync();

        // 2. Disk metriklerini çek (Bilgisayara bağlı tüm diskler için)
        var diskMetrics = await _db.DiskMetrics
            .Include(m => m.ComputerDisk)
            .Where(m => m.ComputerDisk.ComputerId == id && m.CreatedAt >= startTime && m.CreatedAt <= endTime)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new {
                m.CreatedAt,
                m.UsedPercent,
                DiskName = m.ComputerDisk.DiskName
            })
            .ToListAsync();

        return Ok(new
        {
            CpuRam = cpuRamMetrics,
            Disks = diskMetrics
        });
    }
    [HttpGet]
    [HasPermission("Computer.Read")]
    public async Task<IActionResult> GetAllComputers()
    {
        // 1. İsteği yapan kullanıcının ID'sini JWT'den alıyoruz
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int userId = int.TryParse(userIdString, out int id) ? id : 0;

        // 2. Kullanıcının tüm cihazları görme yetkisi var mı? 
        // (Yönetici rolü veya "Computer.Filter" yetkisi gibi bir üst yetki belirleyebilirsiniz)
        bool canSeeAll = User.IsInRole("Yönetici");

        // 3. Sorguyu başlatıyoruz
        var query = _db.Computers.IgnoreQueryFilters().Include(c => c.Tags).AsQueryable();

        // 4. Eğer tümünü görme yetkisi YOKSA, sorguyu filtreliyoruz
        if (!canSeeAll)
        {
            // Kullanıcının DOĞRUDAN erişimi olan cihaz ID'leri
            var accessibleComputerIds = await _db.UserComputerAccesses
                .Where(uca => uca.UserId == userId)
                .Select(uca => uca.ComputerId)
                .ToListAsync();

            // Kullanıcının ETİKET ÜZERİNDEN erişimi olan etiket ID'leri
            var accessibleTagIds = await _db.UserTagAccesses
                .Where(uta => uta.UserId == userId)
                .Select(uta => uta.TagId)
                .ToListAsync();

            // Sorguya filtreyi ekle: Cihaz doğrudan atanmış VEYA cihazın üzerindeki bir etiket kullanıcıya atanmış olmalı
            query = query.Where(c =>
                accessibleComputerIds.Contains(c.Id) ||
                c.Tags.Any(t => accessibleTagIds.Contains(t.Id)));
        }

        var computers = await query.ToListAsync();

        var result = computers.Select(c => new
        {
            id = c.Id,
            machineName = c.MachineName,
            displayName = c.DisplayName,
            ipAddress = c.IpAddress,
            lastSeen = c.LastSeen,
            tags = c.Tags.Select(t => t.Name).ToList(),
            isDeleted = c.IsDeleted,
            isActive = (DateTime.Now - c.LastSeen).TotalSeconds <= 150
        })
        .OrderBy(c => c.isDeleted).ThenByDescending(c => c.isActive).ThenByDescending(c => c.lastSeen);

        return Ok(result);
    }

    // 8. Cihaz Silme (Sadece Pasif Olanlar İçin Soft Delete)
    [HttpDelete("{id:int}")]
    [HasPermission("Computer.Delete")]
    public async Task<IActionResult> DeleteComputer(int id)
    {
        var computer = await _db.Computers.FindAsync(id);
        if (computer == null) return NotFound(new { message = "Bilgisayar bulunamadı." });

        bool isActive = (DateTime.Now - computer.LastSeen).TotalSeconds <= 150;
        if (isActive)
        {
            return BadRequest(new { message = "Aktif olan bir bilgisayarı silemezsiniz. Lütfen önce ajanı durdurun." });
        }

        // Soft delete (Bir önceki adımda eklemiştik)
        computer.IsDeleted = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Bilgisayar başarıyla silindi." });
    }


    private async Task<bool> CheckComputerAccessAsync(int computerId)
    {
        if (User.IsInRole("Yönetici")) return true;

        // 2. Kullanıcı ID'sini al
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int userId = int.TryParse(userIdString, out int id) ? id : 0;
        if (userId == 0) return false;

        // 3. Doğrudan cihaz ataması var mı kontrol et
        bool hasDirectAccess = await _db.UserComputerAccesses
            .AnyAsync(uca => uca.UserId == userId && uca.ComputerId == computerId);
        if (hasDirectAccess) return true;

        // 4. Etiket üzerinden ataması var mı kontrol et
        var computerTagIds = await _db.Computers
            .Where(c => c.Id == computerId)
            .SelectMany(c => c.Tags.Select(t => t.Id))
            .ToListAsync();

        bool hasTagAccess = await _db.UserTagAccesses
            .AnyAsync(uta => uta.UserId == userId && computerTagIds.Contains(uta.TagId));

        return hasTagAccess;
    }

    [HttpGet("tags")]
    [Authorize] // Özel bir yetki istemiyoruz, sisteme giren herkes kendi etiketlerini görebilsin
    public async Task<IActionResult> GetMyTags()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int userId = int.TryParse(userIdString, out int id) ? id : 0;

        bool canSeeAll = User.IsInRole("Yönetici");

        var query = _db.Tags.AsQueryable();

        // Eğer yönetici değilse, sadece kendisine izin verilen etiketleri filtrele
        if (!canSeeAll)
        {
            var accessibleComputerIds = await _db.UserComputerAccesses
                .Where(uca => uca.UserId == userId).Select(uca => uca.ComputerId).ToListAsync();

            var accessibleTagIds = await _db.UserTagAccesses
                .Where(uta => uta.UserId == userId).Select(uta => uta.TagId).ToListAsync();

            // Etiketin kendisi kullanıcıya atanmış olabilir VEYA 
            // Kullanıcıya doğrudan atanmış bir cihazın üzerinde o etiket olabilir
            query = query.Where(t =>
                accessibleTagIds.Contains(t.Id) ||
                t.Computers.Any(c => accessibleComputerIds.Contains(c.Id)));
        }

        var tags = await query.Select(t => new { t.Id, t.Name }).ToListAsync();
        return Ok(tags);
    }

}