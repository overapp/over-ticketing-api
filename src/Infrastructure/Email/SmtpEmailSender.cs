using Application.Abstractions.Emails;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Email;

internal sealed class SmtpEmailSender(
    IOptions<EmailOptions> emailOptions,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string recipient, string subject, string bodyHtml, CancellationToken cancellationToken = default) =>
        SendAsync([recipient], subject, bodyHtml, cancellationToken);

    public async Task SendAsync(IEnumerable<string> recipients, string subject, string bodyHtml, CancellationToken cancellationToken = default)
    {
        EmailOptions options = emailOptions.Value;
        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(options.FromName, options.FromEmail));

        foreach (string recipient in recipients)
        {
            if (!string.IsNullOrWhiteSpace(recipient))
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }
        }

        if (message.To.Count == 0)
        {
            logger.LogWarning("No recipients specified for email '{Subject}'. Skipping send.", subject);
            return;
        }

        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = bodyHtml
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            SecureSocketOptions socketOptions = options.Smtp.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(
                options.Smtp.Host,
                options.Smtp.Port,
                socketOptions,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(options.Smtp.Username))
            {
                await client.AuthenticateAsync(options.Smtp.Username, options.Smtp.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email '{Subject}' via SMTP to {Host}:{Port}.", subject, options.Smtp.Host, options.Smtp.Port);
            throw new InvalidOperationException($"Failed to send email '{subject}' via SMTP to {options.Smtp.Host}:{options.Smtp.Port}.", ex);
        }
    }
}
