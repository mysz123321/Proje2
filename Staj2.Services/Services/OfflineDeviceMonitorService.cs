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
            // Her 60 saniyede bir kontrol et
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

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

        // 150 saniyeden uzun süredir haber alınamayan cihazlar
        var offlineThreshold = DateTime.Now.AddSeconds(-150);

        // Çok eskiden ölmüş cihazları yoksayma süresi (Örn: 15 dakikadan daha uzun süredir yoksa zaten eskiden kapanmıştır)
        var ignoreThreshold = DateTime.Now.AddMinutes(-15);

        // Henüz maili atılmamış, 150 saniyeden uzun süredir yok olan AMA son 15 dakika içinde kopmuş cihazlar
        var offlineComputers = await context.Computers
            .Where(c => !c.IsDeleted
                     && c.LastSeen <= offlineThreshold
                     && c.LastSeen >= ignoreThreshold // YENİ EKLENEN KISIM
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

            // Bir sonraki 60 saniyelik döngüde aynı maili tekrar atmaması için bayrağı işaretle
            computer.IsOfflineAlertSent = true;
        }

        if (offlineComputers.Any())
        {
            await context.SaveChangesAsync(stoppingToken);
        }
    }
}