using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace Jabez.Api.Services;

public class EmailService : IEmailService
{
    private readonly string _host;
    private readonly int    _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _from;
    private readonly bool   _enableSsl;

    public EmailService(IConfiguration config)
    {
        _host      = config["Smtp:Host"]     ?? throw new InvalidOperationException("Smtp:Host is required.");
        _port      = int.TryParse(config["Smtp:Port"], out var p) ? p : 587;
        _username  = config["Smtp:Username"] ?? "";
        _password  = config["Smtp:Password"] ?? "";
        _from      = config["Smtp:From"]     ?? _username;
        _enableSsl = config["Smtp:EnableSsl"]?.ToLower() != "false";
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        using var client = new SmtpClient(_host, _port)
        {
            Credentials = new NetworkCredential(_username, _password),
            EnableSsl   = _enableSsl,
        };

        var msg = new MailMessage
        {
            From       = new MailAddress(_from),
            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true,
        };
        msg.To.Add(to);

        await client.SendMailAsync(msg);
    }
}
