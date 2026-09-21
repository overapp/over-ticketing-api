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

builder.AddProject<Projects.Web_Api>("web-api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Jwt__PrivateKey", "MIIEvwIBADANBgkqhkiG9w0BAQEFAASCBKkwggSlAgEAAoIBAQDLoXyIK5Q51FJuKC+1mgR2ueUFlmgi8RRkNBclPBHfBAD+w7e8sjiw6iPnu/JJ9OVs0Bzzu5XtfyzLtma2f5fdAaHk0hw307fJizmJ9fWH3d/uhBSF6YL5hn1jPqEMUanN4Bmc3SgOsV1KwLmUdJu/hLn1qbI+huXti5tdxmiMvyUFrievFOc3fWT8r+/eZ/mJfmfuVHlD2R3ySXsvSgbcCcqefO5w6gncxWAwkBZGzxRuCz5MbpG79Xr0sLeeIM5h0myc+L+4nR0+Nk/fKkwPGR+Nnv4RVwRVcsq8wKyCt08by9oRr8sI4QnCeohAH3kjjY3liDHIL/swdk6feYT/AgMBAAECggEBAJmZn1Er5ixU/zS+tXY7zmAsSxgS40xwI1yOemMI08Yww/tocMEyglbY1uqnN9UXmLOirCQh/K7gPk8PsJy61DfOUmtDHivyVPD/RxDM6j9pWtAU61Iz2SsViqGBDYQ4WWjFQSy1GdEzcta/V30nFJC7snYfYieOJAQySajLz/NRpiz5MRM6QsuLaz7t7t9PM84s1BEZELgm+F8FkOUPvu0bT5AM0BFKJB+b7ULu3OYV4sNPy1jb5kBseYEu1oe2LQwNXoFCUiztTzA7VL+Ap7mp8FZKLskrluJ+OE6pNDu/nehwHTWbMHn21XDqSkDRxqwjAjxP/rmOOULP7ow7f+ECgYEA94tdVW0DU6Cj7Fz4PCULlHGbAf7vpSrR2a+o7vkuScL8cErV8fW5z5GFsXAfDvJegKFQYDfEU5uordcT8FssGDbBzdQHBw7hmlEUJGWhtomdPOPrIqs4wiJ41ItbtgGrDt+M5hNFi8ci3cItdawaeGyWumpQV98EYsXPgrQcHbECgYEA0pYfWHO2nvUFDKu0dhk0W/OG0L4QB/7o2GbzF/yVAl3RA7Qu3rRGePSOELmNrqc2275si1B0c+3mHzkL4otXvdttco1N3EY+1oksz9DbVel7mxLqv7kbb56V4KVQFr7r6wRygHu2XZ4e7i0YvNOZ6edekhHe0mt6Q1/zpNYWCa8CgYEAtpxum8gxfg2xH3pt/SBu7HDqIozIiJWP/QBipPfZN7zJsKTkMvxMuFznvT+zCbmuEUHvIyvAftUDmEpjgRog6zPpwEc7b++AafCJ5Ve79gaKohKYsRiSZFQ9wr2TSC7u26f6Lvfkg/rBM8o88uOlG1Uv1BD9d/UjLSIztH3dh+ECgYB8+DHp3+GEO4uflVYmr5Zu6voabnA9Dn1HzvB8T5xuJxaeyBL4fqtDpH2aV0NJXloj1cG8eyZgldF2vffbnS2Ysdslm82U5urrjcRWH2/KcCC2SmedF1C80LPI+NGqbeq7MYxxyvjSXb+lQIXpqPkx4egxbwF4heesjAiezZ2OaQKBgQCBJgxGHBXOVtrONuB5paGZWnlYiyLP84b3BrgHw54K3KUsb1wA7aRFf1CmsEheQzMTntapTtUV+4oBBQYxK72DenYy4MIm+BUC6CGu4DBVf5rO+qQ8Bjq1ukrMlyxMDkPI0+zSQfASb23IDhx+PlYWlQE+I7u51wok47DimoEvJw==")
    .WithEnvironment("Jwt__PublicKey", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAy6F8iCuUOdRSbigvtZoEdrnlBZZoIvEUZDQXJTwR3wQA/sO3vLI4sOoj57vySfTlbNAc87uV7X8sy7Zmtn+X3QGh5NIcN9O3yYs5ifX1h93f7oQUhemC+YZ9Yz6hDFGpzeAZnN0oDrFdSsC5lHSbv4S59amyPobl7YubXcZojL8lBa4nrxTnN31k/K/v3mf5iX5n7lR5Q9kd8kl7L0oG3AnKnnzucOoJ3MVgMJAWRs8Ubgs+TG6Ru/V69LC3niDOYdJsnPi/uJ0dPjZP3ypMDxkfjZ7+EVcEVXLKvMCsgrdPG8vaEa/LCOEJwnqIQB95I42N5YgxyC/7MHZOn3mE/wIDAQAB")
    .WithEnvironment("Jwt__Issuer", "clean-architecture")
    .WithEnvironment("Jwt__Audience", "developers")
    .WithEnvironment("Jwt__ExpirationInMinutes", "60")
    .WithEnvironment("RateLimiting__Global__PermitLimit", "100000")
    .WithEnvironment("RateLimiting__Authentication__PermitLimit", "100000")
    .WithReference(database)
    .WithReference(blobs)
    .WithHttpHealthCheck("/health")
    .WaitFor(database)
    .WaitFor(blobs);

await builder.Build().RunAsync();
