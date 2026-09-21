using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Tickets;
using Application.Tickets.Reply;
using Microsoft.AspNetCore.Http;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tickets;

internal sealed class Reply : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("tickets/{ticketId:guid}/messages", async (
            Guid ticketId,
            HttpRequest request,
            ICommandHandler<ReplyTicketCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest("Expected multipart/form-data content type.");
            }

            IFormCollection form = await request.ReadFormAsync(cancellationToken);

            string content = form["content"].ToString();
            bool isInternal = false;
            if (form.ContainsKey("isInternal") && bool.TryParse(form["isInternal"].ToString(), out bool parsedIsInternal))
            {
                isInternal = parsedIsInternal;
            }

            List<FileUploadModel> attachments = [];
            foreach (IFormFile file in form.Files)
            {
                attachments.Add(new FileUploadModel(
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    file.OpenReadStream()));
            }

            var command = new ReplyTicketCommand(ticketId, content, isInternal, attachments);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Tickets)
        .DisableAntiforgery()
        .HasPermission(Permissions.Tickets.Reply);
    }
}
