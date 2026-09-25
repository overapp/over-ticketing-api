namespace Infrastructure.Database;

/// <summary>
/// Exists only so the database connection string is validated at startup alongside the other required settings.
/// </summary>
internal sealed class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}
