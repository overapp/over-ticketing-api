using OpenTelemetry.Trace;

namespace Web.Api.Extensions;

internal static class OpenTelemetryExtensions
{
    internal static IServiceCollection AddObservability(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSqlClientInstrumentation());

        return services;
    }
}
