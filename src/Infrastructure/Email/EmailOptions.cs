namespace Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Smtp";

    public string FromEmail { get; set; } = "noreply@overapp.com";

    public string FromName { get; set; } = "OverTicketing";

    public string AppBaseUrl { get; set; } = "http://localhost:3000";

    public SmtpOptions Smtp { get; set; } = new();

    public AzureCommunicationServicesOptions AzureCommunicationServices { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool EnableSsl { get; set; }
}

public sealed class AzureCommunicationServicesOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}
