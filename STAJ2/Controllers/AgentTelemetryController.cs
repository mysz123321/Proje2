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
                    CpuModel = dto.CpuModel,
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

            // --- YENİ DİSK MANTIĞI ---
            // dto.TotalDiskGb formatı: "C: 465.1234 D: 0.1955"
            var diskTotalParts = dto.TotalDiskGb.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < diskTotalParts.Length; i += 2)
            {
                if (i + 1 < diskTotalParts.Length)
                {
                    string dName = diskTotalParts[i].Replace(":", "");
                    double.TryParse(diskTotalParts[i + 1].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double dSize);

                    // Eğer bu disk tabloda yoksa ekle
                    if (!computer.Disks.Any(d => d.DiskName == dName))
                    {
                        _context.ComputerDisks.Add(new ComputerDisk
                        {
                            ComputerId = computer.Id,
                            DiskName = dName,
                            TotalSizeGb = dSize,
                            ThresholdPercent = 90.0 // Varsayılan eşik
                        });
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
            dto.ComputerId = computer.Id;
            lock (_latestData) { _latestData[dto.MacAddress] = dto; }
            _ = Task.Run(() => HandleBackgroundAlert(computer.Id, dto));

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    // Bu metod arka planda mail gönderip DB günceller
    private async Task HandleBackgroundAlert(int computerId, AgentTelemetryDto dto)
    {
        // Yeni bir scope açarak veritabanı ve mail servislerini alıyoruz
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var mailSender = scope.ServiceProvider.GetRequiredService<IMailSender>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            try
            {
                // Bilgisayarı ve ona bağlı tüm disk ayarlarını veritabanından çekiyoruz
                var computer = await dbContext.Computers
                    .Include(c => c.Disks)
                    .FirstOrDefaultAsync(c => c.Id == computerId);

                if (computer == null) return;

                // Bildirim aralığını yine config'den alabiliriz (veya bunu da bilgisayar bazlı yapabilirsin)
                var alertingConfig = config.GetSection("Alerting");
                int intervalHours = alertingConfig.GetValue<int>("NotifyIntervalHours", 24);

                // Zaman Kontrolü: Eğer son bildirimden bu yana belirlenen süre geçmediyse işlem yapma
                if (computer.LastNotifyTime != null && computer.LastNotifyTime.Value.AddHours(intervalHours) > DateTime.Now)
                {
                    return;
                }

                string alertReasons = "";
                string deviceName = !string.IsNullOrWhiteSpace(computer.DisplayName) ? computer.DisplayName : computer.MachineName;

                // 1. CPU Kontrolü: Veritabanında eşik tanımlanmışsa (null değilse) kontrol et
                if (computer.CpuThreshold.HasValue && dto.CpuUsage >= computer.CpuThreshold.Value)
                {
                    alertReasons += $"* CPU Kullanımı: %{dto.CpuUsage:F1} (Belirlenen Eşik: %{computer.CpuThreshold.Value})\n";
                }

                // 2. RAM Kontrolü: Veritabanında eşik tanımlanmışsa kontrol et
                if (computer.RamThreshold.HasValue && dto.RamUsage >= computer.RamThreshold.Value)
                {
                    alertReasons += $"* RAM Kullanımı: %{dto.RamUsage:F1} (Belirlenen Eşik: %{computer.RamThreshold.Value})\n";
                }

                // 3. Disk Kontrolü: Dinamik olarak gelen her diski kendi eşiğiyle karşılaştır
                // dto.DiskUsage formatı: "C: %40.5 D: %10.2"
                var diskUsageParts = dto.DiskUsage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < diskUsageParts.Length; i += 2)
                {
                    if (i + 1 < diskUsageParts.Length)
                    {
                        string diskName = diskUsageParts[i].Replace(":", "").Trim(); // Örn: "C"
                        string percentStr = diskUsageParts[i + 1].Replace("%", "").Replace(",", ".");

                        if (double.TryParse(percentStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double currentUsage))
                        {
                            // Veritabanında bu diske özel bir eşik ayarı var mı bakıyoruz
                            var diskSetting = computer.Disks.FirstOrDefault(d => d.DiskName == diskName);

                            // Eğer eşik atanmışsa (null değilse) ve sınırı aşmışsa uyarılara ekle
                            if (diskSetting != null && diskSetting.ThresholdPercent.HasValue && currentUsage >= diskSetting.ThresholdPercent.Value)
                            {
                                alertReasons += $"* Disk ({diskName}): %{currentUsage:F1} (Belirlenen Eşik: %{diskSetting.ThresholdPercent.Value})\n";
                            }
                        }
                    }
                }

                // Eğer herhangi bir aşım varsa mail gönder
                if (!string.IsNullOrEmpty(alertReasons))
                {
                    var recipients = alertingConfig.GetSection("Recipients").Get<List<string>>();
                    if (recipients != null && recipients.Count > 0)
                    {
                        string subject = $"⚠️ KRİTİK SİSTEM UYARISI: {deviceName}";
                        string body = $"Merhaba,\n\n{deviceName} cihazında aşağıdaki sınırlar aşılmıştır:\n\n" +
                                      $"{alertReasons}\n" +
                                      $"Ölçüm Zamanı: {DateTime.Now}\n" +
                                      $"Cihaz IP: {computer.IpAddress}\n\n" +
                                      $"Not: Bu cihaz için yeni bir uyarı en erken {intervalHours} saat sonra gönderilecektir.";

                        foreach (var email in recipients)
                        {
                            await mailSender.SendAsync(email, subject, body);
                        }

                        // Son bildirim zamanını güncelle ki sürekli mail atmasın
                        computer.LastNotifyTime = DateTime.Now;
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Alert Error] {ex.Message}");
            }
        }
    }

    [HttpGet("latest")]
    public IActionResult Latest()
    {
        lock (_latestData)
        {
            return Ok(_latestData.Values.OrderByDescending(x => x.Ts).ToList());
        }
    }
}