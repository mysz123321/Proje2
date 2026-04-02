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
        var recipients = alertingConfig.GetSection("Recipients").Get<List<string>>();

        if (recipients == null || recipients.Count == 0) return;

        // Ayarları config'den çekiyoruz. Bulunamazsa varsayılan değerler (150 ve 15) kullanılır.
        int offlineThresholdSeconds = alertingConfig.GetValue<int>("OfflineThresholdSeconds", 150);
        int ignoreThresholdMinutes = alertingConfig.GetValue<int>("IgnoreThresholdMinutes", 15);

        // Config'den gelen saniyeye göre çevrimdışı sayılma zamanı
        var offlineThreshold = DateTime.Now.AddSeconds(-offlineThresholdSeconds);

        // Config'den gelen dakikaya göre çok eskiden ölmüş cihazları yoksayma süresi
        var ignoreThreshold = DateTime.Now.AddMinutes(-ignoreThresholdMinutes);

        // Henüz maili atılmamış, belirlenen süreden uzun süredir yok olan AMA yoksayma süresi içinde kopmuş cihazlar
        var offlineComputers = await context.Computers
            .Where(c => !c.IsDeleted
                     && c.LastSeen <= offlineThreshold
                     && c.LastSeen >= ignoreThreshold
                     && !c.IsOfflineAlertSent)
            .ToListAsync(stoppingToken);

        foreach (var computer in offlineComputers)
        {
            string deviceName = !string.IsNullOrWhiteSpace(computer.DisplayName) ? computer.DisplayName : computer.MachineName;
            string subject = $"🚨 CİHAZ ÇEVRİMDIŞI: {deviceName}";
            string body = $"Merhaba,\n\n{deviceName} ({computer.IpAddress}) isimli cihaz pasife dönmüştür.\nSon Veri Alınan Zaman: {computer.LastSeen}\n\nSistem Kontrol Zamanı: {DateTime.Now}";

            foreach (var email in recipients)
            {
                await mailSender.SendAsync(email, subject, body);
            }

            // Bir sonraki döngüde aynı maili tekrar atmaması için bayrağı işaretle
            computer.IsOfflineAlertSent = true;
        }

        if (offlineComputers.Any())
        {
            await context.SaveChangesAsync(stoppingToken);
        }
    }
}