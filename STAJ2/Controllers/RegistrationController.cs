using Microsoft.AspNetCore.Mvc;
using STAJ2.MailServices;
using Staj2.Services.Interfaces;
using Staj2.Services.Models;

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

        // 1) Hata Kontrolü (Artık tüm hataları IsSuccess üzerinden yakalayıp BadRequest dönüyoruz)
        if (!result.IsSuccess)
            return BadRequest(result.Message);

        // 2) Mail Gönderimi (Veriler artık result.Data içerisinde taşınıyor)
        try
        {
            await _mail.SendAsync(
                result.Data.Email,
                "Kayıt İsteğiniz Alındı",
                $"Merhaba {result.Data.Username},\n\nKayıt isteğiniz alındı. Yönetici onayından sonra bilgilendirileceksiniz."
            );
        }
        catch (Exception ex)
        {
            // Olası mail hatalarını kırmızı yazdıralım ki gözümüzden kaçmasın
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nMAIL GÖNDERİM HATASI: {ex.Message}\n");
            Console.ResetColor();
        }

        // Başarılı dönüşte Data.RequestId kullanıyoruz
        return Ok(new { id = result.Data.RequestId, message = "Kayıt isteği alındı. Admin onayı bekleniyor." });
    }
}