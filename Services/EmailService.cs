using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace KayAyiti.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string toName, string token);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config) => _config = config;

    public async Task SendVerificationEmailAsync(string toEmail, string toName, string token)
    {
        var baseUrl = _config["Email:BaseUrl"]!.TrimEnd('/');
        var link = $"{baseUrl}/verify?token={token}";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_config["Email:FromName"], _config["Email:FromAddress"]));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "Verifye email ou — KayAyiti";
        message.Body = new TextPart("html")
        {
            Text = $"""
                    <div style="font-family:sans-serif;max-width:480px;margin:auto">
                      <h2 style="color:#1a1a2e">Byenveni sou <strong>KayAyiti</strong>!</h2>
                      <p>Bonjou <strong>{toName}</strong>,</p>
                      <p>Klike sou bouton sa a pou konfime adrès email ou:</p>
                      <a href="{link}"
                         style="display:inline-block;padding:12px 24px;background:#e94560;
                                color:#fff;text-decoration:none;border-radius:6px;font-weight:bold">
                        Verifye Email Mwen
                      </a>
                      <p style="margin-top:16px;color:#666;font-size:13px">
                        Oswa kopye lyen sa a nan navigatè ou:<br>
                        <a href="{link}">{link}</a>
                      </p>
                      <p style="color:#999;font-size:12px">Lyen an valid pou 24 èdtan.</p>
                    </div>
                    """
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(
            _config["Email:SmtpHost"],
            int.Parse(_config["Email:SmtpPort"]!),
            SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_config["Email:SmtpUser"], _config["Email:SmtpPass"]);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}
