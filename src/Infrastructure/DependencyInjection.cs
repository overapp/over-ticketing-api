using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Emails;
using Application.Abstractions.Notifications;
using Application.Abstractions.Storage;
using Domain.Users;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Infrastructure.Database;
using Infrastructure.DomainEvents;
using Infrastructure.Email;
using Infrastructure.Notifications;
using Infrastructure.Outbox;
using Infrastructure.Queues;
using Infrastructure.Storage;
using Infrastructure.Time;
using Infrastructure.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedKernel;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddServices()
            .AddDatabase(configuration)
            .AddStorage(configuration)
            .AddQueues(configuration)
            .AddEmail(configuration)
            .AddHealthChecks(configuration)
            .AddIdentityInternal()
            .AddAuthenticationInternal(configuration)
            .AddAuthorizationInternal();

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();
        services.AddTransient<INotificationPublisher, NotificationPublisher>();
        services.AddTransient<IDomainEventToNotificationMapper, TicketCreatedNotificationMapper>();
        services.AddTransient<IDomainEventToNotificationMapper, TicketReplyNotificationMapper>();
        services.AddTransient<IDomainEventToNotificationMapper, UserCreatedNotificationMapper>();
        services.AddTransient<IDomainEventToNotificationMapper, ProjectAssignmentNotificationMapper>();
        services.AddTransient<IDomainEventToNotificationMapper, UserPasswordResetRequestedNotificationMapper>();
        services.AddTransient<IDomainEventToNotificationMapper, UserTemporaryPasswordAssignedNotificationMapper>();
        services.AddTransient<IDomainEventToNotificationMapper, UserPasswordChangedNotificationMapper>();
        services.AddTransient<IDomainEventHandler<UserDeletedDomainEvent>, UserDeletedEventHandler>();

#pragma warning disable EXTEXP0018 // HybridCache is released; the API is stable in .NET 10.
        services.AddHybridCache();
#pragma warning restore EXTEXP0018

        return services;
    }

    private static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AzureBlobStorageOptions>()
            .Configure(options =>
            {
                configuration.GetSection(AzureBlobStorageOptions.SectionName).Bind(options);

                string? connectionString =
                    configuration.GetConnectionString("blobs") ??
                    configuration.GetConnectionString("storage") ??
                    configuration.GetConnectionString("BlobStorage");

                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    options.ConnectionString = connectionString;
                }
            })
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Blob storage connection string is required: set 'ConnectionStrings:BlobStorage'.")
            .ValidateOnStart();

        services.AddTransient<IFileStorageService, AzureBlobStorageService>();

        return services;
    }

    private static IServiceCollection AddQueues(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AzureQueueStorageOptions>()
            .Configure(options =>
            {
                configuration.GetSection(AzureQueueStorageOptions.SectionName).Bind(options);

                string? connectionString =
                    configuration.GetConnectionString("queues") ??
                    configuration.GetConnectionString("storage") ??
                    configuration.GetConnectionString("QueueStorage");

                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    options.ConnectionString = connectionString;
                }
            })
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Queue storage connection string is required: set 'ConnectionStrings:QueueStorage'.")
            .ValidateOnStart();

        services.AddHostedService<OutboxPublisherBackgroundService>();
        services.AddHostedService<AzureQueueConsumerBackgroundService>();

        return services;
    }

    private static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Validate(
                options => !IsAzureCommunicationServices(options.Provider) ||
                    !string.IsNullOrWhiteSpace(options.AzureCommunicationServices.ConnectionString),
                "'Email:AzureCommunicationServices:ConnectionString' is required when 'Email:Provider' is 'AzureCommunicationServices'.")
            .ValidateOnStart();
        services.Configure<EmailNotificationOptions>(configuration.GetSection(EmailNotificationOptions.SectionName));

        services.AddTransient<IEmailTemplateRenderer, FluidEmailTemplateRenderer>();

        string emailProvider = configuration.GetValue<string>("Email:Provider") ?? "Smtp";
        if (IsAzureCommunicationServices(emailProvider))
        {
            services.AddTransient<IEmailSender, AzureCommunicationServicesEmailSender>();
        }
        else
        {
            services.AddTransient<IEmailSender, SmtpEmailSender>();
        }

        return services;
    }

    private static bool IsAzureCommunicationServices(string? provider) =>
        string.Equals(provider, "AzureCommunicationServices", StringComparison.OrdinalIgnoreCase);

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Database");

        services.AddOptions<DatabaseOptions>()
            .Configure(options => options.ConnectionString = connectionString ?? string.Empty)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Database connection string is required: set 'ConnectionStrings:Database'.")
            .ValidateOnStart();

        services.AddDbContext<ApplicationDbContext>(
            options => options
                .UseSqlServer(connectionString, sqlServerOptions =>
                    sqlServerOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddSqlServer(configuration.GetConnectionString("Database")!);

        return services;
    }

    private static IServiceCollection AddIdentityInternal(this IServiceCollection services)
    {
        services
            .AddIdentityCore<User>(options =>
            {
                // Password options use the ASP.NET Core Identity defaults.
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(1);
        });

        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddSingleton<JwtSigningKeys>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, JwtSigningKeys>((o, jwtOptions, signingKeys) =>
            {
                o.RequireHttpsMetadata = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = signingKeys.ValidationKey,
                    ValidIssuer = jwtOptions.Value.Issuer,
                    ValidAudience = jwtOptions.Value.Audience,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddSingleton<ITokenProvider, TokenProvider>();
        services.AddSingleton<IPasswordGenerator, CryptographicPasswordGenerator>();

        return services;
    }

    private static IServiceCollection AddAuthorizationInternal(this IServiceCollection services)
    {
        services.AddAuthorization();

        services.AddScoped<PermissionProvider>();

        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        return services;
    }
}
