using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Domain.Entities;
using Staj2.Infrastructure.Data;
using STAJ2.Models.Agent;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/agent-telemetry")]
public sealed class AgentTelemetryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    private static readonly Dictionary<string, AgentTelemetryDto> _latestData = new();

    public AgentTelemetryController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
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

        // 2. RAM Cache Güncelle
        lock (_latestData)
        {
            _latestData[dto.MacAddress] = dto;
        }

        // 3. Veritabanı İşlemleri
        try
        {
            // ÖNCE bilgisayarı buluyoruz (Tanımlama burada yapılıyor)
            var computer = await _context.Computers
                .FirstOrDefaultAsync(c => c.MacAddress == dto.MacAddress, ct);

            if (computer == null)
            {
                // A) Yeni Bilgisayar
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
                // B) Mevcut Bilgisayar Güncelleme
                computer.LastSeen = DateTime.Now;
                computer.MachineName = dto.MachineName;
                computer.IpAddress = dto.Ip;
                computer.CpuModel = dto.CpuModel;
                computer.TotalDiskGb = dto.TotalDiskGb;

                if (Math.Abs(computer.TotalRamMb - dto.TotalRamMb) > 1)
                    computer.TotalRamMb = dto.TotalRamMb;
            }

            // Değişiklikleri kaydet (ComputerId oluşması için)
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

            return Ok();
        }
        catch (Exception ex)
        {
            // Hatayı görmek için konsola yazdıralım
            Console.WriteLine($"[ERROR] Ingest: {ex}");
            return StatusCode(500, ex.Message);
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