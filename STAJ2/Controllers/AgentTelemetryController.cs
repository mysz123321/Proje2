using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using STAJ2.Models.Agent;
using STAJ2.Services;
using System.Globalization;
// --- EKSİK OLAN SATIRLAR EKLENDİ ---
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
// -----------------------------------

namespace STAJ2.Controllers;

[ApiController]
[Route("api/agent-telemetry")]
public sealed class AgentTelemetryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;

    private static readonly Dictionary<string, AgentTelemetryDto> _latestData = new();

    public AgentTelemetryController(AppDbContext context, IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _config = config;
        _scopeFactory = scopeFactory;
    }

    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] AgentTelemetryDto dto, CancellationToken ct)
    {
        // 1. Güvenlik Kontrolü
        var expectedKey = _config["Agent:IngestKey"];
        if (!string.IsNullOrWhiteSpace(expectedKey))
        {
            if (!Request.Headers.TryGetValue("X-Agent-Key", out var got) || got != expectedKey)
                return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(dto.MacAddress))
            return BadRequest("MacAddress is required.");

        try
        {
            // 2. Bilgisayar Kaydı veya Güncelleme
            var computer = await _context.Computers
                .Include(c => c.Disks) // Diskleri de dahil ediyoruz
                .FirstOrDefaultAsync(c => c.MacAddress == dto.MacAddress, ct);

            if (computer == null)
            {
                computer = new Computer
                {
                    MacAddress = dto.MacAddress,
                    MachineName = dto.MachineName,
                    DisplayName = dto.MachineName, // İlk kayıtta agent'tan gelen isim
                    IpAddress = dto.Ip,
                    CpuModel = dto.CpuModel,//********
                    TotalRamMb = dto.TotalRamMb,
                    LastSeen = DateTime.Now,
                    // TotalDiskGb SİLİNDİ
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
            }

            // Bilgisayarı kaydediyoruz (Id oluşması için)
            await _context.SaveChangesAsync(ct);

            // --- YENİ DİSK MANTIĞI (GÜNCELLENMİŞ) ---
            // dto.TotalDiskGb formatı: "C: 465.1234 D: 0.1955"
            var diskTotalParts = dto.TotalDiskGb.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < diskTotalParts.Length; i += 2)
            {
                if (i + 1 < diskTotalParts.Length)
                {
                    string dName = diskTotalParts[i].Replace(":", "");
                    double.TryParse(diskTotalParts[i + 1].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double dSize);

                    // Mevcut diskleri kontrol et
                    var existingDisk = computer.Disks.FirstOrDefault(d => d.DiskName == dName);

                    if (existingDisk == null)
                    {
                        // 1. Eğer bu disk tabloda yoksa yeni kayıt ekle
                        _context.ComputerDisks.Add(new ComputerDisk
                        {
                            ComputerId = computer.Id,
                            DiskName = dName,
                            TotalSizeGb = dSize,
                            ThresholdPercent = null
                        });
                    }
                    else
                    {
                        // 2. Eğer disk varsa ama boyutu değişmişse (örn: SSD yükseltmesi), veritabanını güncelle
                        // Küçük yuvarlama farklarını (0.1 GB altı) görmezden gelmek için Math.Abs kullanıyoruz
                        if (Math.Abs(existingDisk.TotalSizeGb - dSize) > 0.1)
                        {
                            existingDisk.TotalSizeGb = dSize;
                        }
                    }
                }
            }
            await _context.SaveChangesAsync(ct);

            dto.DisplayName = computer.DisplayName; // Veritabanındaki güncel görünen adı DTO'ya bas
            dto.ComputerId = computer.Id;
            // 3. Metrik Kaydı
            var metric = new ComputerMetric
            {
                ComputerId = computer.Id,
                CpuUsage = dto.CpuUsage,
                RamUsage = dto.RamUsage,// Eski raporlar için string olarak kalsın veya silebilirsin
                CreatedAt = DateTime.Now
            };
            _context.ComputerMetrics.Add(metric);
            await _context.SaveChangesAsync(ct);

            // --- YENİ DİSK METRİK KAYDI ---
            // dto.DiskUsage formatı: "C: %40.5 D: %10.2"
            var currentDisks = await _context.ComputerDisks.Where(d => d.ComputerId == computer.Id).ToListAsync(ct);
            var diskUsageParts = dto.DiskUsage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < diskUsageParts.Length; i += 2)
            {
                if (i + 1 < diskUsageParts.Length)
                {
                    string dName = diskUsageParts[i].Replace(":", "");
                    string usageStr = diskUsageParts[i + 1].Replace("%", "").Replace(",", ".");
                    double.TryParse(usageStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double dUsage);

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
            await _context.SaveChangesAsync(ct);

            // Cache ve Alert işlemleri...
            dto.ComputerId = computer.Id; // ÖNEMLİ: ID'yi DTO'ya geri yaz
            dto.DisplayName = computer.DisplayName;

            lock (_latestData) { _latestData[dto.MacAddress] = dto; }
            _ = Task.Run(() => HandleBackgroundAlert(computer.Id, dto));
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
    private async Task HandleBackgroundAlert(int computerId, AgentTelemetryDto dto)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var mailSender = scope.ServiceProvider.GetRequiredService<IMailSender>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            try
            {
                var computer = await dbContext.Computers
                    .Include(c => c.Disks)
                    .FirstOrDefaultAsync(c => c.Id == computerId);

                if (computer == null) return;

                var alertingConfig = config.GetSection("Alerting");
                var recipients = alertingConfig.GetSection("Recipients").Get<List<string>>();

                // Alıcı yoksa hiç başlama
                if (recipients == null || recipients.Count == 0) return;

                int intervalHours = alertingConfig.GetValue<int>("NotifyIntervalHours", 1);
                string deviceName = !string.IsNullOrWhiteSpace(computer.DisplayName) ? computer.DisplayName : computer.MachineName;

                bool dbUpdateNeeded = false;

                // --- YARDIMCI YEREL FONKSİYON: Mail Gönderme İşi ---
                // Kod tekrarını önlemek için mail atma işlemini buraya aldık.
                async Task SendAlertMail(string title, string message)
                {
                    string subject = $"🚨 {title}: {deviceName}";
                    string fullBody = $"Merhaba,\n\n" +
                                      $"{deviceName} ({computer.IpAddress}) cihazında aşağıdaki limit aşımı tespit edilmiştir:\n\n" +
                                      $"{message}\n\n" +
                                      $"--------------------------------------------------\n" +
                                      $"Zaman: {DateTime.Now}";

                    foreach (var email in recipients)
                    {
                        try
                        {
                            await mailSender.SendAsync(email, subject, fullBody);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Mail Hatası ({email}): {ex.Message}");
                        }
                    }
                }
                // ----------------------------------------------------

                // 1. CPU KONTROLÜ
                if (computer.CpuThreshold.HasValue && dto.CpuUsage >= computer.CpuThreshold.Value)
                {
                    if (computer.CpuLastNotifyTime == null || (DateTime.Now - computer.CpuLastNotifyTime.Value).TotalHours >= intervalHours)
                    {
                        string msg = $"⚠️ CPU KULLANIMI: %{dto.CpuUsage:F1} (Limit: %{computer.CpuThreshold.Value})";
                        await SendAlertMail("CPU UYARISI", msg); // Anında gönder

                        computer.CpuLastNotifyTime = DateTime.Now;
                        dbUpdateNeeded = true;
                    }
                }

                // 2. RAM KONTROLÜ
                if (computer.RamThreshold.HasValue && dto.RamUsage >= computer.RamThreshold.Value)
                {
                    if (computer.RamLastNotifyTime == null || (DateTime.Now - computer.RamLastNotifyTime.Value).TotalHours >= intervalHours)
                    {
                        string msg = $"⚠️ RAM KULLANIMI: %{dto.RamUsage:F1} (Limit: %{computer.RamThreshold.Value})";
                        await SendAlertMail("RAM UYARISI", msg); // Anında gönder

                        computer.RamLastNotifyTime = DateTime.Now;
                        dbUpdateNeeded = true;
                    }
                }

                // 3. DİSK KONTROLÜ (Tek Tek)
                var diskUsageParts = dto.DiskUsage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
                                    string msg = $"⚠️ DİSK DOLULUK ({diskName}): %{currentUsage:F1} (Limit: %{targetDisk.ThresholdPercent.Value})";
                                    await SendAlertMail($"DİSK UYARISI ({diskName})", msg); // Anında ve diske özel başlıkla gönder

                                    targetDisk.LastNotifyTime = DateTime.Now;
                                    dbUpdateNeeded = true;
                                }
                            }
                        }
                    }
                }

                // Eğer herhangi bir mail atıldıysa zamanlayıcıları kaydet
                if (dbUpdateNeeded)
                {
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Alert Error] {ex.Message}");
            }
        }
    }


    [HttpGet("latest")]
    public async Task<IActionResult> Latest()
    {
        List<AgentTelemetryDto> list;
        lock (_latestData) { list = _latestData.Values.OrderByDescending(x => x.Ts).ToList(); }

        var macs = list.Select(x => x.MacAddress).ToList();
        var computers = await _context.Computers.Include(c => c.Tags).Where(c => macs.Contains(c.MacAddress)).ToListAsync();

        foreach (var dto in list)
        {
            var comp = computers.FirstOrDefault(c => c.MacAddress == dto.MacAddress);
            if (comp != null)
            {
                dto.ComputerId = comp.Id;
                dto.DisplayName = comp.DisplayName;
                dto.Tags = comp.Tags.Select(t => t.Name).ToList(); // Etiketleri aktar
            }
        }
        return Ok(list);
    }
}