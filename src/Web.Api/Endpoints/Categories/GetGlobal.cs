using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.TicketCategories;
using Application.TicketCategories.GetGlobal;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Categories;

internal sealed class GetGlobal : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("categories", async (
            bool includeArchived,
            IQueryHandler<GetGlobalTicketCategoriesQuery, IReadOnlyCollection<TicketCategoryResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetGlobalTicketCategoriesQuery(includeArchived);

            Result<IReadOnlyCollection<TicketCategoryResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Categories)
        .WithName("GetGlobalTicketCategories")
        .WithSummary("List the ticket categories available to all projects.")
        .HasPermission(Permissions.Categories.Manage)
        .Produces<IReadOnlyCollection<TicketCategoryResponse>>(StatusCodes.Status200OK);
    }
}
