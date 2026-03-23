using Staj2.Services.Models.Agent;

namespace Staj2.Services.Interfaces;

public interface IAgentTelemetryService
{
    // (Yetkisiz mi, Hatalı İstek mi, Hata Mesajı, Gönderilecek Mailler Listesi)
    Task<(bool IsUnauthorized, bool IsBadRequest, string? ErrorMessage, List<(string Email, string Subject, string Body)>? Alerts)> IngestAsync(AgentTelemetryDto dto, string? agentKey, CancellationToken ct);

    Task<object> GetLatestAsync(int userId, bool isAdmin);
}