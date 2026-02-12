using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

public sealed class DefaultMetricsCollector : IMetricsCollector
{
    private PerformanceCounter? _cpuTotal;
    private PerformanceCounter? _availableRamMb;
    private bool _perfReady;

    private readonly CpuSampler _linuxCpu = new();

    public DefaultMetricsCollector()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                _cpuTotal = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _availableRamMb = new PerformanceCounter("Memory", "Available MBytes");

                // warm-up: ilk ölçüm 0 dönebiliyor
                _cpuTotal.NextValue();

                _perfReady = true;
            }
            catch
            {
                _perfReady = false;
            }
        }
    }

    public async Task<AgentTelemetryDto> CollectAsync(CancellationToken ct)
    {
        var agentId = GetOrCreateAgentId();
        var machineName = Environment.MachineName;
        var ip = GetLocalIPv4() ?? "-";

        double? cpu = null;
        double? availableRamMb = null;

        // Windows: gerçek CPU/RAM
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _perfReady)
        {
            try { cpu = Math.Round(_cpuTotal!.NextValue(), 2); } catch { /* ignore */ }
            try { availableRamMb = Math.Round(_availableRamMb!.NextValue(), 2); } catch { /* ignore */ }
        }
        else
        {
            // Linux: CPU /proc/stat
            cpu = await _linuxCpu.GetCpuPercentAsync(ct);
            availableRamMb = null; // Linux RAM'i sonra ekleriz
        }

        var disks = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
            .Select(d => new AgentTelemetryDto.DiskDto
            {
                Name = d.Name,
                TotalBytes = d.TotalSize,
                FreeBytes = d.TotalFreeSpace
            })
            .ToList();

        return new AgentTelemetryDto
        {
            Ts = DateTimeOffset.UtcNow,
            AgentId = agentId,
            MachineName = machineName,
            Ip = ip,
            CpuPercent = cpu,
            AvailableRamMb = availableRamMb,
            Disks = disks
        };
    }

    private static string GetOrCreateAgentId()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Staj2", "agent");

        Directory.CreateDirectory(dir);

        var file = Path.Combine(dir, "agent-id.txt");

        try
        {
            if (File.Exists(file))
            {
                var existing = File.ReadAllText(file).Trim();
                if (!string.IsNullOrWhiteSpace(existing))
                    return existing;
            }

            var id = Guid.NewGuid().ToString("N");
            File.WriteAllText(file, id);
            return id;
        }
        catch
        {
            // Dosyaya yazamazsa en azından runtime id üretelim
            return Guid.NewGuid().ToString("N");
        }
    }

    private static string? GetLocalIPv4()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            var ipProps = ni.GetIPProperties();
            foreach (var ua in ipProps.UnicastAddresses)
            {
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    var ip = ua.Address.ToString();
                    if (ip != "127.0.0.1")
                        return ip;
                }
            }
        }
        return null;
    }

    // Linux CPU sampler: /proc/stat
    private sealed class CpuSampler
    {
        private long? _prevIdle;
        private long? _prevTotal;

        public async Task<double?> GetCpuPercentAsync(CancellationToken ct)
        {
            if (!File.Exists("/proc/stat"))
                return null;

            try
            {
                var lines = await File.ReadAllLinesAsync("/proc/stat", ct);
                var first = lines.FirstOrDefault(x => x.StartsWith("cpu "));
                if (first is null) return null;

                var parts = first.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) return null;

                // cpu user nice system idle iowait irq softirq steal ...
                long user = long.Parse(parts[1]);
                long nice = long.Parse(parts[2]);
                long system = long.Parse(parts[3]);
                long idle = long.Parse(parts[4]);
                long iowait = parts.Length > 5 ? long.Parse(parts[5]) : 0;

                var idleAll = idle + iowait;

                long total = 0;
                // user..steal genelde ilk 8 alan
                for (int i = 1; i < Math.Min(parts.Length, 9); i++)
                    total += long.Parse(parts[i]);

                if (_prevTotal is null || _prevIdle is null)
                {
                    _prevTotal = total;
                    _prevIdle = idleAll;
                    return null; // ilk ölçümde yüzde yok
                }

                var totald = total - _prevTotal.Value;
                var idled = idleAll - _prevIdle.Value;

                _prevTotal = total;
                _prevIdle = idleAll;

                if (totald <= 0) return null;

                var usage = (double)(totald - idled) / totald * 100.0;
                return Math.Round(usage, 2);
            }
            catch
            {
                return null;
            }
        }
    }
}
