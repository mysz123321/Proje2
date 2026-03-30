namespace STAJ2.MailServices;

public interface IMailSender
{
    Task SendAsync(string toEmail, string subject, string body);
}
