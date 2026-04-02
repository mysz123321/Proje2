using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;
using Staj2.Services.Models.Agent;
using System.Globalization;

namespace Staj2.Services.Services;

public class AgentTelemetryService : IAgentTelemetryService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    // Anlık metrikleri RAM'de tutan önbellek
    private static readonly Dictionary<string, AgentTelemetryDto> _latestData = new();

    public AgentTelemetryService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<(bool IsUnauthorized, bool IsBadRequest, string? ErrorMessage, List<(string Email, string Subject, string Body)>? Alerts)> IngestAsync(AgentTelemetryDto dto, string? agentKey, CancellationToken ct)
    {
        // 1. Güvenlik Kontrolü
        var expectedKey = _config["Agent:IngestKey"];
        if (!string.IsNullOrWhiteSpace(expectedKey) && agentKey != expectedKey)
        {
            return (true, false, null, null);
        }

        if (string.IsNullOrWhiteSpace(dto.MacAddress))
            return (false, true, "MacAddress is required.", null);

        // 2. Bilgisayar Kaydı veya Güncelleme
        var computer = await _context.Computers
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
            _context.Computers.Add(computer);
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

        await _context.SaveChangesAsync(ct);

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
                    _context.ComputerDisks.Add(new ComputerDisk
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
        await _context.SaveChangesAsync(ct);

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
        _context.ComputerMetrics.Add(metric);

        var currentDisks = await _context.ComputerDisks.Where(d => d.ComputerId == computer.Id).ToListAsync(ct);
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
                        _context.DiskMetrics.Add(new DiskMetric
                        {
                            ComputerDiskId = diskEntity.Id,
                            UsedPercent = dUsage,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }
        }
        await _context.SaveChangesAsync(ct);

        // =======================================================
        // 5. ALERT (UYARI) KONTROLLERİ VE MAİL İÇERİĞİ HAZIRLAMA
        // =======================================================
        var alertsToSend = new List<(string Email, string Subject, string Body)>();
        var alertingConfig = _config.GetSection("Alerting");
        var recipients = alertingConfig.GetSection("Recipients").Get<List<string>>();

        if (recipients != null && recipients.Count > 0)
        {
            int intervalHours = alertingConfig.GetValue<int>("NotifyIntervalHours", 1);
            string deviceName = !string.IsNullOrWhiteSpace(computer.DisplayName) ? computer.DisplayName : computer.MachineName;
            bool dbUpdateNeeded = false;

            void CreateAlert(string title, string message)
            {
                string subject = $"🚨 {title}: {deviceName}";
                string fullBody = $"Merhaba,\n\n{deviceName} ({computer.IpAddress}) cihazında aşağıdaki limit aşımı tespit edilmiştir:\n\n{message}\n\n--------------------------------------------------\nZaman: {DateTime.Now}";
                foreach (var email in recipients) alertsToSend.Add((email, subject, fullBody));
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

            if (dbUpdateNeeded) await _context.SaveChangesAsync(ct);
        }
        dto.Ts = DateTime.Now;
        lock (_latestData) { _latestData[dto.MacAddress] = dto; }

        return (false, false, null, alertsToSend);
    }

    public async Task<object> GetLatestAsync(int userId, bool isAdmin)
    {
        List<AgentTelemetryDto> list;
        lock (_latestData) { list = _latestData.Values.OrderByDescending(x => x.Ts).ToList(); }

        var macs = list.Select(x => x.MacAddress).ToList();

        var accessibleCompIds = await _context.UserComputerAccesses.Where(x => x.UserId == userId).Select(x => x.ComputerId).ToListAsync();
        var accessibleTagIds = await _context.UserTagAccesses.Where(x => x.UserId == userId).Select(x => x.TagId).ToListAsync();

        var computersQuery = _context.Computers
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

        return sortedResult;
    }
}