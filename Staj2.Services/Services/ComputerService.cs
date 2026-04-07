using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;
using Staj2.Services.Models;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Staj2.Services.Services;

public class ComputerService : IComputerService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    public ComputerService(AppDbContext db, IConfiguration config, IMemoryCache cache)
    {
        _db = db;
        _config = config;
        _cache = cache;
    }

    private async Task<bool> CheckComputerAccessAsync(int computerId, int userId, bool isAdmin)
    {
        if (isAdmin) return true;
        if (userId == 0) return false;

        bool hasDirectAccess = await _db.UserComputerAccesses
            .AnyAsync(uca => uca.UserId == userId && uca.ComputerId == computerId);

        if (hasDirectAccess) return true;

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
            return (true, false, null);

        var computer = await _db.Computers.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (computer == null)
            return (false, true, null);

        var data = new { computer.Id, computer.CpuThreshold, computer.RamThreshold, Tags = computer.Tags.Select(t => t.Name) };
        return (false, false, data);
    }

    // 2. Disk Listesi
    public async Task<(bool isForbidden, object? data)> GetComputerDisksAsync(int computerId, int userId, bool isAdmin)
    {
        if (!await CheckComputerAccessAsync(computerId, userId, isAdmin))
            return (true, null);

        var disks = await _db.ComputerDisks.Where(d => d.ComputerId == computerId).ToListAsync();
        return (false, disks);
    }

    // 3. Eşik Değerlerini Güncelle
    public async Task<(bool isForbidden, bool isNotFound, bool isBadRequest, string message)> UpdateThresholdsAsync(int computerId, UpdateThresholdsRequest request, int userId, bool isAdmin)
    {
        if (!await CheckComputerAccessAsync(computerId, userId, isAdmin))
            return (true, false, false, "Bu cihaza erişim yetkiniz bulunmamaktadır.");

        if (request.CpuThreshold.HasValue && (request.CpuThreshold < 0 || request.CpuThreshold > 100))
            return (false, false, true, "CPU eşik değeri 0 ile 100 arasında olmalıdır.");

        if (request.RamThreshold.HasValue && (request.RamThreshold < 0 || request.RamThreshold > 100))
            return (false, false, true, "RAM eşik değeri 0 ile 100 arasında olmalıdır.");

        if (request.DiskThresholds != null)
        {
            foreach (var disk in request.DiskThresholds)
            {
                if (disk.ThresholdPercent.HasValue && (disk.ThresholdPercent < 0 || disk.ThresholdPercent > 100))
                    return (false, false, true, $"'{disk.DiskName}' diski için eşik değeri 0-100 arasında olmalıdır.");
            }
        }

        var computer = await _db.Computers.Include(c => c.Disks).FirstOrDefaultAsync(c => c.Id == computerId);
        if (computer == null)
            return (false, true, false, "Bilgisayar bulunamadı.");

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

        return (false, false, false, "Sınırlar başarıyla kaydedildi.");
    }

    // 4. Etiket Atama
    public async Task<(bool isNotFound, string message)> UpdateComputerTagsAsync(int id, UpdateComputerTagsRequest request)
    {
        var computer = await _db.Computers.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (computer == null)
            return (true, "Bilgisayar bulunamadı.");

        var newTags = await _db.Tags.Where(t => request.Tags.Contains(t.Name)).ToListAsync();
        computer.Tags.Clear();
        foreach (var tag in newTags) computer.Tags.Add(tag);

        await _db.SaveChangesAsync();

        return (false, "Etiketler cihaza başarıyla atandı.");
    }

    // 5. İsim Değiştirme (TÜM VALIDASYONLAR EKLENDİ)
    public async Task<(bool isSuccess, bool isNotFound, string message)> UpdateDisplayNameAsync(UpdateComputerNameRequest request)
    {
        // JS'den taşınan kural 1: Boş bırakılamaz
        if (string.IsNullOrWhiteSpace(request.NewDisplayName))
            return (false, false, "İsim alanı boş bırakılamaz.");

        // JS'den taşınan kural 2: 200 Karakter sınırı
        if (request.NewDisplayName.Length > 200)
            return (false, false, "Görünen isim 200 karakterden uzun olamaz.");

        // Veritabanı çakışma kontrolü
        bool isNameTaken = await _db.Computers
            .AnyAsync(c => c.DisplayName == request.NewDisplayName && c.Id != request.Id);

        if (isNameTaken)
            return (false, false, "Bu isim zaten başka bir cihaza ait. Lütfen farklı bir isim giriniz.");

        var computer = await _db.Computers.FindAsync(request.Id);
        if (computer == null)
            return (false, true, "Bilgisayar bulunamadı.");

        computer.DisplayName = request.NewDisplayName;
        await _db.SaveChangesAsync();

        return (true, false, "Cihaz ismi başarıyla güncellendi.");
    }

    // 6. Belirli bir tarih aralığındaki metrik geçmişini getir (TÜM VALIDASYONLAR EKLENDİ)
    public async Task<(bool isBadRequest, string? errorMessage, object? data)> GetMetricsHistoryAsync(int id, string start, string end)
    {
        if (id <= 0)
            return (true, "Lütfen analiz yapmak için sol menüden bir cihaz seçiniz.", null);

        // JS'den taşınan kural 1: Boş bırakılamaz
        if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end))
            return (true, "Lütfen tarih aralığı seçiniz.", null);

        if (!DateTime.TryParse(start, out DateTime startTime) || !DateTime.TryParse(end, out DateTime endTime))
        {
            return (true, "Geçersiz tarih formatı.", null);
        }

        // JS'den taşınan kural 2: Başlangıç bitişten büyük olamaz
        if (startTime > endTime)
            return (true, "Başlangıç tarihi bitiş tarihinden sonra olamaz.", null);

        // JS'den taşınan kural 3: Maksimum 7 gün kuralı
        if ((endTime - startTime).TotalDays > 7)
            return (true, "Lütfen maksimum 7 günlük bir tarih aralığı seçiniz.", null);

        var cpuRamMetrics = await _db.ComputerMetrics
            .Where(m => m.ComputerId == id && m.CreatedAt >= startTime && m.CreatedAt <= endTime)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new { m.CreatedAt, m.CpuUsage, m.RamUsage })
            .ToListAsync();

        var diskMetrics = await _db.DiskMetrics
            .Include(m => m.ComputerDisk)
            .Where(m => m.ComputerDisk.ComputerId == id && m.CreatedAt >= startTime && m.CreatedAt <= endTime)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new {
                m.CreatedAt,
                m.UsedPercent,
                diskName = m.ComputerDisk.DiskName
            })
            .ToListAsync();

        var data = new { CpuRam = cpuRamMetrics, Disks = diskMetrics };
        return (false, null, data);
    }

    // 7. Tüm Cihazları Getir
    public async Task<object> GetAllComputersAsync(int userId, bool isAdmin)
    {
        var accCompIds = await _db.UserComputerAccesses.Where(x => x.UserId == userId).Select(x => x.ComputerId).ToListAsync();
        var accTagIds = await _db.UserTagAccesses.Where(x => x.UserId == userId).Select(x => x.TagId).ToListAsync();

        var query = _db.Computers.AsQueryable();
        bool isRestricted = !isAdmin || (accCompIds.Count > 0 || accTagIds.Count > 0);

        if (isRestricted)
        {
            query = query.Where(c =>
                accCompIds.Contains(c.Id) ||
                _db.ComputerTags.Any(ct => ct.ComputerId == c.Id && !ct.IsDeleted && accTagIds.Contains(ct.TagId))
            );
        }

        var computersData = await query.Select(c => new {
            Computer = c,
            ActiveTags = _db.ComputerTags
                            .Where(ct => ct.ComputerId == c.Id && !ct.IsDeleted && !ct.Tag.IsDeleted)
                            .Select(ct => ct.Tag.Name)
                            .ToList()
        }).ToListAsync();

        int offlineThreshold = _config.GetValue<int>("Alerting:OfflineThresholdSeconds", 150);

        var result = computersData.Select(x => new {
            id = x.Computer.Id,
            machineName = x.Computer.MachineName,
            displayName = x.Computer.DisplayName,
            ipAddress = x.Computer.IpAddress,
            lastSeen = x.Computer.LastSeen,
            tags = x.ActiveTags,
            isDeleted = x.Computer.IsDeleted,
            isActive = (DateTime.Now - x.Computer.LastSeen).TotalSeconds <= offlineThreshold
        })
        .OrderBy(c => c.isDeleted).ThenByDescending(c => c.isActive).ThenByDescending(c => c.lastSeen);

        return result;
    }

    // 8. Cihaz Silme
    public async Task<(bool isNotFound, bool isBadRequest, string message)> DeleteComputerAsync(int id)
    {
        var computer = await _db.Computers.FindAsync(id);
        if (computer == null)
            return (true, false, "Bilgisayar bulunamadı.");

        int offlineThreshold = _config.GetValue<int>("Alerting:OfflineThresholdSeconds", 150);
        bool isActive = (DateTime.Now - computer.LastSeen).TotalSeconds <= offlineThreshold;
        if (isActive)
        {
            return (false, true, "Aktif olan bir bilgisayarı silemezsiniz. Lütfen önce ajanı durdurun.");
        }

        computer.IsDeleted = true;
        await _db.SaveChangesAsync();

        return (false, false, "Bilgisayar sistemden başarıyla silinmiştir.");
    }

    // 9. Kullanıcının Etiketlerini Getir
    public async Task<object> GetMyTagsAsync(int userId, bool isAdmin)
    {
        var query = _db.Tags.AsQueryable();

        if (!isAdmin)
        {
            bool hasTagManagePerm = await _db.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .AnyAsync(rp => rp.Permission.Name == "Tag_Manage" || rp.Permission.Name == "Tag.Manage");

            if (!hasTagManagePerm)
            {
                var accessibleComputerIds = await _db.UserComputerAccesses
                    .Where(uca => uca.UserId == userId).Select(uca => uca.ComputerId).ToListAsync();

                var accessibleTagIds = await _db.UserTagAccesses
                    .Where(uta => uta.UserId == userId).Select(uta => uta.TagId).ToListAsync();

                query = query.Where(t =>
                    accessibleTagIds.Contains(t.Id) ||
                    t.Computers.Any(c => accessibleComputerIds.Contains(c.Id)));
            }
        }

        var tags = await query.Select(t => new { t.Id, t.Name }).ToListAsync();
        return tags;
    }

    public async Task<PerformanceReportDto> GetPerformanceReportAsync(int userId, bool isAdmin)
    {
        string cacheKey = $"PerformanceReport_User_{userId}";

        if (_cache.TryGetValue(cacheKey, out PerformanceReportDto? cachedReport) && cachedReport != null)
        {
            return cachedReport;
        }

        var query = _db.Computers.AsQueryable();

        if (!isAdmin)
        {
            var accCompIds = await _db.UserComputerAccesses.Where(x => x.UserId == userId).Select(x => x.ComputerId).ToListAsync();
            var accTagIds = await _db.UserTagAccesses.Where(x => x.UserId == userId).Select(x => x.TagId).ToListAsync();

            query = query.Where(c =>
                accCompIds.Contains(c.Id) ||
                _db.ComputerTags.Any(ct => ct.ComputerId == c.Id && !ct.IsDeleted && accTagIds.Contains(ct.TagId))
            );
        }

        var activeComputerIds = await query.Select(c => c.Id).ToListAsync();

        if (!activeComputerIds.Any()) return new PerformanceReportDto();

        var metricsSummary = await _db.ComputerMetrics
            .Where(m => activeComputerIds.Contains(m.ComputerId))
            .GroupBy(m => m.ComputerId)
            .Select(g => new
            {
                ComputerId = g.Key,
                AvgCpu = g.Average(m => m.CpuUsage),
                AvgRam = g.Average(m => m.RamUsage)
            })
            .ToListAsync();

        if (!metricsSummary.Any()) return new PerformanceReportDto();

        var diskMetricsSummary = await _db.DiskMetrics
            .Where(m => activeComputerIds.Contains(m.ComputerDisk.ComputerId))
            .GroupBy(m => new { m.ComputerDisk.ComputerId, m.ComputerDisk.DiskName })
            .Select(g => new
            {
                ComputerId = g.Key.ComputerId,
                DiskName = g.Key.DiskName,
                AvgUsed = g.Average(m => m.UsedPercent)
            })
            .ToListAsync();

        var globalDiskStats = diskMetricsSummary
            .GroupBy(d => d.DiskName)
            .ToDictionary(g => g.Key, g => new
            {
                Count = g.Count(),
                GlobalAvg = g.Average(x => x.AvgUsed)
            });

        var computers = await _db.Computers
            .Where(c => activeComputerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.DisplayName, c.MachineName })
            .ToListAsync();

        var deviceAverages = metricsSummary.Select(m => new
        {
            ComputerId = m.ComputerId,
            ComputerName = computers.FirstOrDefault(c => c.Id == m.ComputerId)?.DisplayName
                    ?? computers.FirstOrDefault(c => c.Id == m.ComputerId)?.MachineName
                    ?? "Bilinmeyen Cihaz",
            AvgCpu = m.AvgCpu,
            AvgRam = m.AvgRam,

            Disks = diskMetricsSummary
            .Where(d => d.ComputerId == m.ComputerId)
            .Select(d =>
            {
                var stats = globalDiskStats[d.DiskName];
                string status = "Nötr";

                if (stats.Count > 1)
                {
                    status = d.AvgUsed > stats.GlobalAvg ? "Kötü" : "İyi";
                }

                return new DiskPerformanceDto
                {
                    DiskName = d.DiskName,
                    AverageUsedPercent = Math.Round(d.AvgUsed, 2),
                    DiskStatus = status
                };
            }).OrderBy(d => d.DiskName).ToList()
        }).ToList();

        double globalAvgCpu = deviceAverages.Average(d => d.AvgCpu);
        double globalAvgRam = deviceAverages.Average(d => d.AvgRam);

        var report = new PerformanceReportDto
        {
            GlobalAverageCpu = Math.Round(globalAvgCpu, 2),
            GlobalAverageRam = Math.Round(globalAvgRam, 2),

            GlobalDiskAverages = globalDiskStats.Select(g => new GlobalDiskAverageDto
            {
                DiskName = g.Key,
                AverageUsedPercent = Math.Round(g.Value.GlobalAvg, 2)
            }).OrderBy(x => x.DiskName).ToList(),

            Devices = deviceAverages.Select(d => new DevicePerformanceDto
            {
                ComputerId = d.ComputerId,
                ComputerName = d.ComputerName,
                AverageCpu = Math.Round(d.AvgCpu, 2),
                AverageRam = Math.Round(d.AvgRam, 2),
                CpuStatus = d.AvgCpu <= globalAvgCpu ? "İyi" : "Kötü",
                RamStatus = d.AvgRam <= globalAvgRam ? "İyi" : "Kötü",
                Disks = d.Disks
            })
            .OrderByDescending(d => d.AverageCpu)
            .ToList()
        };

        _cache.Set(cacheKey, report, TimeSpan.FromSeconds(30));

        return report;
    }

    public async Task<MetricSummaryDto> GetMetricsSummaryAsync(int computerId, string metricType, string? diskName)
    {
        var summary = new MetricSummaryDto();

        if (metricType == "CPU" || metricType == "RAM")
        {
            var query = _db.ComputerMetrics.Where(m => m.ComputerId == computerId);
            summary.TotalCount = await query.CountAsync();

            if (summary.TotalCount > 0)
            {
                if (metricType == "CPU")
                {
                    summary.MaxVal = await query.MaxAsync(m => m.CpuUsage);
                    summary.MinVal = await query.MinAsync(m => m.CpuUsage);
                    summary.MaxCount = await query.CountAsync(m => m.CpuUsage == summary.MaxVal);
                    summary.MinCount = await query.CountAsync(m => m.CpuUsage == summary.MinVal);
                }
                else
                {
                    summary.MaxVal = await query.MaxAsync(m => m.RamUsage);
                    summary.MinVal = await query.MinAsync(m => m.RamUsage);
                    summary.MaxCount = await query.CountAsync(m => m.RamUsage == summary.MaxVal);
                    summary.MinCount = await query.CountAsync(m => m.RamUsage == summary.MinVal);
                }
            }
        }
        else if (metricType.StartsWith("Disk") && !string.IsNullOrEmpty(diskName))
        {
            var query = _db.DiskMetrics
                .Where(m => m.ComputerDisk.ComputerId == computerId && m.ComputerDisk.DiskName == diskName);

            summary.TotalCount = await query.CountAsync();

            if (summary.TotalCount > 0)
            {
                summary.MaxVal = await query.MaxAsync(m => m.UsedPercent);
                summary.MinVal = await query.MinAsync(m => m.UsedPercent);
                summary.MaxCount = await query.CountAsync(m => m.UsedPercent == summary.MaxVal);
                summary.MinCount = await query.CountAsync(m => m.UsedPercent == summary.MinVal);
            }
        }

        return summary;
    }
}