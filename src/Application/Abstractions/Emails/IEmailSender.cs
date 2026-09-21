namespace Application.Abstractions.Emails;

public interface IEmailSender
{
    Task SendAsync(
        string recipient,
        string subject,
        string bodyHtml,
        CancellationToken cancellationToken = default);

    Task SendAsync(
        IEnumerable<string> recipients,
        string subject,
        string bodyHtml,
        CancellationToken cancellationToken = default);
}
