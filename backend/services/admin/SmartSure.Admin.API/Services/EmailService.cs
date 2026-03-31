using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SmartSure.Admin.Application.Interfaces;

namespace SmartSure.Admin.API.Services;

public class MailSettings
{
    public string Server { get; set; } = "";
    public int Port { get; set; } = 587;
    public string SenderName { get; set; } = "SmartSure";
    public string SenderEmail { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public bool UseSsl { get; set; } = false;
}

public class EmailService : IEmailService
{
    private readonly MailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<MailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("Sending email to {To} | Subject: {Subject}", to, subject);

        if (string.IsNullOrEmpty(_settings.UserName) || string.IsNullOrEmpty(_settings.Password))
        {
            _logger.LogWarning("SMTP not configured — skipping email to {To}", to);
            return;
        }

        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.Server, _settings.Port, _settings.UseSsl);
            await smtp.AuthenticateAsync(_settings.UserName, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }
}
