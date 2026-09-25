using Azure.Identity;

namespace Web.Api.Extensions;

public static class KeyVaultExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as a configuration source outside the Development environment.
    /// Secret names use <c>--</c> in place of <c>:</c> (e.g. <c>Jwt--PrivateKey</c> maps to <c>Jwt:PrivateKey</c>).
    /// </summary>
    public static WebApplicationBuilder AddKeyVaultConfiguration(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            return builder;
        }

        string? vaultUri = builder.Configuration["KeyVault:Uri"];

        if (!Uri.TryCreate(vaultUri, UriKind.Absolute, out Uri? uri))
        {
            throw new InvalidOperationException(
                $"'KeyVault:Uri' must be set to the vault URI (e.g. https://<vault-name>.vault.azure.net/) " +
                $"outside the Development environment (current environment: '{builder.Environment.EnvironmentName}').");
        }

        // Managed identity in Azure; developer credentials (Azure CLI, Visual Studio) when run locally.
        // KeyVault:ManagedIdentityClientId selects a user-assigned identity; leave it empty for system-assigned.
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = builder.Configuration["KeyVault:ManagedIdentityClientId"]
        });

        builder.Configuration.AddAzureKeyVault(uri, credential);

        return builder;
    }
}
