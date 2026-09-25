using System.Security.Cryptography;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IntegrationTests;

public sealed class IntegrationTestAppHost : IAsyncLifetime
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);

    private DistributedApplication? _application;

    public HttpClient CreateHttpClient()
    {
        if (_application is null)
        {
            throw new InvalidOperationException("The Aspire test AppHost has not been initialized.");
        }

        return _application.CreateHttpClient("web-api");
    }

    public async Task<string?> GetConnectionStringAsync(string resourceName)
    {
        if (_application is null)
        {
            throw new InvalidOperationException("The Aspire test AppHost has not been initialized.");
        }

        return await _application.GetConnectionStringAsync(resourceName);
    }

    public async Task InitializeAsync()
    {
        IDistributedApplicationTestingBuilder appHost =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();

        // The testing builder does not load the AppHost's user secrets, so supply a throwaway
        // JWT key pair for the secret parameters. Tests then need no local secret setup.
        using (var rsa = RSA.Create(2048))
        {
            appHost.Configuration["Parameters:jwt-private-key"] = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
            appHost.Configuration["Parameters:jwt-public-key"] = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
        }

        appHost.Services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Debug));

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            clientBuilder.AddStandardResilienceHandler());

        _application = await appHost.BuildAsync().WaitAsync(DefaultTimeout);

        await _application.StartAsync().WaitAsync(DefaultTimeout);

        await _application.ResourceNotifications
            .WaitForResourceHealthyAsync("web-api")
            .WaitAsync(DefaultTimeout);
    }

    public async Task DisposeAsync()
    {
        if (_application is not null)
        {
            await _application.DisposeAsync();
        }
    }
}
