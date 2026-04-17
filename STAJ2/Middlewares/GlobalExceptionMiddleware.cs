using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace STAJ2.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // İsteği sonraki adıma (diğer middleware'lere veya controller'a) ilet
            await _next(context);
        }
        catch (Exception ex)
        {
            // Bir hata fırlatılırsa burada yakala
            _logger.LogError(ex, "Sistemde beklenmeyen bir hata meydana geldi.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError; // 500 status code

        // Varsayılan genel hata mesajı
        string message = "Sistemde beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.";

        // Eğer hata spesifik olarak veritabanı bağlantısı veya güncellemesi ile ilgiliyse mesajı özelleştirebiliriz
        if (exception is SqlException || exception is DbUpdateException)
        {
            message = "Veritabanı sunucusu ile bağlantı kurulamadı veya işlem kaydedilemedi. Lütfen daha sonra tekrar deneyin.";
        }

        // İstemciye (UI/Frontend) dönecek JSON formatı
        var response = new
        {
            isSuccess = false,
            errorMessage = message
            // Not: exception.Message değerini güvenlik için dışarı dönmüyoruz, sadece logluyoruz.
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(jsonResponse);
    }
}