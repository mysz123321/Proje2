using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using STAJ2.Models.Agent;
using STAJ2.Services;
using System.Globalization;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/agent-telemetry")]
public sealed class AgentTelemetryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IMailSender _mailSender;

    private static readonly Dictionary<string, AgentTelemetryDto> _latestData = new();

    public AgentTelemetryController(AppDbContext context, IConfiguration config, IMailSender mailSender)
    {
        _context = context;
        _config = config;
        _mailSender = mailSender;
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

        // 2. Cache Güncelleme
        lock (_latestData)
        {
            _latestData[dto.MacAddress] = dto;
        }

        try
        {
            // 3. Bilgisayar Kaydı/Güncelleme
            var computer = await _context.Computers
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
                    TotalDiskGb = dto.TotalDiskGb,
                    LastSeen = DateTime.Now
                };
                _context.Computers.Add(computer);
            }
            else
            {
                computer.LastSeen = DateTime.Now;
                computer.MachineName = dto.MachineName;
                computer.IpAddress = dto.Ip;
                computer.CpuModel = dto.CpuModel;
                computer.TotalDiskGb = dto.TotalDiskGb;

                if (Math.Abs(computer.TotalRamMb - dto.TotalRamMb) > 1)
                    computer.TotalRamMb = dto.TotalRamMb;
            }

            await _context.SaveChangesAsync(ct);

            // 4. Metrik Kaydı
            var metric = new ComputerMetric
            {
                ComputerId = computer.Id,
                CpuUsage = dto.CpuUsage,
                RamUsage = dto.RamUsage,
                DiskUsage = dto.DiskUsage,
                CreatedAt = DateTime.Now
            };

            _context.ComputerMetrics.Add(metric);
            await _context.SaveChangesAsync(ct);

            // --- 5. ALERTING (UYARI SİSTEMİ) ---
            // Mail gönderimi yavaş olabilir, bu yüzden Task.Run ile arka planda 
            // veya Cancellation'dan bağımsız bir şekilde işlemek daha güvenlidir.
            await CheckAndSendAlerts(computer, dto, ct);

            return Ok();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[INFO] Ingest: İstek istemci tarafından iptal edildi.");
            return StatusCode(499); // Client Closed Request
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Ingest: {ex}");
            return StatusCode(500, ex.Message);
        }
    }

    private async Task CheckAndSendAlerts(Computer computer, AgentTelemetryDto dto, CancellationToken ct)
    {
        var alertingConfig = _config.GetSection("Alerting");
        double cpuLimit = alertingConfig.GetValue<double>("CpuThreshold");
        double ramLimit = alertingConfig.GetValue<double>("RamThreshold");
        double diskLimit = alertingConfig.GetValue<double>("DiskThreshold");
        int intervalHours = alertingConfig.GetValue<int>("NotifyIntervalHours");

        // Adminin belirlediği ismi (DisplayName) kullanıyoruz
        string deviceName = !string.IsNullOrWhiteSpace(computer.DisplayName) ? computer.DisplayName : computer.MachineName;

        if (computer.LastNotifyTime == null || computer.LastNotifyTime.Value.AddHours(intervalHours) < DateTime.Now)
        {
            string alertReasons = "";

            if (dto.CpuUsage >= cpuLimit)
                alertReasons += $"* CPU: %{dto.CpuUsage:F1} (Eşik: %{cpuLimit}) [Hız: {computer.CpuModel}]\n";

            if (dto.RamUsage >= ramLimit)
                alertReasons += $"* RAM: %{dto.RamUsage:F1} (Eşik: %{ramLimit})\n";

            // Disk Parse ve Noktalama Düzeltmesi (972.3 sorununu çözer)
            var diskParts = dto.DiskUsage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < diskParts.Length; i += 2)
            {
                if (i + 1 < diskParts.Length)
                {
                    string diskName = diskParts[i].Replace(":", "");
                    // Virgülü noktaya çevirip InvariantCulture ile parse ederek ondalık hatasını önlüyoruz
                    string percentStr = diskParts[i + 1].Replace("%", "").Replace(",", ".");

                    if (double.TryParse(percentStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    {
                        if (val >= diskLimit)
                            alertReasons += $"* Disk ({diskName}): %{val:F1} (Eşik: %{diskLimit})\n";
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
                        await _mailSender.SendAsync(email, subject, body);
                    }

                    computer.LastNotifyTime = DateTime.Now;
                    await _context.SaveChangesAsync(ct);
                }
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