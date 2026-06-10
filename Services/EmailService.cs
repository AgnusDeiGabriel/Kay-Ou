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
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string toName, string token)
    {
        var smtpUser = _config["Email:SmtpUser"];
        var smtpPass = _config["Email:SmtpPass"];

        if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
        {
            _logger.LogWarning("SMTP pa konfigire — email verifikasyon pa voye pou {Email}", toEmail);
            return;
        }

        try
        {
            var baseUrl = _config["Email:BaseUrl"]!.TrimEnd('/');
            var link    = $"{baseUrl}/verify?token={token}";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config["Email:FromName"], _config["Email:FromAddress"]));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = "Verifye email ou — KayAyiti";
            message.Body = new TextPart("html")
            {
                Text = $"""
                        <div style="font-family:sans-serif;max-width:480px;margin:auto">
                          <h2 style="color:#2C1810">Byenveni sou <strong>KayOu</strong>!</h2>
                          <p>Bonjou <strong>{toName}</strong>,</p>
                          <p>Klike sou bouton sa a pou konfime adrès email ou:</p>
                          <a href="{link}"
                             style="display:inline-block;padding:12px 24px;background:#D4531A;
                                    color:#fff;text-decoration:none;border-radius:8px;font-weight:bold">
                            Verifye Email Mwen
                          </a>
                          <p style="margin-top:16px;color:#666;font-size:13px">
                            Oswa kopye lyen sa a:<br><a href="{link}">{link}</a>
                          </p>
                          <p style="color:#999;font-size:12px">Lyen an valid pou 24 èdtan.</p>
                        </div>
                        """
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_config["Email:SmtpHost"],
                int.Parse(_config["Email:SmtpPort"]!), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(smtpUser, smtpPass);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email verifikasyon voye bay {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Echèk voye email verifikasyon bay {Email}", toEmail);
            // Pa lanse erè — kite enskripsyon kontinye san email
        }
    }
}
