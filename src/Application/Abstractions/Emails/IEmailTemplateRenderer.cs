namespace Application.Abstractions.Emails;

public interface IEmailTemplateRenderer
{
    Task<string> RenderAsync(string templateName, object model, CancellationToken cancellationToken = default);
}
