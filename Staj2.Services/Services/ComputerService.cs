using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;
using Staj2.Services.Models;

namespace Staj2.Services.Services;

// YENİ: BaseService'den miras alıyoruz
public class ComputerService : BaseService, IComputerService
{
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;

    // YENİ: AppDbContext db'yi base sınıfa (BaseService) gönderiyoruz
    public ComputerService(AppDbContext db, IConfiguration config, IMemoryCache cache) : base(db)
    {
        _config = config;
        _cache = cache;
    }

    // --- YARDIMCI METOT (Okuma İşlemi) ---
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

    // 1. Cihaz Detayı (Okuma İşlemi)
    public async Task<ServiceResult<object>> GetComputerAsync(int id, int userId, bool isAdmin)
    {
        if (!await CheckComputerAccessAsync(id, userId, isAdmin))
            return ServiceResult<object>.Failure("Bu cihaza erişim yetkiniz bulunmamaktadır.");

        var computer = await _db.Computers.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (computer == null)
            return ServiceResult<object>.Failure("Bilgisayar bulunamadı.");

        var data = new { computer.Id, computer.CpuThreshold, computer.RamThreshold, Tags = computer.Tags.Select(t => t.Name) };
        return ServiceResult<object>.Success(data);
    }

    // 2. Disk Listesi (Okuma İşlemi)
    public async Task<ServiceResult<object>> GetComputerDisksAsync(int computerId, int userId, bool isAdmin)
    {
        if (!await CheckComputerAccessAsync(computerId, userId, isAdmin))
            return ServiceResult<object>.Failure("Bu cihaza erişim yetkiniz bulunmamaktadır.");

        var disks = await _db.ComputerDisks
        .Where(d => d.ComputerId == computerId)
        .Select(d => new { d.Id, d.DiskName, thresholdPercent = d.ThresholdPercent })
        .ToListAsync();
        return ServiceResult<object>.Success(disks);
    }

    // 3. Eşik Değerlerini Güncelle (YAZMA İŞLEMİ - SARMALANDI)
    // 3. Eşik Değerlerini Güncelle (YAZMA İŞLEMİ - SARMALANDI)
    public Task<ServiceResult> UpdateThresholdsAsync(int computerId, UpdateThresholdsRequest request, int userId, bool isAdmin)
    {
        return ExecuteWithDbHandlingAsync(async () =>
        {
            if (!await CheckComputerAccessAsync(computerId, userId, isAdmin))
                return ServiceResult.Failure("Bu cihaza erişim yetkiniz bulunmamaktadır.");

            if (request.CpuThreshold.HasValue && (request.CpuThreshold < 0 || request.CpuThreshold > 100))
                return ServiceResult.Failure("CPU eşik değeri 0 ile 100 arasında olmalıdır.");

            if (request.RamThreshold.HasValue && (request.RamThreshold < 0 || request.RamThreshold > 100))
                return ServiceResult.Failure("RAM eşik değeri 0 ile 100 arasında olmalıdır.");

            if (request.DiskThresholds != null)
            {
                foreach (var disk in request.DiskThresholds)
                {
                    if (disk.ThresholdPercent.HasValue && (disk.ThresholdPercent < 0 || disk.ThresholdPercent > 100))
                        return ServiceResult.Failure($"'{disk.DiskName}' diski için eşik değeri 0-100 arasında olmalıdır.");
                }
            }

            var computer = await _db.Computers.Include(c => c.Disks).FirstOrDefaultAsync(c => c.Id == computerId);
            if (computer == null)
                return ServiceResult.Failure("Bilgisayar bulunamadı.");


            // Ana tabloyu güncelle (Yaklaşım 2: Güncel değeri ana tabloda tutuyoruz)
            computer.CpuThreshold = request.CpuThreshold;
            computer.RamThreshold = request.RamThreshold;

            if (request.DiskThresholds != null)
            {
                foreach (var dReq in request.DiskThresholds)
                {
                    var disk = computer.Disks.FirstOrDefault(d => d.DiskName == dReq.DiskName);
                    if (disk != null && disk.ThresholdPercent != dReq.ThresholdPercent)
                    {
                        disk.ThresholdPercent = dReq.ThresholdPercent;
                    }
                }
            }
            await _db.SaveChangesAsync();

            return ServiceResult.Success("Sınırlar başarıyla kaydedildi.");
        }, "Cihaz Eşik Değerleri", DbOperation.Update);
    }

    // 4. Etiket Atama (YAZMA İŞLEMİ - SARMALANDI)
    public Task<ServiceResult> UpdateComputerTagsAsync(int id, UpdateComputerTagsRequest request)
    {
        return ExecuteWithDbHandlingAsync(async () =>
        {
            var computer = await _db.Computers.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
            if (computer == null)
                return ServiceResult.Failure("Bilgisayar bulunamadı.");

            var newTags = await _db.Tags.Where(t => request.Tags.Contains(t.Name)).ToListAsync();
            computer.Tags.Clear();
            foreach (var tag in newTags) computer.Tags.Add(tag);

            await _db.SaveChangesAsync();

            return ServiceResult.Success("Etiketler cihaza başarıyla atandı.");
        }, "Cihaz Etiketleri", DbOperation.Update);
    }

    // 5. İsim Değiştirme (YAZMA İŞLEMİ - SARMALANDI)
    public Task<ServiceResult> UpdateDisplayNameAsync(UpdateComputerNameRequest request)
    {
        return ExecuteWithDbHandlingAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(request.NewDisplayName))
                return ServiceResult.Failure("İsim alanı boş bırakılamaz.");

            if (request.NewDisplayName.Length > 200)
                return ServiceResult.Failure("Görünen isim 200 karakterden uzun olamaz.");

            bool isNameTaken = await _db.Computers
                .AnyAsync(c => c.DisplayName == request.NewDisplayName && c.Id != request.Id);

            if (isNameTaken)
                return ServiceResult.Failure("Bu isim zaten başka bir cihaza ait. Lütfen farklı bir isim giriniz.");

            var computer = await _db.Computers.FindAsync(request.Id);
            if (computer == null)
                return ServiceResult.Failure("Bilgisayar bulunamadı.");

            computer.DisplayName = request.NewDisplayName;
            await _db.SaveChangesAsync();

            return ServiceResult.Success("Cihaz ismi başarıyla güncellendi.");
        }, "Cihaz Görünen İsmi", DbOperation.Update);
    }

    // 6. Belirli bir tarih aralığındaki metrik geçmişini getir (Okuma İşlemi)
    public async Task<ServiceResult<object>> GetMetricsHistoryAsync(int id, string start, string end)
    {
        if (id <= 0)
            return ServiceResult<object>.Failure("Lütfen analiz yapmak için sol menüden bir cihaz seçiniz.");

        if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end))
            return ServiceResult<object>.Failure("Lütfen tarih aralığı seçiniz.");

        if (!DateTime.TryParse(start, out DateTime startTime) || !DateTime.TryParse(end, out DateTime endTime))
            return ServiceResult<object>.Failure("Geçersiz tarih formatı.");

        if (startTime > endTime)
            return ServiceResult<object>.Failure("Başlangıç tarihi bitiş tarihinden sonra olamaz.");

        var totalMinutes = (endTime - startTime).TotalMinutes;
        int bucketSizeInMinutes = totalMinutes <= 150 ? 0 : (int)Math.Ceiling(totalMinutes / 200.0);

        // 1. ADIM: Verileri veritabanından en küçük haliyle (sadece tarih, değerler) RAM'e al
        var rawCpuRam = await _db.ComputerMetrics
            .Where(m => m.ComputerId == id && m.CreatedAt >= startTime && m.CreatedAt <= endTime)
            .Select(m => new { m.CreatedAt, m.CpuUsage, m.RamUsage })
            .ToListAsync();

        var rawDisks = await _db.DiskMetrics
            .Where(m => m.ComputerDisk.ComputerId == id && m.CreatedAt >= startTime && m.CreatedAt <= endTime)
            .Select(m => new { m.CreatedAt, m.UsedPercent, diskName = m.ComputerDisk.DiskName })
            .ToListAsync();

        // 2. ADIM: Gruplamaları C# üzerinde (Memory) yap
        if (bucketSizeInMinutes == 0)
        {
            var data = new
            {
                CpuRam = rawCpuRam.OrderByDescending(m => m.CreatedAt),
                Disks = rawDisks.OrderByDescending(m => m.CreatedAt)
            };
            return ServiceResult<object>.Success(data);
        }
        else
        {
            long bucketTicks = TimeSpan.FromMinutes(bucketSizeInMinutes).Ticks;

            var groupedCpuRam = rawCpuRam
                .GroupBy(m => m.CreatedAt.Ticks / bucketTicks)
                .Select(g => new {
                    CreatedAt = new DateTime(g.Key * bucketTicks),
                    CpuUsage = Math.Round(g.Average(m => m.CpuUsage), 2),
                    RamUsage = Math.Round(g.Average(m => m.RamUsage), 2)
                })
                .OrderByDescending(m => m.CreatedAt)
                .ToList();

            var groupedDisks = rawDisks
                .GroupBy(m => new { m.diskName, Bucket = m.CreatedAt.Ticks / bucketTicks })
                .Select(g => new {
                    CreatedAt = new DateTime(g.Key.Bucket * bucketTicks),
                    UsedPercent = Math.Round(g.Average(m => m.UsedPercent), 2),
                    diskName = g.Key.diskName
                })
                .OrderByDescending(m => m.CreatedAt)
                .ToList();

            var data = new { CpuRam = groupedCpuRam, Disks = groupedDisks };
            return ServiceResult<object>.Success(data);
        }
    }

    // 7. Tüm Cihazları Getir (Okuma İşlemi)
    public async Task<ServiceResult<object>> GetAllComputersAsync(int userId, bool isAdmin)
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
        .OrderBy(c => c.isDeleted).ThenByDescending(c => c.isActive).ThenByDescending(c => c.lastSeen)
        .ToList();

        return ServiceResult<object>.Success(result);
    }

    // 8. Cihaz Silme (YAZMA İŞLEMİ - SARMALANDI)
    public Task<ServiceResult> DeleteComputerAsync(int id)
    {
        return ExecuteWithDbHandlingAsync(async () =>
        {
            var computer = await _db.Computers.FindAsync(id);
            if (computer == null)
                return ServiceResult.Failure("Bilgisayar bulunamadı.");

            int offlineThreshold = _config.GetValue<int>("Alerting:OfflineThresholdSeconds", 150);
            bool isActive = (DateTime.Now - computer.LastSeen).TotalSeconds <= offlineThreshold;
            if (isActive)
            {
                return ServiceResult.Failure("Aktif olan bir bilgisayarı silemezsiniz. Lütfen önce ajanı durdurun.");
            }

            computer.IsDeleted = true;
            await _db.SaveChangesAsync();

            return ServiceResult.Success("Bilgisayar sistemden başarıyla silinmiştir.");
        }, "Cihaz",DbOperation.Delete);
    }

    // 9. Kullanıcının Etiketlerini Getir (Okuma İşlemi)
    public async Task<ServiceResult<object>> GetMyTagsAsync(int userId, bool isAdmin)
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
        return ServiceResult<object>.Success(tags);
    }

    // 10. Performans Raporu (Okuma İşlemi)
    public async Task<ServiceResult<PerformanceReportDto>> GetPerformanceReportAsync(int userId, bool isAdmin)
    {
        string cacheKey = $"PerformanceReport_User_{userId}";

        if (_cache.TryGetValue(cacheKey, out PerformanceReportDto? cachedReport) && cachedReport != null)
        {
            return ServiceResult<PerformanceReportDto>.Success(cachedReport);
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

        if (!activeComputerIds.Any())
            return ServiceResult<PerformanceReportDto>.Success(new PerformanceReportDto());

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

        if (!metricsSummary.Any())
            return ServiceResult<PerformanceReportDto>.Success(new PerformanceReportDto());

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

        return ServiceResult<PerformanceReportDto>.Success(report);
    }

    // 11. Metrik Özeti (Okuma İşlemi)
    public async Task<ServiceResult<MetricSummaryDto>> GetMetricsSummaryAsync(int computerId, string metricType, string? diskName)
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

        return ServiceResult<MetricSummaryDto>.Success(summary);
    }
    // 12. Rapor Detayları İçin Son 5 Veri Gününün Trendi (Yeni Eklendi)
    public async Task<ServiceResult<object>> GetMetricsTrendDataAsync(int computerId, string metricType, string? diskName)
    {
        if (metricType == "CPU" || metricType == "RAM")
        {
            var dates = await _db.ComputerMetrics
                .Where(m => m.ComputerId == computerId)
                .Select(m => m.CreatedAt.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .Take(5)
                .ToListAsync();

            if (!dates.Any()) return ServiceResult<object>.Success(new List<object>());

            var minDate = dates.Min();
            var maxDate = dates.Max().AddDays(1);

            var totalMinutes = (maxDate - minDate).TotalMinutes;
            int bucketSizeInMinutes = totalMinutes <= 150 ? 0 : (int)Math.Ceiling(totalMinutes / 300.0);

            // 1. ADIM: Veritabanından en yalın haliyle RAM'e çek (Client-Side Evaluation için)
            var rawData = await _db.ComputerMetrics
                .Where(m => m.ComputerId == computerId && m.CreatedAt >= minDate && m.CreatedAt < maxDate)
                .Select(m => new { m.CreatedAt, value = metricType == "CPU" ? m.CpuUsage : m.RamUsage })
                .ToListAsync(); // Veritabanı sorgusu burada biter

            // 2. ADIM: Gruplamayı bellekte yap
            if (bucketSizeInMinutes == 0)
            {
                var data = rawData.OrderBy(m => m.CreatedAt).Select(m => new { createdAt = m.CreatedAt, value = m.value }).ToList();
                return ServiceResult<object>.Success(data);
            }
            else
            {
                long bucketTicks = TimeSpan.FromMinutes(bucketSizeInMinutes).Ticks;

                var groupedData = rawData
                    .GroupBy(m => m.CreatedAt.Ticks / bucketTicks)
                    .Select(g => new {
                        createdAt = new DateTime(g.Key * bucketTicks),
                        value = Math.Round(g.Average(m => m.value), 2)
                    })
                    .OrderBy(m => m.createdAt)
                    .ToList();

                return ServiceResult<object>.Success(groupedData);
            }
        }
        else if (metricType.StartsWith("Disk") && !string.IsNullOrEmpty(diskName))
        {
            var dates = await _db.DiskMetrics
                .Where(m => m.ComputerDisk.ComputerId == computerId && m.ComputerDisk.DiskName == diskName)
                .Select(m => m.CreatedAt.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .Take(5)
                .ToListAsync();

            if (!dates.Any()) return ServiceResult<object>.Success(new List<object>());

            var minDate = dates.Min();
            var maxDate = dates.Max().AddDays(1);

            var totalMinutes = (maxDate - minDate).TotalMinutes;
            int bucketSizeInMinutes = totalMinutes <= 150 ? 0 : (int)Math.Ceiling(totalMinutes / 300.0);

            // 1. ADIM: Diski veritabanından yalın çek
            var rawData = await _db.DiskMetrics
                .Where(m => m.ComputerDisk.ComputerId == computerId && m.ComputerDisk.DiskName == diskName && m.CreatedAt >= minDate && m.CreatedAt < maxDate)
                .Select(m => new { m.CreatedAt, value = m.UsedPercent })
                .ToListAsync(); // Veritabanı işlemi bitti

            // 2. ADIM: Bellekte grupla
            if (bucketSizeInMinutes == 0)
            {
                var data = rawData.OrderBy(m => m.CreatedAt).Select(m => new { createdAt = m.CreatedAt, value = m.value }).ToList();
                return ServiceResult<object>.Success(data);
            }
            else
            {
                long bucketTicks = TimeSpan.FromMinutes(bucketSizeInMinutes).Ticks;

                var groupedData = rawData
                    .GroupBy(m => m.CreatedAt.Ticks / bucketTicks)
                    .Select(g => new {
                        createdAt = new DateTime(g.Key * bucketTicks),
                        value = Math.Round(g.Average(m => m.value), 2)
                    })
                    .OrderBy(m => m.createdAt)
                    .ToList();

                return ServiceResult<object>.Success(groupedData);
            }
        }

        return ServiceResult<object>.Success(new List<object>());
    }

    public async Task<ServiceResult<ThresholdAnalysisReportDto>> GetThresholdAnalysisAsync(int computerId, ThresholdReportRequestDto request)
    {
        var computer = await _db.Computers.Include(c => c.Disks).FirstOrDefaultAsync(c => c.Id == computerId);
        if (computer == null) return ServiceResult<ThresholdAnalysisReportDto>.Failure("Cihaz bulunamadı.");

        var startDate = request.StartDate;
        var endDate = request.EndDate;

        if ((endDate - startDate).TotalDays > 31)
        {
            return ServiceResult<ThresholdAnalysisReportDto>.Failure("Sistem performansı için lütfen maksimum 31 günlük bir analiz aralığı seçiniz.");
        }

        // --- YEREL YARDIMCI FONKSİYON: 100 Noktaya İndirme (Decimation) ---
        // Aşımları zamana göre gruplayıp her gruptaki EN YÜKSEK (Peak) aşımı seçer.
        List<ThresholdBreachDetailDto> DecimateBreaches(List<ThresholdBreachDetailDto> rawBreaches, int targetCount)
        {
            if (rawBreaches.Count <= targetCount) return rawBreaches;

            long totalTicks = (endDate - startDate).Ticks;
            long bucketTicks = totalTicks / targetCount;
            if (bucketTicks <= 0) return rawBreaches;

            return rawBreaches
                .GroupBy(b => (b.Timestamp.Ticks - startDate.Ticks) / bucketTicks)
                .Select(g => g.OrderByDescending(x => x.Value).First()) // O aralıktaki en şiddetli aşımı seç
                .OrderBy(x => x.Timestamp)
                .ToList();
        }

        // 1. TOPLAM VERİ SAYISINI ÇEK
        var totalCpuRamCount = await _db.ComputerMetrics
            .Where(m => m.ComputerId == computerId && m.CreatedAt >= startDate && m.CreatedAt <= endDate)
            .CountAsync();

        // 2. UYARI DETAYLARINI ÇEK (CPU & RAM)
        var cpuBreachesRaw = await _db.MetricWarningLogs
            .Where(w => w.ComputerId == computerId && w.MetricTypeId == 1 && w.CreatedAt >= startDate && w.CreatedAt <= endDate)
            .OrderBy(w => w.CreatedAt)
            .Select(w => new ThresholdBreachDetailDto { Timestamp = w.CreatedAt, Value = w.MetricValue, ThresholdPercent = w.ThresholdValue })
            .ToListAsync();

        var ramBreachesRaw = await _db.MetricWarningLogs
            .Where(w => w.ComputerId == computerId && w.MetricTypeId == 2 && w.CreatedAt >= startDate && w.CreatedAt <= endDate)
            .OrderBy(w => w.CreatedAt)
            .Select(w => new ThresholdBreachDetailDto { Timestamp = w.CreatedAt, Value = w.MetricValue, ThresholdPercent = w.ThresholdValue })
            .ToListAsync();

        // MAKSİMUM 100 NOKTA KURALINI UYGULA
        var cpuBreaches = DecimateBreaches(cpuBreachesRaw, 70);
        var ramBreaches = DecimateBreaches(ramBreachesRaw, 70);

        var report = new ThresholdAnalysisReportDto
        {
            ComputerId = computer.Id,
            ComputerName = computer.DisplayName ?? computer.MachineName,
            TotalActiveCount = totalCpuRamCount,
            CpuResult = new MetricThresholdResult
            {
                TotalCount = totalCpuRamCount,
                WarningCount = cpuBreachesRaw.Count, // Toplam sayı gerçek veri üzerinden kalsın
                BelowThresholdCount = Math.Max(0, totalCpuRamCount - cpuBreachesRaw.Count),
                Breaches = cpuBreaches // Grafik için seyreltilmiş liste
            },
            RamResult = new MetricThresholdResult
            {
                TotalCount = totalCpuRamCount,
                WarningCount = ramBreachesRaw.Count,
                BelowThresholdCount = Math.Max(0, totalCpuRamCount - ramBreachesRaw.Count),
                Breaches = ramBreaches
            }
        };

        // 3. DİSKLER İÇİN AYNI İŞLEM
        var diskIds = computer.Disks.Select(d => d.Id).ToList();
        if (diskIds.Any())
        {
            var diskTotalCounts = await _db.DiskMetrics
                .Where(m => diskIds.Contains(m.ComputerDiskId) && m.CreatedAt >= startDate && m.CreatedAt <= endDate)
                .GroupBy(m => m.ComputerDiskId)
                .Select(g => new { DiskId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DiskId, x => x.Count);

            var allDiskBreaches = await _db.MetricWarningLogs
                .Where(w => w.ComputerId == computerId && w.MetricTypeId == 3 && w.ComputerDiskId != null && w.CreatedAt >= startDate && w.CreatedAt <= endDate)
                .OrderBy(w => w.CreatedAt)
                .Select(w => new {
                    DiskId = w.ComputerDiskId!.Value,
                    Breach = new ThresholdBreachDetailDto { Timestamp = w.CreatedAt, Value = w.MetricValue, ThresholdPercent = w.ThresholdValue }
                })
                .ToListAsync();

            var diskBreachesLookup = allDiskBreaches.ToLookup(x => x.DiskId, x => x.Breach);

            foreach (var disk in computer.Disks)
            {
                int totalDiskCount = diskTotalCounts.ContainsKey(disk.Id) ? diskTotalCounts[disk.Id] : 0;
                var rawBreachesForThisDisk = diskBreachesLookup[disk.Id].ToList();

                // DİSK İÇİN 100 NOKTA KURALI
                var decimatedDiskBreaches = DecimateBreaches(rawBreachesForThisDisk, 70);

                report.DiskResults.Add(new DiskThresholdResult
                {
                    DiskName = disk.DiskName,
                    TotalCount = totalDiskCount,
                    WarningCount = rawBreachesForThisDisk.Count,
                    BelowThresholdCount = Math.Max(0, totalDiskCount - rawBreachesForThisDisk.Count),
                    Breaches = decimatedDiskBreaches
                });
            }
        }

        return ServiceResult<ThresholdAnalysisReportDto>.Success(report);
    }
}