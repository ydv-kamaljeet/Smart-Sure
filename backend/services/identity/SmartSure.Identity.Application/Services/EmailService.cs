using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SmartSure.Identity.Application.DTOs;
using SmartSure.Identity.Application.Interfaces;

namespace SmartSure.Identity.Application.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly MailSettings _mailSettings;

    public EmailService(ILogger<EmailService> logger, IOptions<MailSettings> mailSettings)
    {
        _logger = logger;
        _mailSettings = mailSettings.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // Still log for fallback in case SMTP fails or isn't configured yet
        _logger.LogInformation("Attempting to send real email to {To}. Subject: {Subject}\nBody: {Body}", to, subject, body);

        // If settings are default/placeholder, skip real sending but stay successful (for dev)
        if (_mailSettings.UserName == "YOUR_MAILTRAP_USERNAME" || string.IsNullOrEmpty(_mailSettings.Password))
        {
            _logger.LogWarning("SMTP credentials not configured. Skipping real email send. Check terminal above for the link.");
            return;
        }

        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_mailSettings.SenderName, _mailSettings.SenderEmail));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_mailSettings.Server, _mailSettings.Port, _mailSettings.UseSsl);
            await smtp.AuthenticateAsync(_mailSettings.UserName, _mailSettings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            // We don't throw here to avoid breaking the registration flow in dev, 
            // but in prod you might want to handle this differently.
        }
    }
}
