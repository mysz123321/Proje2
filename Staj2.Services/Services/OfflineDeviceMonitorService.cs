using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;

namespace Staj2.Services.Services;

public class OfflineDeviceMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;

    public OfflineDeviceMonitorService(IServiceProvider serviceProvider, IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // stoppingToken.IsCancellationRequested = "Uygulama durduruldu mu?" demektir.
        // Yani bu döngü SADECE proje çalışırken döner. Proje kapanırsa döngü biter.
        while (!stoppingToken.IsCancellationRequested)
        {
            // Bekleme süresini config'den al, yoksa varsayılan olarak 60 saniye kullan
            int checkIntervalSeconds = _config.GetValue<int>("Alerting:CheckIntervalSeconds", 60);

            await Task.Delay(TimeSpan.FromSeconds(checkIntervalSeconds), stoppingToken);

            // Kontrol metodunu çağır
            await CheckOfflineDevicesAsync(stoppingToken);
        }
    }

    private async Task CheckOfflineDevicesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mailSender = scope.ServiceProvider.GetRequiredService<IMailSender>();

        var alertingConfig = _config.GetSection("Alerting");

        // 1. Config'den Yönetici rol adını al
        var adminRoleName = _config["AppDefaults:AdminRoleName"] ?? "Yönetici";

        // 2. Sistemdeki tüm "Yönetici"leri tek seferde çekelim
        var adminEmails = await context.Users
            .Where(u => !u.IsDeleted && u.Roles.Any(r => r.Name == adminRoleName && !r.IsDeleted))
            .Select(u => u.Email)
            .ToListAsync(stoppingToken);

        int offlineThresholdSeconds = alertingConfig.GetValue<int>("OfflineThresholdSeconds", 150);
        int ignoreThresholdMinutes = alertingConfig.GetValue<int>("IgnoreThresholdMinutes", 15);

        var offlineThreshold = DateTime.Now.AddSeconds(-offlineThresholdSeconds);
        var ignoreThreshold = DateTime.Now.AddMinutes(-ignoreThresholdMinutes);

        var offlineComputers = await context.Computers
            .Where(c => !c.IsDeleted
                     && c.LastSeen <= offlineThreshold
                     && c.LastSeen >= ignoreThreshold
                     && !c.IsOfflineAlertSent)
            .ToListAsync(stoppingToken);

        foreach (var computer in offlineComputers)
        {
            // 3. SADECE bu cihaza atanmış kullanıcıları bul
            var assignedUserEmails = await context.UserComputerAccesses
                .Where(uca => uca.ComputerId == computer.Id && !uca.IsDeleted && !uca.User.IsDeleted)
                .Select(uca => uca.User.Email)
                .ToListAsync(stoppingToken);

            // Yönetici ve cihaza özel listeyi birleştirip mükerrer olanları filtrele
            var finalRecipients = adminEmails
                .Concat(assignedUserEmails)
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct()
                .ToList();

            // Eğer e-posta atılacak kimse yoksa atla
            if (finalRecipients.Count == 0) continue;

            string deviceName = !string.IsNullOrWhiteSpace(computer.DisplayName) ? computer.DisplayName : computer.MachineName;
            string subject = $"🚨 CİHAZ ÇEVRİMDIŞI: {deviceName}";
            string body = $"Merhaba,\n\n{deviceName} ({computer.IpAddress}) isimli cihaz pasife dönmüştür.\nSon Veri Alınan Zaman: {computer.LastSeen}\n\nSistem Kontrol Zamanı: {DateTime.Now}";

            foreach (var email in finalRecipients)
            {
                await mailSender.SendAsync(email, subject, body);
            }

            computer.IsOfflineAlertSent = true;
        }

        if (offlineComputers.Any())
        {
            await context.SaveChangesAsync(stoppingToken);
        }
    }
}