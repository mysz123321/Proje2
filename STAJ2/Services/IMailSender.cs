namespace STAJ2.Services;

public interface IMailSender
{
    Task SendAsync(string toEmail, string subject, string body);
}
