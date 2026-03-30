using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Staj2.Services.Models.Agent;
using STAJ2.MailServices;
using Staj2.Services.Interfaces;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/agent-telemetry")]
public class AgentTelemetryController : ControllerBase
{
    private readonly IAgentTelemetryService _telemetryService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    // ScopeFactory kalıyor çünkü Task.Run (arka plan işlemi) Controller'ın yaşam döngüsünden bağımsız çalışmak zorundadır.
    public AgentTelemetryController(IAgentTelemetryService telemetryService, IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _telemetryService = telemetryService;
        _scopeFactory = scopeFactory;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] AgentTelemetryDto dto, CancellationToken ct)
    {
        // Header'dan key'i oku ve servise yolla
        Request.Headers.TryGetValue("X-Agent-Key", out var agentKey);

        var result = await _telemetryService.IngestAsync(dto, agentKey, ct);

        if (result.IsUnauthorized) return Unauthorized();
        if (result.IsBadRequest) return BadRequest(result.ErrorMessage);

        // Servisten dönen mailler varsa arka planda fırlat!
        if (result.Alerts != null && result.Alerts.Any())
        {
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var mailSender = scope.ServiceProvider.GetRequiredService<IMailSender>();

                foreach (var alert in result.Alerts)
                {
                    try { await mailSender.SendAsync(alert.Email, alert.Subject, alert.Body); }
                    catch (Exception ex) { Console.WriteLine($"[Alert Mail Error]: {ex.Message}"); }
                }
            });
        }

        return Ok();
    }

    [HttpGet("latest")]
    [Authorize]
    public async Task<IActionResult> Latest()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int userId = int.TryParse(userIdString, out int id) ? id : 0;

        var adminRoleName = _config["AppDefaults:AdminRoleName"] ?? "Yönetici";
        bool isAdmin = User.IsInRole(adminRoleName);

        var result = await _telemetryService.GetLatestAsync(userId, isAdmin);

        return Ok(result);
    }


}