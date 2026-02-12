using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using STAJ2.Models.Agent;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/agent-telemetry")]
public sealed class AgentTelemetryController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    private static readonly object _lock = new();
    private static readonly Dictionary<string, AgentTelemetryDto> _latestByAgent = new();

    public AgentTelemetryController(IWebHostEnvironment env, IConfiguration config)
    {
        _env = env;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] AgentTelemetryDto dto, CancellationToken ct)
    {
        var expectedKey = _config["Agent:IngestKey"];
        if (!string.IsNullOrWhiteSpace(expectedKey))
        {
            var got = Request.Headers["X-Agent-Key"].ToString();
            if (!string.Equals(got, expectedKey, StringComparison.Ordinal))
                return Unauthorized();
        }

        var folder = Path.Combine(_env.ContentRootPath, "App_Data", "agent");
        Directory.CreateDirectory(folder);

        var file = Path.Combine(folder, "telemetry.ndjson");

        var line = JsonSerializer.Serialize(dto);
        await System.IO.File.AppendAllTextAsync(file, line + Environment.NewLine, Encoding.UTF8, ct);

        lock (_lock)
        {
            var key = string.IsNullOrWhiteSpace(dto.AgentId) ? dto.MachineName : dto.AgentId;
            _latestByAgent[key] = dto;
        }

        return Ok();
    }

    [HttpGet("latest")]
    public IActionResult Latest()
    {
        lock (_lock)
        {
            return Ok(_latestByAgent.Values
                .OrderByDescending(x => x.Ts)
                .ToList());
        }
    }
}
