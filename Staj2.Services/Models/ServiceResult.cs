namespace Staj2.Services.Models;

// Veri dönmeyen, sadece başarılı/başarısız durumunu bildiren işlemler için
public class ServiceResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }

    // İleride gerekirse buraya eklenebilir: 
    // public List<string>? Errors { get; set; } 
    // public int StatusCode { get; set; }

    // Kullanımı kolaylaştırmak için statik Factory (Üretici) metotlar:
    public static ServiceResult Success(string message = "İşlem başarılı.")
        => new ServiceResult { IsSuccess = true, Message = message };

    public static ServiceResult Failure(string message)
        => new ServiceResult { IsSuccess = false, Message = message };
}

// Geriye bir Data dönmesi gereken işlemler için (ServiceResult'tan miras alır)
public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; set; }

    public static ServiceResult<T> Success(T data, string message = "İşlem başarılı.")
        => new ServiceResult<T> { IsSuccess = true, Message = message, Data = data };

    public static new ServiceResult<T> Failure(string message)
        => new ServiceResult<T> { IsSuccess = false, Message = message, Data = default };
}