using Staj2.Services.Models;

namespace Staj2.Services.Interfaces;

public interface IComputerService
{
    // 1. Cihaz Detayı: (ErişimYasakMı, BulunamadıMı, Veri) döner
    Task<(bool isForbidden, bool isNotFound, object? data)> GetComputerAsync(int id, int userId, bool isAdmin);

    // 2. Disk Listesi: (ErişimYasakMı, Veri) döner
    Task<(bool isForbidden, object? data)> GetComputerDisksAsync(int computerId, int userId, bool isAdmin);

    // 3. Eşik Güncelleme: (ErişimYasakMı, BulunamadıMı, HatalıİstekMi, Mesaj) döner
    Task<(bool isForbidden, bool isNotFound, bool isBadRequest, string message)> UpdateThresholdsAsync(int computerId, UpdateThresholdsRequest request, int userId, bool isAdmin);

    // 4. Etiket Atama: (BulunamadıMı, Mesaj) döner
    Task<(bool isNotFound, string message)> UpdateComputerTagsAsync(int id, UpdateComputerTagsRequest request);

    // 5. İsim Değiştirme: (İşlem Sonucu, Bulunamadı mı?, Mesaj)
    Task<(bool isSuccess, bool isNotFound, string message)> UpdateDisplayNameAsync(UpdateComputerNameRequest request);

    // 6. Metrik Geçmişi: (Hatalı İstek mi?, Hata Mesajı, Veri)
    Task<(bool isBadRequest, string? errorMessage, object? data)> GetMetricsHistoryAsync(int id, string start, string end);

    // 7. Tüm Cihazları Getir: (Kullanıcı yetkilerine göre filtrelenmiş)
    Task<object> GetAllComputersAsync(int userId, bool isAdmin);

    // 8. Cihaz Silme: (Bulunamadı mı?, Hatalı İstek mi?, Mesaj)
    Task<(bool isNotFound, bool isBadRequest, string message)> DeleteComputerAsync(int id);

    // 9. Kullanıcının Etiketlerini Getir
    Task<object> GetMyTagsAsync(int userId, bool isAdmin);

    Task<PerformanceReportDto> GetPerformanceReportAsync(int userId, bool isAdmin);

    Task<MetricSummaryDto> GetMetricsSummaryAsync(int computerId, string metricType, string? diskName);
}