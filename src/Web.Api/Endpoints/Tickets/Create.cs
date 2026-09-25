using Application.Abstractions.Authorization;
using Application.Abstractions.Messaging;
using Application.Tickets;
using Application.Tickets.Create;
using Domain.Tickets;
using Microsoft.AspNetCore.Http;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Tickets;

internal sealed class Create : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("tickets", async (
            HttpRequest request,
            ICommandHandler<CreateTicketCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest("Expected multipart/form-data content type.");
            }

            IFormCollection form = await request.ReadFormAsync(cancellationToken);

            if (!Guid.TryParse(form["projectId"], out Guid projectId))
            {
                return Results.BadRequest("Invalid or missing projectId.");
            }

            string title = form["title"].ToString();
            string message = form["message"].ToString();

            TicketPriority priority = TicketPriority.Medium;
            if (form.ContainsKey("priority") &&
                Enum.TryParse<TicketPriority>(form["priority"].ToString(), ignoreCase: true, out TicketPriority parsedPriority))
            {
                priority = parsedPriority;
            }

            Guid? categoryId = null;
            if (form.ContainsKey("categoryId") &&
                Guid.TryParse(form["categoryId"].ToString(), out Guid parsedCategoryId))
            {
                categoryId = parsedCategoryId;
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

            var command = new CreateTicketCommand(projectId, title, message, priority, categoryId, attachments);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Tickets)
        .WithName("CreateTicket")
        .WithSummary("Open a new support ticket with an initial message and optional attachments.")
        .DisableAntiforgery()
        .HasPermission(Permissions.Tickets.Create)
        .Produces<Guid>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest, "text/plain")
        .ProducesValidationError()
        .ProducesError(StatusCodes.Status404NotFound, "Projects.NotFound", "No project exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "Projects.AlreadyArchived", "The project is archived and cannot receive new tickets.")
        .ProducesError(StatusCodes.Status400BadRequest, "Tickets.UserNotInProject", "The caller is not assigned to the specified project.")
        .ProducesError(StatusCodes.Status404NotFound, "TicketCategories.NotFound", "No ticket category exists with the specified Id.")
        .ProducesError(StatusCodes.Status400BadRequest, "TicketCategories.CategoryArchived", "The ticket category is archived and cannot be assigned.")
        .ProducesError(StatusCodes.Status400BadRequest, "TicketCategories.InvalidForProject", "The ticket category does not belong to the specified project.");
    }
}
