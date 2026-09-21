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
