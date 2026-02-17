using System.Management;
using Microsoft.Win32;
using STAJ2.Models.Agent;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Staj2.Agent;

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

                // Isınma turu
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
        var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed).ToList();

        // MB olan disklerde 0.19xxx görünmesi için virgülden sonra 4 basamak (F4) kullanıyoruz
        var totalDiskParts = drives.Select(d =>
        {
            double gbSize = d.TotalSize / 1073741824.0;
            return $"{d.Name.Replace("\\", "")} {gbSize.ToString("F4")}";
        });
        string totalDiskStr = string.Join(" ", totalDiskParts);

        var usageDiskParts = drives.Select(d => {
            double used = d.TotalSize - d.TotalFreeSpace;
            double percent = Math.Round((used / d.TotalSize) * 100, 1);
            return $"{d.Name.Replace("\\", "")} %{percent}";
        });
        string usageDiskStr = string.Join(" ", usageDiskParts);

        return new AgentTelemetryDto
        {
            MacAddress = GetMacAddress(),
            MachineName = Environment.MachineName,
            Ip = GetLocalIPv4() ?? "-",
            CpuModel = GetCpuModelName(), // Yeni garanti yöntem
            TotalRamMb = GetTotalRamInfo(),
            TotalDiskGb = totalDiskStr,    // "C: 465.1234 D: 0.1955"
            DiskUsage = usageDiskStr,
            CpuUsage = GetCpuUsageValue(),
            RamUsage = GetRamUsagePercent(),
            Ts = DateTime.Now
        };
    }

    // --- YARDIMCI HESAPLAMA METOTLARI ---

    private double GetTotalRamInfo()
    {
        // Toplam RAM (MB cinsinden)
        var gcInfo = GC.GetGCMemoryInfo();
        return Math.Round(gcInfo.TotalAvailableMemoryBytes / 1024.0 / 1024.0, 0);
    }

    private double GetCpuUsageValue()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _perfReady)
        {
            try { return Math.Round(_cpuTotal!.NextValue(), 1); } catch { return 0; }
        }
        return 0; // Linux için CpuSampler kullanılıyor
    }

    private double GetRamUsagePercent()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _perfReady)
        {
            try
            {
                double total = GetTotalRamInfo();
                double available = _availableRamMb!.NextValue();
                double used = total - available;
                return Math.Round((used / total) * 100, 1);
            }
            catch { return 0; }
        }
        return 0;
    }

    private string GetCpuModelName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                // WMI ile işlemci bilgilerini sorguluyoruz
                using var searcher = new ManagementObjectSearcher("SELECT Name, MaxClockSpeed FROM Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "";
                    // MaxClockSpeed MHz cinsinden gelir (Örn: 2700)
                    uint speedMhz = (uint)(obj["MaxClockSpeed"] ?? 0);
                    double ghz = speedMhz / 1000.0;

                    // Eğer isim zaten GHz içeriyorsa direkt döndür, içermiyorsa biz ekleyelim
                    if (name.Contains("@") || name.ToLower().Contains("ghz"))
                    {
                        return name.Trim();
                    }

                    return $"{name.Trim()} @ {ghz:F2} GHz";
                }
            }
            catch
            {
                // WMI hata verirse Registry'ye geri düş (Fallback)
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                return key?.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? "Unknown CPU";
            }
        }
        return RuntimeInformation.OSDescription;
    }

    private string GetMacAddress()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
         // Sanal ve Loopback olmayanları al, OperationalStatus'a BAKMA (Down olsa da al)
         .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                && !n.Description.ToLower().Contains("virtual")
                && !n.Description.ToLower().Contains("pseudo"))
         .OrderByDescending(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet) // Ethernet varsa hep onu seç
         .ThenBy(n => n.Name);

            var mac = interfaces.Select(n => n.GetPhysicalAddress().ToString()).FirstOrDefault();

            // Formatsız gelirse (AABBCCDDEEFF), aralara tire koyarak daha okunaklı yapabilirsin:
            if (!string.IsNullOrEmpty(mac) && mac.Length == 12)
            {
                return string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));
            }

            return mac ?? "00:00:00:00:00:00";
        }
        catch { return "00:00:00:00:00:00"; }
    }

    private static string? GetLocalIPv4()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            var ipProps = ni.GetIPProperties();
            foreach (var ua in ipProps.UnicastAddresses)
            {
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ua.Address))
                    return ua.Address.ToString();
            }
        }
        return null;
    }

    private sealed class CpuSampler
    {
        private long? _prevIdle, _prevTotal;
        public async Task<double?> GetCpuPercentAsync(CancellationToken ct)
        {
            if (!File.Exists("/proc/stat")) return null;
            try
            {
                var lines = await File.ReadAllLinesAsync("/proc/stat", ct);
                var parts = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) return null;

                long idle = long.Parse(parts[4]);
                long total = 0;
                for (int i = 1; i < Math.Min(parts.Length, 9); i++) total += long.Parse(parts[i]);

                // Pattern Matching kullanarak değerleri 'prevTotal' ve 'prevIdle' değişkenlerine güvenle alıyoruz
                if (_prevTotal is not long prevTotal || _prevIdle is not long prevIdle)
                {
                    _prevTotal = total;
                    _prevIdle = idle;
                    return null;
                }

                long dTotal = total - prevTotal;
                long dIdle = idle - prevIdle;

                _prevTotal = total;
                _prevIdle = idle;

                return dTotal > 0 ? Math.Round((double)(dTotal - dIdle) / dTotal * 100.0, 2) : 0;
            }
            catch { return null; }
        }
    }
}