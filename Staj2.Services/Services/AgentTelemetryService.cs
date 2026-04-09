using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;
using Staj2.Services.Models;
using Staj2.Services.Models.Agent;
using System.Globalization;

namespace Staj2.Services.Services;

// YENİ: BaseService'den miras alıyoruz.
public class AgentTelemetryService : BaseService, IAgentTelemetryService
{
    private readonly IConfiguration _config;

    // Anlık metrikleri RAM'de tutan önbellek
    private static readonly Dictionary<string, AgentTelemetryDto> _latestData = new();

    // YENİ: AppDbContext db'yi base sınıfa gönderiyoruz.
    public AgentTelemetryService(AppDbContext db, IConfiguration config) : base(db)
    {
        _config = config;
    }

    // YAZMA/GÜNCELLEME İŞLEMİ - Sarmalandı (CancellationToken closure içinde sorunsuz çalışır)
    public Task<ServiceResult<List<(string Email, string Subject, string Body)>>> IngestAsync(AgentTelemetryDto dto, string? agentKey, CancellationToken ct)
    {
        // Geri dönüş tipi karmaşık bir Tuple listesi olduğu için Generic sarmalayıcıyı buna göre ayarladık
        return ExecuteWithDbHandlingAsync<List<(string Email, string Subject, string Body)>>(async () =>
        {
            // 1. Güvenlik Kontrolü
            var expectedKey = _config["Agent:IngestKey"];
            if (!string.IsNullOrWhiteSpace(expectedKey) && agentKey != expectedKey)
            {
                // Controller'da yakalayıp 401 dönebilmek için spesifik mesaj atıyoruz
                return ServiceResult<List<(string Email, string Subject, string Body)>>.Failure("Unauthorized");
            }

            if (string.IsNullOrWhiteSpace(dto.MacAddress))
                return ServiceResult<List<(string Email, string Subject, string Body)>>.Failure("MacAddress is required.");

            // 2. Bilgisayar Kaydı veya Güncelleme
            var computer = await _db.Computers
                .Include(c => c.Disks)
                .Include(c => c.Tags) // Tags sonradan DTO'ya basmak için gerekli
                .FirstOrDefaultAsync(c => c.MacAddress == dto.MacAddress, ct);

            if (computer == null)
            {
                computer = new Computer
                {
                    MacAddress = dto.MacAddress,
                    MachineName = dto.MachineName,
                    DisplayName = dto.MachineName,
                    IpAddress = dto.Ip,
                    CpuModel = dto.CpuModel,
                    TotalRamMb = dto.TotalRamMb,
                    LastSeen = DateTime.Now,
                    CreatedAt = DateTime.Now
                };
                _db.Computers.Add(computer);
            }
            else
            {
                computer.LastSeen = DateTime.Now;
                computer.MachineName = dto.MachineName;
                computer.IpAddress = dto.Ip;
                computer.CpuModel = dto.CpuModel;
                if (Math.Abs(computer.TotalRamMb - dto.TotalRamMb) > 1)
                    computer.TotalRamMb = dto.TotalRamMb;
                if (computer.IsOfflineAlertSent)
                {
                    computer.IsOfflineAlertSent = false;
                }
            }

            // Transaction aktif olduğu için bu save işlemi aslında henüz kalıcı olarak DB'ye Commitlenmiyor
            await _db.SaveChangesAsync(ct);

            // 3. Yeni Disk Mantığı
            var diskTotalParts = dto.TotalDiskGb.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < diskTotalParts.Length; i += 2)
            {
                if (i + 1 < diskTotalParts.Length)
                {
                    string dName = diskTotalParts[i].Replace(":", "");
                    double.TryParse(diskTotalParts[i + 1].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double dSize);

                    var existingDisk = computer.Disks.FirstOrDefault(d => d.DiskName == dName);
                    if (existingDisk == null)
                    {
                        _db.ComputerDisks.Add(new ComputerDisk
                        {
                            ComputerId = computer.Id,
                            DiskName = dName,
                            TotalSizeGb = dSize,
                            ThresholdPercent = null
                        });
                    }
                    else if (Math.Abs(existingDisk.TotalSizeGb - dSize) > 0.1)
                    {
                        existingDisk.TotalSizeGb = dSize;
                    }
                }
            }
            await _db.SaveChangesAsync(ct);

            dto.DisplayName = computer.DisplayName;
            dto.ComputerId = computer.Id;
            dto.Tags = computer.Tags?.Select(t => t.Name).ToList() ?? new List<string>();
            dto.CpuThreshold = computer.CpuThreshold;
            dto.RamThreshold = computer.RamThreshold;

            // 4. Metrik Kaydı
            var metric = new ComputerMetric
            {
                ComputerId = computer.Id,
                CpuUsage = dto.CpuUsage,
                RamUsage = dto.RamUsage,
                CreatedAt = DateTime.Now
            };
            _db.ComputerMetrics.Add(metric);

            var currentDisks = await _db.ComputerDisks.Where(d => d.ComputerId == computer.Id).ToListAsync(ct);
            var diskUsageParts = dto.DiskUsage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < diskUsageParts.Length; i += 2)
            {
                if (i + 1 < diskUsageParts.Length)
                {
                    string dName = diskUsageParts[i].Replace(":", "");
                    string usageStr = diskUsageParts[i + 1].Replace("%", "").Replace(",", ".");
                    if (double.TryParse(usageStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double dUsage))
                    {
                        var diskEntity = currentDisks.FirstOrDefault(d => d.DiskName == dName);
                        if (diskEntity != null)
                        {
                            _db.DiskMetrics.Add(new DiskMetric
                            {
                                ComputerDiskId = diskEntity.Id,
                                UsedPercent = dUsage,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                }
            }
            await _db.SaveChangesAsync(ct);

            // =======================================================
            // 5. ALERT (UYARI) KONTROLLERİ VE MAİL İÇERİĞİ HAZIRLAMA
            // =======================================================
            var alertsToSend = new List<(string Email, string Subject, string Body)>();
            var alertingConfig = _config.GetSection("Alerting");

            // 1. Config'den Yönetici rol adını al, yoksa "Yönetici" kullan
            var adminRoleName = _config["AppDefaults:AdminRoleName"] ?? "Yönetici";

            // 2. Sistemdeki "Yönetici" rolüne sahip aktif kullanıcılar
            var adminEmails = await _db.Users
                .Where(u => !u.IsDeleted && u.Roles.Any(r => r.Name == adminRoleName && !r.IsDeleted))
                .Select(u => u.Email)
                .ToListAsync(ct);

            // 3. Bu cihaza doğrudan atanmış aktif kullanıcılar (UserComputerAccess tablosu üzerinden)
            var assignedUserEmails = await _db.UserComputerAccesses
                .Where(uca => uca.ComputerId == computer.Id && !uca.IsDeleted && !uca.User.IsDeleted)
                .Select(uca => uca.User.Email)
                .ToListAsync(ct);

            // Yönetici ve Atalı Kullanıcıları birleştir, tekrarlayan mailleri temizle (Distinct)
            var finalRecipients = adminEmails
                .Concat(assignedUserEmails)
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct()
                .ToList();

            if (finalRecipients.Count > 0)
            {
                int intervalHours = alertingConfig.GetValue<int>("NotifyIntervalHours", 1);
                string deviceName = !string.IsNullOrWhiteSpace(computer.DisplayName) ? computer.DisplayName : computer.MachineName;
                bool dbUpdateNeeded = false;

                void CreateAlert(string title, string message)
                {
                    string subject = $"🚨 {title}: {deviceName}";
                    string fullBody = $"Merhaba,\n\n{deviceName} ({computer.IpAddress}) cihazında aşağıdaki limit aşımı tespit edilmiştir:\n\n{message}\n\n--------------------------------------------------\nZaman: {DateTime.Now}";

                    foreach (var email in finalRecipients)
                        alertsToSend.Add((email, subject, fullBody));
                }

                // CPU Kontrolü
                if (computer.CpuThreshold.HasValue && dto.CpuUsage >= computer.CpuThreshold.Value)
                {
                    if (computer.CpuLastNotifyTime == null || (DateTime.Now - computer.CpuLastNotifyTime.Value).TotalHours >= intervalHours)
                    {
                        CreateAlert("CPU UYARISI", $"⚠️ CPU KULLANIMI: %{dto.CpuUsage:F1} (Limit: %{computer.CpuThreshold.Value})");
                        computer.CpuLastNotifyTime = DateTime.Now;
                        dbUpdateNeeded = true;
                    }
                }

                // RAM Kontrolü
                if (computer.RamThreshold.HasValue && dto.RamUsage >= computer.RamThreshold.Value)
                {
                    if (computer.RamLastNotifyTime == null || (DateTime.Now - computer.RamLastNotifyTime.Value).TotalHours >= intervalHours)
                    {
                        CreateAlert("RAM UYARISI", $"⚠️ RAM KULLANIMI: %{dto.RamUsage:F1} (Limit: %{computer.RamThreshold.Value})");
                        computer.RamLastNotifyTime = DateTime.Now;
                        dbUpdateNeeded = true;
                    }
                }

                // DİSK Kontrolü
                for (int i = 0; i < diskUsageParts.Length; i += 2)
                {
                    if (i + 1 < diskUsageParts.Length)
                    {
                        string diskName = diskUsageParts[i].Replace(":", "").Trim();
                        string percentStr = diskUsageParts[i + 1].Replace("%", "").Replace(",", ".");

                        if (double.TryParse(percentStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double currentUsage))
                        {
                            var targetDisk = computer.Disks.FirstOrDefault(d => d.DiskName == diskName);
                            if (targetDisk != null && targetDisk.ThresholdPercent.HasValue && currentUsage >= targetDisk.ThresholdPercent.Value)
                            {
                                if (targetDisk.LastNotifyTime == null || (DateTime.Now - targetDisk.LastNotifyTime.Value).TotalHours >= intervalHours)
                                {
                                    CreateAlert($"DİSK UYARISI ({diskName})", $"⚠️ DİSK DOLULUK ({diskName}): %{currentUsage:F1} (Limit: %{targetDisk.ThresholdPercent.Value})");
                                    targetDisk.LastNotifyTime = DateTime.Now;
                                    dbUpdateNeeded = true;
                                }
                            }
                        }
                    }
                }

                if (dbUpdateNeeded) await _db.SaveChangesAsync(ct);
            }

            dto.Ts = DateTime.Now;
            lock (_latestData) { _latestData[dto.MacAddress] = dto; }

            // Başarılı durumunda uyarı listesini Data olarak dönüyoruz
            return ServiceResult<List<(string Email, string Subject, string Body)>>.Success(alertsToSend);

        }, "Cihaz Telemetri Verisi");
    }

    // SADECE OKUMA İŞLEMİ - Sarmalanmadı
    public async Task<ServiceResult<object>> GetLatestAsync(int userId, bool isAdmin)
    {
        List<AgentTelemetryDto> list;
        lock (_latestData) { list = _latestData.Values.OrderByDescending(x => x.Ts).ToList(); }

        var macs = list.Select(x => x.MacAddress).ToList();

        var accessibleCompIds = await _db.UserComputerAccesses.Where(x => x.UserId == userId).Select(x => x.ComputerId).ToListAsync();
        var accessibleTagIds = await _db.UserTagAccesses.Where(x => x.UserId == userId).Select(x => x.TagId).ToListAsync();

        var computersQuery = _db.Computers
            .Include(c => c.Tags)
            .Include(c => c.Disks)
            .AsSplitQuery()
            .Where(c => macs.Contains(c.MacAddress));

        if (!isAdmin || accessibleCompIds.Any() || accessibleTagIds.Any())
        {
            computersQuery = computersQuery.Where(c =>
                accessibleCompIds.Contains(c.Id) ||
                c.Tags.Any(t => accessibleTagIds.Contains(t.Id)));
        }

        var computers = await computersQuery.ToListAsync();
        var compMap = computers.ToDictionary(c => c.MacAddress);

        var filteredList = new List<AgentTelemetryDto>();

        foreach (var dto in list)
        {
            if (compMap.TryGetValue(dto.MacAddress, out var comp))
            {
                dto.ComputerId = comp.Id;
                dto.DisplayName = comp.DisplayName;
                dto.Tags = comp.Tags.Select(t => t.Name).ToList();
                dto.CpuThreshold = comp.CpuThreshold;
                dto.RamThreshold = comp.RamThreshold;
                filteredList.Add(dto);
            }
        }

        var sortedResult = filteredList.OrderByDescending(dto =>
        {
            var comp = compMap[dto.MacAddress];
            var notifyTimes = new List<DateTime>();
            if (comp.CpuLastNotifyTime.HasValue) notifyTimes.Add(comp.CpuLastNotifyTime.Value);
            if (comp.RamLastNotifyTime.HasValue) notifyTimes.Add(comp.RamLastNotifyTime.Value);

            var lastDiskNotify = comp.Disks
                .Where(d => d.LastNotifyTime.HasValue)
                .Select(d => d.LastNotifyTime!.Value)
                .OrderByDescending(t => t).FirstOrDefault();

            if (lastDiskNotify != default) notifyTimes.Add(lastDiskNotify);

            return notifyTimes.Any() ? notifyTimes.Max() : DateTime.MinValue;
        })
        .ThenByDescending(dto => dto.Ts)
        .ToList();

        return ServiceResult<object>.Success(sortedResult);
    }
}