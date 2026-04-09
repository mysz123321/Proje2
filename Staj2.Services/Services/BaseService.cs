using Microsoft.EntityFrameworkCore;
using Staj2.Infrastructure.Data;
using Staj2.Services.Models;

namespace Staj2.Services.Services;

// İşlem tiplerini belirlediğimiz Enum
public enum DbOperation
{
    Create,
    Update,
    Delete,
    General // Varsayılan veya belirsiz işlemler için
}

public abstract class BaseService
{
    protected readonly AppDbContext _db;

    protected BaseService(AppDbContext db)
    {
        _db = db;
    }

    // 🌟 YENİ: Hata mesajlarını dinamik üreten yardımcı metot
    private string GenerateDbErrorMessage(DbUpdateException ex, string entityName, DbOperation operation)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            return "Veritabanı hatası tespit edildi.";

        // 1. UNIQUE Hataları (Daha çok Create/Update işlemlerinde olur)
        if (ex.InnerException != null && (ex.InnerException.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                                       || ex.InnerException.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)))
        {
            return $"Bu {entityName} zaten sistemde kayıtlı. Lütfen farklı bir değer deneyin.";
        }

        // 2. FOREIGN KEY Hataları (İşleme göre anlamı değişir!)
        if (ex.InnerException != null && ex.InnerException.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
        {
            if (operation == DbOperation.Delete)
            {
                // Eğer silerken FK hatası alıyorsak, veri başka bir yerde kullanılıyor demektir.
                return $"Bu {entityName} başka verilerle ilişkili (kullanımda) olduğu için silinemez.";
            }
            else
            {
                // Eklerken veya güncellerken alıyorsak, seçilen üst/bağlı veri (örn: Kategori) DB'de yok demektir.
                return $"{entityName} işlemi için seçtiğiniz bağlı verilerden biri bulunamadı.";
            }
        }

        // 3. Genel Bağlantı/Kural İhlali Hataları
        string actionWord = operation switch
        {
            DbOperation.Create => "eklenirken",
            DbOperation.Update => "güncellenirken",
            DbOperation.Delete => "silinirken",
            _ => "üzerinde işlem yapılırken"
        };

        return $"{entityName} {actionWord} veritabanı kural ihlali oluştu veya bağlantı koptu.";
    }


    // Normal ServiceResult dönen metotlar için
    protected async Task<ServiceResult> ExecuteWithDbHandlingAsync(
        Func<Task<ServiceResult>> action,
        string entityName = "",
        DbOperation operation = DbOperation.General) // Parametre eklendi
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var result = await action();

            if (result.IsSuccess)
                await transaction.CommitAsync();
            else
                await transaction.RollbackAsync();

            return result;
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();

            // Hata mesajını yeni dinamik metottan alıyoruz
            string errorMessage = GenerateDbErrorMessage(ex, entityName, operation);
            return ServiceResult.Failure(errorMessage);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Generic ServiceResult<T> dönen metotlar için
    protected async Task<ServiceResult<T>> ExecuteWithDbHandlingAsync<T>(
        Func<Task<ServiceResult<T>>> action,
        string entityName = "",
        DbOperation operation = DbOperation.General) // Parametre eklendi
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var result = await action();

            if (result.IsSuccess)
                await transaction.CommitAsync();
            else
                await transaction.RollbackAsync();

            return result;
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();

            // Hata mesajını yeni dinamik metottan alıyoruz
            string errorMessage = GenerateDbErrorMessage(ex, entityName, operation);
            return ServiceResult<T>.Failure(errorMessage);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}