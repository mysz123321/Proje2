using Staj2.Services.Models;
using Staj2.Services.Models.Agent;

namespace Staj2.Services.Interfaces;

public interface IAgentTelemetryService
{
    // Tuple yerine ServiceResult içerisine liste olarak mail verilerini koyuyoruz
    Task<ServiceResult<List<(string Email, string Subject, string Body)>>> IngestAsync(AgentTelemetryDto dto, string? agentKey, CancellationToken ct);

    Task<ServiceResult<object>> GetLatestAsync(int userId, bool isAdmin);
}