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
        .WithName("GetTicketAttachment")
        .WithSummary("Download a ticket message attachment.")
        .HasPermission(Permissions.Tickets.Read)
        .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
        .ProducesError(StatusCodes.Status404NotFound, "Attachments.NotFound", "No attachment exists with the specified Id.")
        .ProducesError(StatusCodes.Status404NotFound, "Tickets.MessageNotFound", "The message the attachment belongs to was not found.")
        .ProducesError(StatusCodes.Status404NotFound, "Tickets.NotFound", "The ticket the attachment belongs to was not found.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.UserNotInProject", "The caller is not assigned to the ticket's project.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.UnauthorizedAccess", "The caller does not have access to this ticket or message.")
        .ProducesError(StatusCodes.Status404NotFound, "Attachments.FileNotFound", "The attachment file was not found on storage.");
    }
}
