using Microsoft.EntityFrameworkCore;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;
using Staj2.Services.Models;

namespace Staj2.Services.Services;

public class ComputerService : IComputerService
{
    private readonly AppDbContext _db;

    public ComputerService(AppDbContext db)
    {
        _db = db;
    }

    // --- ORTAK YETKİ KONTROLÜ (Controller'dan Servise taşındı ve HttpContext'ten arındırıldı) ---
    private async Task<bool> CheckComputerAccessAsync(int computerId, int userId, bool isAdmin)
    {
        if (isAdmin) return true;
        if (userId == 0) return false;

        // Doğrudan cihaz ataması var mı?
        bool hasDirectAccess = await _db.UserComputerAccesses
            .AnyAsync(uca => uca.UserId == userId && uca.ComputerId == computerId);

        if (hasDirectAccess) return true;

        // Etiket üzerinden ataması var mı?
        var computerTagIds = await _db.Computers
            .Where(c => c.Id == computerId)
            .SelectMany(c => c.Tags.Select(t => t.Id))
            .ToListAsync();

        bool hasTagAccess = await _db.UserTagAccesses
            .AnyAsync(uta => uta.UserId == userId && computerTagIds.Contains(uta.TagId));

        return hasTagAccess;
    }

    // 1. Cihaz Detayı
    public async Task<(bool isForbidden, bool isNotFound, object? data)> GetComputerAsync(int id, int userId, bool isAdmin)
    {
        if (!await CheckComputerAccessAsync(id, userId, isAdmin))
            return (true, false, null); // Forbid

        var computer = await _db.Computers.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (computer == null)
            return (false, true, null); // NotFound

        var data = new { computer.Id, computer.CpuThreshold, computer.RamThreshold, Tags = computer.Tags.Select(t => t.Name) };
        return (false, false, data); // Ok
    }

    // 2. Disk Listesi
    public async Task<(bool isForbidden, object? data)> GetComputerDisksAsync(int computerId, int userId, bool isAdmin)
    {
        if (!await CheckComputerAccessAsync(computerId, userId, isAdmin))
            return (true, null); // Forbid

        var disks = await _db.ComputerDisks.Where(d => d.ComputerId == computerId).ToListAsync();
        return (false, disks); // Ok
    }

    // 3. Eşik Değerlerini Güncelle
    public async Task<(bool isForbidden, bool isNotFound, string? errorMessage)> UpdateThresholdsAsync(int computerId, UpdateThresholdsRequest request, int userId, bool isAdmin)
    {
        if (!await CheckComputerAccessAsync(computerId, userId, isAdmin))
            return (true, false, null); // Forbid

        // --- VALIDATION BAŞLANGICI ---
        if (request.CpuThreshold.HasValue && (request.CpuThreshold < 0 || request.CpuThreshold > 100))
            return (false, false, "CPU eşik değeri 0 ile 100 arasında olmalıdır.");

        if (request.RamThreshold.HasValue && (request.RamThreshold < 0 || request.RamThreshold > 100))
            return (false, false, "RAM eşik değeri 0 ile 100 arasında olmalıdır.");

        if (request.DiskThresholds != null)
        {
            foreach (var disk in request.DiskThresholds)
            {
                if (disk.ThresholdPercent.HasValue && (disk.ThresholdPercent < 0 || disk.ThresholdPercent > 100))
                    return (false, false, $"'{disk.DiskName}' diski için eşik değeri 0-100 arasında olmalıdır.");
            }
        }
        // --- VALIDATION SONU ---

        var computer = await _db.Computers.Include(c => c.Disks).FirstOrDefaultAsync(c => c.Id == computerId);
        if (computer == null)
            return (false, true, null); // NotFound

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

        return (false, false, null); // Ok
    }

    // 4. Etiket Atama
    public async Task<bool> UpdateComputerTagsAsync(int id, UpdateComputerTagsRequest request)
    {
        var computer = await _db.Computers.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (computer == null) return false;

        var newTags = await _db.Tags.Where(t => request.Tags.Contains(t.Name)).ToListAsync();
        computer.Tags.Clear();
        foreach (var tag in newTags) computer.Tags.Add(tag);

        await _db.SaveChangesAsync();
        return true;
    }

    // 5. İsim Değiştirme
    public async Task<(bool isSuccess, bool isNotFound, string? errorMessage)> UpdateDisplayNameAsync(UpdateComputerNameRequest request)
    {
        // --- VALIDATION (JSON Formatlı) ---
        if (!string.IsNullOrEmpty(request.NewDisplayName) && request.NewDisplayName.Length > 200)
            return (false, false, "Görünen isim 200 karakterden uzun olamaz.");

        if (!string.IsNullOrWhiteSpace(request.NewDisplayName))
        {
            // Veritabanında aynı isimde BAŞKA bir cihaz var mı diye kontrol ediyoruz
            bool isNameTaken = await _db.Computers
                .AnyAsync(c => c.DisplayName == request.NewDisplayName && c.Id != request.Id);

            if (isNameTaken)
                return (false, false, "Bu isim zaten başka bir cihaza ait. Lütfen farklı bir isim giriniz.");
        }

        var computer = await _db.Computers.FindAsync(request.Id);
        if (computer == null)
            return (false, true, "Bilgisayar bulunamadı."); // NotFound

        computer.DisplayName = request.NewDisplayName;
        await _db.SaveChangesAsync();

        return (true, false, null); // Başarılı
    }

    // 6. Belirli bir tarih aralığındaki metrik geçmişini getir
    public async Task<(bool isBadRequest, string? errorMessage, object? data)> GetMetricsHistoryAsync(int id, string start, string end)
    {
        // Tarih formatı: "yyyy-MM-ddTHH:mm" (HTML5 datetime-local formatı)
        if (!DateTime.TryParse(start, out DateTime startTime) || !DateTime.TryParse(end, out DateTime endTime))
        {
            return (true, "Geçersiz tarih formatı.", null); // BadRequest
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

        var data = new
        {
            CpuRam = cpuRamMetrics,
            Disks = diskMetrics
        };

        return (false, null, data);
    }

    // 7. Tüm Cihazları Getir
    public async Task<object> GetAllComputersAsync(int userId, bool isAdmin)
    {
        // 1. Mevcut erişimleri çek
        var accCompIds = await _db.UserComputerAccesses.Where(x => x.UserId == userId).Select(x => x.ComputerId).ToListAsync();
        var accTagIds = await _db.UserTagAccesses.Where(x => x.UserId == userId).Select(x => x.TagId).ToListAsync();

        var query = _db.Computers.IgnoreQueryFilters().AsQueryable();

        // 2. Admin olsa bile eğer bir kısıtlama listesi varsa onu uygula
        bool isRestricted = !isAdmin || (accCompIds.Count > 0 || accTagIds.Count > 0);

        if (isRestricted)
        {
            query = query.Where(c =>
                accCompIds.Contains(c.Id) ||
                _db.ComputerTags.Any(ct => ct.ComputerId == c.Id && !ct.IsDeleted && accTagIds.Contains(ct.TagId))
            );
        }

        // 3. Veritabanından veriyi çekerken sadece "Aktif (Silinmemiş)" etiketleri listeye dahil et
        var computersData = await query.Select(c => new {
            Computer = c,
            ActiveTags = _db.ComputerTags
                            .Where(ct => ct.ComputerId == c.Id && !ct.IsDeleted && !ct.Tag.IsDeleted)
                            .Select(ct => ct.Tag.Name)
                            .ToList()
        }).ToListAsync();

        //var now = DateTime.Now; // Not: Sunucu saatleri farklılık yaratmasın diye UtcNow önerilir ama mevcut kodun Now'dı, değiştirmeden devam edelim.
        //if (now.Kind == DateTimeKind.Utc) now = DateTime.Now; // Uyumluluk için eski DateTime.Now kalsın.

        // 4. İstemciye (UI) gidecek modeli oluştur
        var result = computersData.Select(x => new {
            id = x.Computer.Id,
            machineName = x.Computer.MachineName,
            displayName = x.Computer.DisplayName,
            ipAddress = x.Computer.IpAddress,
            lastSeen = x.Computer.LastSeen,
            tags = x.ActiveTags,
            isDeleted = x.Computer.IsDeleted,
            isActive = (DateTime.Now - x.Computer.LastSeen).TotalSeconds <= 150
        })
        .OrderBy(c => c.isDeleted).ThenByDescending(c => c.isActive).ThenByDescending(c => c.lastSeen);

        return result;
    }

    // 8. Cihaz Silme
    public async Task<(bool isNotFound, bool isBadRequest, string? errorMessage)> DeleteComputerAsync(int id)
    {
        var computer = await _db.Computers.FindAsync(id);
        if (computer == null)
            return (true, false, "Bilgisayar bulunamadı.");

        bool isActive = (DateTime.Now - computer.LastSeen).TotalSeconds <= 150;
        if (isActive)
        {
            return (false, true, "Aktif olan bir bilgisayarı silemezsiniz. Lütfen önce ajanı durdurun.");
        }

        computer.IsDeleted = true;
        await _db.SaveChangesAsync();

        return (false, false, null); // Başarılı
    }

    // 9. Kullanıcının Etiketlerini Getir
    public async Task<object> GetMyTagsAsync(int userId, bool isAdmin)
    {
        var query = _db.Tags.AsQueryable();

        if (!isAdmin)
        {
            var accessibleComputerIds = await _db.UserComputerAccesses
                .Where(uca => uca.UserId == userId).Select(uca => uca.ComputerId).ToListAsync();

            var accessibleTagIds = await _db.UserTagAccesses
                .Where(uta => uta.UserId == userId).Select(uta => uta.TagId).ToListAsync();

            query = query.Where(t =>
                accessibleTagIds.Contains(t.Id) ||
                t.Computers.Any(c => accessibleComputerIds.Contains(c.Id)));
        }

        var tags = await query.Select(t => new { t.Id, t.Name }).ToListAsync();
        return tags;
    }
}