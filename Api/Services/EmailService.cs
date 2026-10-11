using System.Net;
using System.Net.Mail;
using Jabez.Api.Common;
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

        // 非正式環境標記：主旨前綴（信箱列表看到的）＋ 內文頂端橫幅（點開後看到的）。
        // 只加主旨的話，轉寄或列印出來的信就看不出是測試了。
        // 正式站未設定時兩者皆不套用，輸出與加此功能之前完全相同。
        var finalSubject = EnvironmentLabel.Prefix(subject);
        var finalBody    = EnvironmentLabel.HasValue
            ? "<div style=\"background:#B8892A;color:#FFFFFF;padding:10px 16px;font-weight:bold;"
              + "font-family:'Microsoft JhengHei',Arial,sans-serif;font-size:14px\">"
              + System.Net.WebUtility.HtmlEncode(EnvironmentLabel.Value)
              + "</div>" + htmlBody
            : htmlBody;

        var msg = new MailMessage
        {
            From       = new MailAddress(_from),
            Subject    = finalSubject,
            Body       = finalBody,
            IsBodyHtml = true,
        };
        msg.To.Add(to);

        await client.SendMailAsync(msg);
    }
}
