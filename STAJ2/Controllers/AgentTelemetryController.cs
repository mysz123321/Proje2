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
                    DisplayName = dto.MachineName,
                    IpAddress = dto.Ip,
                    CpuModel = dto.CpuModel,
                    TotalRamMb = dto.TotalRamMb,
                    LastSeen = DateTime.Now
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
        // YENİ BİR SCOPE (BAĞLANTI) AÇIYORUZ
        using (var scope = _scopeFactory.CreateScope())
        {
            // Taze servisleri çağırıyoruz
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var mailSender = scope.ServiceProvider.GetRequiredService<IMailSender>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            try
            {
                // Bilgisayarı taze context ile tekrar çekiyoruz
                var computer = await dbContext.Computers.FindAsync(computerId);
                if (computer == null) return;

                var alertingConfig = config.GetSection("Alerting");
                double cpuLimit = alertingConfig.GetValue<double>("CpuThreshold");
                double ramLimit = alertingConfig.GetValue<double>("RamThreshold");
                double diskLimit = alertingConfig.GetValue<double>("DiskThreshold");
                int intervalHours = alertingConfig.GetValue<int>("NotifyIntervalHours");

                string deviceName = !string.IsNullOrWhiteSpace(computer.DisplayName) ? computer.DisplayName : computer.MachineName;

                // Süre kontrolü
                if (computer.LastNotifyTime == null || computer.LastNotifyTime.Value.AddHours(intervalHours) < DateTime.Now)
                {
                    string alertReasons = "";

                    // CPU Kontrol
                    if (dto.CpuUsage >= cpuLimit)
                        alertReasons += $"* CPU: %{dto.CpuUsage:F1} (Eşik: %{cpuLimit}) [Hız: {computer.CpuModel}]\n";

                    // RAM Kontrol
                    if (dto.RamUsage >= ramLimit)
                        alertReasons += $"* RAM: %{dto.RamUsage:F1} (Eşik: %{ramLimit})\n";

                    // AgentTelemetryController.cs içindeki HandleBackgroundAlert metodunun ilgili kısmı

                    // Disk Kontrolü
                    var diskParts = dto.DiskUsage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < diskParts.Length; i += 2)
                    {
                        if (i + 1 < diskParts.Length)
                        {
                            string diskNameWithColon = diskParts[i]; // Örn: "C:"
                            string diskName = diskNameWithColon.Replace(":", "").Trim(); // Örn: "C"
                            string percentStr = diskParts[i + 1].Replace("%", "").Replace(",", ".");

                            if (double.TryParse(percentStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double currentUsage))
                            {
                                // Diske özel eşik kontrolü: DiskThreshold_C gibi bir anahtar var mı?
                                // Yoksa genel DiskThreshold değerini kullan.
                                double specificLimit = alertingConfig.GetValue<double>($"DiskThreshold_{diskName}", diskLimit);

                                if (currentUsage >= specificLimit)
                                {
                                    alertReasons += $"* Disk ({diskName}): %{currentUsage:F1} (Eşik: %{specificLimit})\n";
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(alertReasons))
                    {
                        var recipients = alertingConfig.GetSection("Recipients").Get<List<string>>();
                        if (recipients != null && recipients.Count > 0)
                        {
                            string subject = $"⚠️ KRİTİK UYARI: {deviceName}";
                            string body = $"Merhaba,\n\n{deviceName} isimli cihazda limit aşımları tespit edildi:\n\n" +
                                          $"{alertReasons}\n" +
                                          $"Zaman: {DateTime.Now}\n" +
                                          $"IP: {computer.IpAddress}\n" +
                                          $"MAC: {computer.MacAddress}\n\n" +
                                          $"Bu cihaz için bir sonraki uyarı en erken {intervalHours} saat sonra gönderilecektir.";

                            foreach (var email in recipients)
                            {
                                await mailSender.SendAsync(email, subject, body);
                            }

                            // --- GÜNCELLEME İŞLEMİ ---
                            // Artık kendi context'imiz olduğu için sorunsuz çalışacak
                            computer.LastNotifyTime = DateTime.Now;
                            await dbContext.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Arka planda olduğu için console'a yazdırıyoruz
                Console.WriteLine($"[Background Alert Error] {ex.Message}");
            }
        } // Scope burada biter, dbContext otomatik olarak dispose edilir.
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