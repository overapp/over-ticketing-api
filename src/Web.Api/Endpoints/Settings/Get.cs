using Application.Abstractions.Messaging;
using Application.Settings.Get;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Settings;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("settings", async (
            IQueryHandler<GetUserSettingsQuery, UserSettingsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserSettingsQuery();

            Result<UserSettingsResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Settings)
        .WithName("GetUserSettings")
        .WithSummary("Retrieve the authenticated user's notification settings.")
        .RequireAuthorization()
        .Produces<UserSettingsResponse>(StatusCodes.Status200OK)
        .ProducesError(StatusCodes.Status404NotFound, "Users.NotFound", "No user exists for the current caller.");
    }
}
