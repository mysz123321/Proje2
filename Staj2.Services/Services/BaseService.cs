using Microsoft.EntityFrameworkCore;
using Staj2.Infrastructure.Data;
using Staj2.Services.Models;

namespace Staj2.Services.Services;

public abstract class BaseService
{
    protected readonly AppDbContext _db;

    protected BaseService(AppDbContext db)
    {
        _db = db;
    }

    // Normal ServiceResult dönen metotlar için
    // Yöneticinin istediği gibi varsayılan değeri boş string ("") yapıyoruz
    protected async Task<ServiceResult> ExecuteWithDbHandlingAsync(Func<Task<ServiceResult>> action, string entityName = "")
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

            // 🌟 SENİN HARİKA FİKRİN: Eğer parametre boş geldiyse direkt genel hatayı dön!
            if (string.IsNullOrWhiteSpace(entityName))
            {
                return ServiceResult.Failure("Veritabanı hatası tespit edildi.");
            }

            // Eğer parametre DOLU geldiyse, eski özel mesaj mantığını çalıştır:
            if (ex.InnerException != null && (ex.InnerException.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                                           || ex.InnerException.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)))
            {
                return ServiceResult.Failure($"Bu {entityName} zaten sistemde kayıtlı. Lütfen farklı bir isim deneyin.");
            }

            if (ex.InnerException != null && ex.InnerException.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Failure($"{entityName} işlemi için seçtiğiniz bağlı verilerden biri bulunamadı.");
            }

            return ServiceResult.Failure($"{entityName} kaydedilirken veritabanı kural ihlali oluştu veya bağlantı koptu.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw; // Global Exception Middleware'in yakalaması için yukarı fırlatıyoruz
        }
    }

    // Generic ServiceResult<T> dönen metotlar için
    // 1. Değişiklik: entityName parametresine varsayılan değer olarak "" (boş string) atadık.
    protected async Task<ServiceResult<T>> ExecuteWithDbHandlingAsync<T>(Func<Task<ServiceResult<T>>> action, string entityName = "")
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

            // 2. Değişiklik: Eğer parametre gönderilmemişse (boşsa), genel hata dön.
            if (string.IsNullOrWhiteSpace(entityName))
            {
                return ServiceResult<T>.Failure("Veritabanı hatası tespit edildi.");
            }

            // 3. Değişiklik: UNIQUE kelimesini daha geniş kapsamlı yakalıyoruz (önceki metotta yaptığımız gibi)
            if (ex.InnerException != null && (ex.InnerException.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                                           || ex.InnerException.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)))
            {
                return ServiceResult<T>.Failure($"Bu {entityName} zaten sistemde kayıtlı. Lütfen farklı bir isim deneyin.");
            }

            if (ex.InnerException != null && ex.InnerException.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<T>.Failure($"{entityName} işlemi için seçtiğiniz bağlı verilerden biri bulunamadı.");
            }

            return ServiceResult<T>.Failure($"{entityName} kaydedilirken veritabanı kural ihlali oluştu veya bağlantı koptu.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw; // Global Exception Middleware'in yakalaması için yukarı fırlatıyoruz
        }
    }
}