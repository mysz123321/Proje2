namespace STAJ2.Services;

public class ConsoleMailSender : IMailSender
{
    public Task SendAsync(string toEmail, string subject, string body)
    {
        Console.WriteLine("=== MAIL ===");
        Console.WriteLine($"To: {toEmail}");
        Console.WriteLine($"Subject: {subject}");
        Console.WriteLine(body);
        Console.WriteLine("============");
        return Task.CompletedTask;
    }
}
