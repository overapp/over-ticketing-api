using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.TicketCategories;
using Application.TicketCategories.GetByProject;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Categories;

internal sealed class GetByProject : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("projects/{projectId:guid}/categories", async (
            Guid projectId,
            IQueryHandler<GetProjectTicketCategoriesQuery, IReadOnlyCollection<TicketCategoryResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProjectTicketCategoriesQuery(projectId);

            Result<IReadOnlyCollection<TicketCategoryResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Categories)
        .HasPermission(Permissions.Tickets.Read);
    }
}
