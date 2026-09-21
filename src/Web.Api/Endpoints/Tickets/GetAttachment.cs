using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Tickets.GetAttachment;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tickets;

internal sealed class GetAttachment : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("tickets/attachments/{attachmentId:guid}", async (
            Guid attachmentId,
            IQueryHandler<GetAttachmentQuery, AttachmentDownloadResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAttachmentQuery(attachmentId);

            Result<AttachmentDownloadResponse> result = await handler.Handle(query, cancellationToken);

            if (result.IsFailure)
            {
                return CustomResults.Problem(result);
            }

            return Results.File(
                result.Value.Stream,
                result.Value.ContentType,
                result.Value.FileName);
        })
        .WithTags(Tags.Tickets)
        .HasPermission(Permissions.Tickets.Read);
    }
}
