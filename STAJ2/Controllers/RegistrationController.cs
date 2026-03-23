using Microsoft.AspNetCore.Mvc;
using STAJ2.MailServices;
using Staj2.Services.Interfaces;
using Staj2.Services.Models; // Yeni modeli buradan tanıyacak

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly IMailSender _mail;

    public RegistrationController(IRegistrationService registrationService, IMailSender mail)
    {
        _registrationService = registrationService;
        _mail = mail;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRegistrationRequest request)
    {
        var result = await _registrationService.CreateRegistrationAsync(request);

        // 1) Hata Kontrolleri
        if (result.IsBadRequest)
            return BadRequest(result.ErrorMessage);

        if (result.IsConflict)
            return Conflict(result.ErrorMessage);

        // 2) Mail Gönderimi (Veritabanı kaydı serviste başarıyla bittiği için maili fırlatıyoruz)
        try
        {
            await _mail.SendAsync(
                result.Email!,
                "Kayıt İsteğiniz Alındı",
                $"Merhaba {result.Username},\n\nKayıt isteğiniz alındı. Yönetici onayından sonra bilgilendirileceksiniz."
            );
        }
        catch (Exception ex)
        {
            // Olası mail hatalarını kırmızı yazdıralım ki gözümüzden kaçmasın
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nMAIL GÖNDERİM HATASI: {ex.Message}\n");
            Console.ResetColor();
        }

        return Ok(new { id = result.RequestId, message = "Kayıt isteği alındı. Admin onayı bekleniyor." });
    }
}