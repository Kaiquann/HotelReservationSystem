using MailKit.Net.Smtp;
using MimeKit;

namespace HotelReservationSystem.Services;

/*
 * @author: Chong Jun Xiang
 * @description:
 * This service class is used to handle all the mail related services.
 */
public class Smtp
{
    public string From { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
}

public class Mail
{
    public string[] To { get; set; } = [];
    public string[]? Cc { get; set; }
    public string[]? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public interface IMailerService
{
    Task<bool> SendEmail(Mail mail);
}

public class MailerService : IMailerService
{
    private readonly Smtp _smtp;

    public MailerService(Smtp smtp)
    {
        _smtp = smtp;
    }

    public async Task<bool> SendEmail(Mail mail)
    {
        // Email is optional in environments where SMTP credentials have not been configured.
        // Returning immediately prevents login and other requests from waiting on an
        // SMTP connection that cannot authenticate.
        if (string.IsNullOrWhiteSpace(_smtp.Host) ||
            string.IsNullOrWhiteSpace(_smtp.From) ||
            string.IsNullOrWhiteSpace(_smtp.Username) ||
            string.IsNullOrWhiteSpace(_smtp.Password))
        {
            return false;
        }

        MimeMessage message = new();

        // From
        message.From.Add(new MailboxAddress(_smtp.Name, _smtp.From));

        // To
        InternetAddressList toList = [];
        foreach (string to in mail.To) toList.Add(MailboxAddress.Parse(to));
        message.To.AddRange(toList);

        // Cc
        InternetAddressList ccList = [];
        if (mail.Cc != null)
            foreach (string cc in mail.Cc)
                ccList.Add(MailboxAddress.Parse(cc));

        message.Cc.AddRange(ccList);

        // Bcc
        InternetAddressList bccList = [];
        if (mail.Bcc != null)
            foreach (string bcc in mail.Bcc)
                bccList.Add(MailboxAddress.Parse(bcc));

        message.Bcc.AddRange(bccList);

        // Subject
        message.Subject = mail.Subject;

        // Body
        message.Body = new TextPart("html")
        {
            Text = mail.Body
        };

        try
        {
            using SmtpClient client = new();
            await client.ConnectAsync(_smtp.Host, _smtp.Port, false);
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }
}
