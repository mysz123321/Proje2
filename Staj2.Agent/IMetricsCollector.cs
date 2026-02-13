using STAJ2.Models.Agent; // DTO burada
using System.Threading;
using System.Threading.Tasks;

namespace Staj2.Agent; // Namespace ekledik

public interface IMetricsCollector
{
    Task<AgentTelemetryDto> CollectAsync(CancellationToken ct);
}