using InvoiceSystem.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace InvoiceSystem.Infrastructure.Email;

public class MailtrapEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public MailtrapEmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var host = _config["Mailtrap:Host"];
        var port = int.Parse(_config["Mailtrap:Port"] ?? "2525");
        var user = _config["Mailtrap:UserName"];
        var pass = _config["Mailtrap:Password"];
        var from = _config["Mailtrap:From"] ?? "no-reply@example.com";

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true
        };

        var message = new MailMessage(from, to, subject, body)
        {
            IsBodyHtml = false
        };

        await client.SendMailAsync(message);
    }
}

