using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Users;
using Application.Users.Get;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Get : IEndpoint
{
    public sealed record GetUsersRequest(int Page = 1, int PageSize = 10, string? SearchTerm = null, string? Role = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users", async (
            [AsParameters] GetUsersRequest request,
            IQueryHandler<GetUsersQuery, PagedResponse<UserResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUsersQuery(request.Page, request.PageSize, request.SearchTerm, request.Role);

            Result<PagedResponse<UserResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .HasPermission(Permissions.Users.Read)
        .WithTags(Tags.Users)
        .WithName("GetUsers")
        .WithSummary("List users, with optional search and role filters.")
        .Produces<PagedResponse<UserResponse>>(StatusCodes.Status200OK);
    }
}
