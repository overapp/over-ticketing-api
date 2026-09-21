using Application.Abstractions.Emails;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Email;

internal sealed class AzureCommunicationServicesEmailSender(
    IOptions<EmailOptions> emailOptions,
    ILogger<AzureCommunicationServicesEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string recipient, string subject, string bodyHtml, CancellationToken cancellationToken = default) =>
        SendAsync([recipient], subject, bodyHtml, cancellationToken);

    public async Task SendAsync(IEnumerable<string> recipients, string subject, string bodyHtml, CancellationToken cancellationToken = default)
    {
        EmailOptions options = emailOptions.Value;

        if (string.IsNullOrWhiteSpace(options.AzureCommunicationServices.ConnectionString))
        {
            throw new InvalidOperationException("Azure Communication Services connection string is not configured.");
        }

        var client = new EmailClient(options.AzureCommunicationServices.ConnectionString);
        var emailRecipients = new List<EmailAddress>();

        foreach (string recipient in recipients)
        {
            if (!string.IsNullOrWhiteSpace(recipient))
            {
                emailRecipients.Add(new EmailAddress(recipient));
            }
        }

        if (emailRecipients.Count == 0)
        {
            logger.LogWarning("No recipients specified for email '{Subject}'. Skipping send.", subject);
            return;
        }

        var emailContent = new EmailContent(subject)
        {
            Html = bodyHtml
        };

        var emailMessage = new Azure.Communication.Email.EmailMessage(
            senderAddress: options.FromEmail,
            recipients: new EmailRecipients(emailRecipients),
            content: emailContent);

        try
        {
            await client.SendAsync(WaitUntil.Started, emailMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email '{Subject}' via Azure Communication Services.", subject);
            throw new InvalidOperationException($"Failed to send email '{subject}' via Azure Communication Services.", ex);
        }
    }
}
