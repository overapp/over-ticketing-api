namespace Application.Abstractions.Emails;

public sealed class EmailNotificationOptions
{
    public const string SectionName = "Email";

    public string AppBaseUrl { get; set; } = "http://localhost:3000";
}
