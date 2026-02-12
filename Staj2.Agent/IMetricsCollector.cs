public interface IMetricsCollector
{
    Task<AgentTelemetryDto> CollectAsync(CancellationToken ct);
}
