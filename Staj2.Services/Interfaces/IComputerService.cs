using Staj2.Services.Models;

namespace Staj2.Services.Interfaces;

public interface IComputerService
{
    // Cihaz Detayı: (ErişimYasakMı, BulunamadıMı, Veri) döner
    Task<(bool isForbidden, bool isNotFound, object? data)> GetComputerAsync(int id, int userId, bool isAdmin);

    // Disk Listesi: (ErişimYasakMı, Veri) döner
    Task<(bool isForbidden, object? data)> GetComputerDisksAsync(int computerId, int userId, bool isAdmin);

    // Eşik Güncelleme: (ErişimYasakMı, BulunamadıMı, HataMesajı) döner
    Task<(bool isForbidden, bool isNotFound, string? errorMessage)> UpdateThresholdsAsync(int computerId, UpdateThresholdsRequest request, int userId, bool isAdmin);
    // 4. Etiket Atama (Bulunamadıysa false, başarılıysa true döner)
    Task<bool> UpdateComputerTagsAsync(int id, UpdateComputerTagsRequest request);

    // 5. İsim Değiştirme (İşlem Sonucu, Bulunamadı mı?, Hata Mesajı)
    Task<(bool isSuccess, bool isNotFound, string? errorMessage)> UpdateDisplayNameAsync(UpdateComputerNameRequest request);

    // 6. Metrik Geçmişi (Hatalı İstek mi?, Hata Mesajı, Veri)
    Task<(bool isBadRequest, string? errorMessage, object? data)> GetMetricsHistoryAsync(int id, string start, string end);
    // 7. Tüm Cihazları Getir (Kullanıcı yetkilerine göre filtrelenmiş)
    Task<object> GetAllComputersAsync(int userId, bool isAdmin);

    // 8. Cihaz Silme (Bulunamadı mı?, Hatalı İstek mi?, Hata Mesajı)
    Task<(bool isNotFound, bool isBadRequest, string? errorMessage)> DeleteComputerAsync(int id);

    // 9. Kullanıcının Etiketlerini Getir
    Task<object> GetMyTagsAsync(int userId, bool isAdmin);
    Task<PerformanceReportDto> GetPerformanceReportAsync(int userId, bool isAdmin);
    Task<MetricSummaryDto> GetMetricsSummaryAsync(int computerId, string metricType, string? diskName);
}