using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<SqlServerServerResource> sqlServer = builder
    .AddSqlServer("sqlserver");

IResourceBuilder<SqlServerDatabaseResource> database = sqlServer
    .AddDatabase("database");

IResourceBuilder<AzureStorageResource> storage = builder
    .AddAzureStorage("storage")
    .RunAsEmulator();

IResourceBuilder<AzureBlobStorageResource> blobs = storage
    .AddBlobs("blobs");

IResourceBuilder<AzureQueueStorageResource> queues = storage
    .AddQueues("queues");

IResourceBuilder<ContainerResource> mailpit = builder
    .AddContainer("mailpit", "axllent/mailpit")
    .WithHttpEndpoint(8025, targetPort: 8025, name: "dashboard")
    .WithEndpoint(1025, targetPort: 1025, name: "smtp");

IResourceBuilder<ParameterResource> jwtPrivateKey = builder
    .AddParameter("jwt-private-key", secret: true);

IResourceBuilder<ParameterResource> jwtPublicKey = builder
    .AddParameter("jwt-public-key", secret: true);

builder.AddProject<Projects.Web_Api>("web-api", launchProfileName: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Jwt__PrivateKey", jwtPrivateKey)
    .WithEnvironment("Jwt__PublicKey", jwtPublicKey)
    .WithEnvironment("Jwt__Issuer", "clean-architecture")
    .WithEnvironment("Jwt__Audience", "developers")
    .WithEnvironment("Jwt__ExpirationInMinutes", "60")
    .WithEnvironment("RateLimiting__Global__PermitLimit", "100000")
    .WithEnvironment("RateLimiting__Authentication__PermitLimit", "100000")
    .WithReference(database)
    .WithReference(blobs)
    .WithReference(queues)
    .WithEnvironment("Email__Smtp__Host", mailpit.GetEndpoint("smtp").Property(EndpointProperty.Host))
    .WithEnvironment("Email__Smtp__Port", mailpit.GetEndpoint("smtp").Property(EndpointProperty.Port))
    .WithHttpHealthCheck("/health")
    .WaitFor(database)
    .WaitFor(blobs)
    .WaitFor(queues);

await builder.Build().RunAsync();
