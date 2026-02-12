using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace STAJ2.Services;

public class MailKitMailSender : IMailSender
{
    private readonly IConfiguration _config;

    public MailKitMailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        var s = _config.GetSection("Smtp");
        var host = s["Host"]!;
        var port = int.Parse(s["Port"]!);
        var user = s["User"]!;
        var pass = s["Pass"]!;
        var fromEmail = s["FromEmail"] ?? user;
        var fromName = s["FromName"] ?? "STAJ2";
        var useStartTls = bool.Parse(s["UseStartTls"] ?? "true");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        message.Body = new TextPart("plain")
        {
            Text = body
        };

        using var client = new SmtpClient();
        var options = useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

        await client.ConnectAsync(host, port, options);
        await client.AuthenticateAsync(user, pass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
