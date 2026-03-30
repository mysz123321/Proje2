namespace Staj2.Services.Interfaces;
public interface IMailSender
{
    Task SendAsync(string toEmail, string subject, string body);
}
