using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.TicketCategories;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Tickets.GetById;

internal sealed class GetTicketByIdQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetTicketByIdQuery, TicketDetailResponse>
{
    public async Task<Result<TicketDetailResponse>> Handle(
        GetTicketByIdQuery query,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        Ticket? ticket = await context.Tickets
            .AsNoTracking()
            .Include(t => t.Messages)
                .ThenInclude(m => m.Attachments)
            .SingleOrDefaultAsync(t => t.Id == query.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure<TicketDetailResponse>(TicketErrors.NotFound(query.TicketId));
        }

        ProjectAssignment? assignment = await context.ProjectAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(pa => pa.ProjectId == ticket.ProjectId && pa.UserId == userId, cancellationToken);

        if (assignment is null)
        {
            return Result.Failure<TicketDetailResponse>(TicketErrors.UserNotInProject(userId, ticket.ProjectId));
        }

        bool isSupportOrAdmin = string.Equals(assignment.Role, RoleNames.Support, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(assignment.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);

        // Standard user can only view their own ticket
        if (!isSupportOrAdmin && ticket.CreatedByUserId != userId)
        {
            return Result.Failure<TicketDetailResponse>(TicketErrors.UnauthorizedAccess(query.TicketId));
        }

        // Filter out internal messages for standard users
        IEnumerable<TicketMessage> messages = ticket.Messages;
        if (!isSupportOrAdmin)
        {
            messages = messages.Where(m => !m.IsInternal);
        }

        TicketCategoryResponse? categoryResponse = null;
        if (ticket.CategoryId.HasValue)
        {
            TicketCategory? category = await context.TicketCategories
                .AsNoTracking()
                .SingleOrDefaultAsync(tc => tc.Id == ticket.CategoryId.Value, cancellationToken);

            if (category is not null)
            {
                categoryResponse = new TicketCategoryResponse(
                    category.Id,
                    category.ProjectId,
                    category.Name,
                    category.Description,
                    category.BackgroundColor,
                    category.ForegroundColor,
                    category.Status);
            }
        }

        var response = new TicketDetailResponse
        {
            Id = ticket.Id,
            ProjectId = ticket.ProjectId,
            CreatedByUserId = ticket.CreatedByUserId,
            AssignedToUserId = ticket.AssignedToUserId,
            Category = categoryResponse,
            Title = ticket.Title,
            Status = ticket.Status,
            Priority = ticket.Priority,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            FirstResponseDueAt = ticket.FirstResponseDueAt,
            FirstResponseAt = ticket.FirstResponseAt,
            ResolutionDueAt = ticket.ResolutionDueAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            Messages = messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new TicketMessageResponse
                {
                    Id = m.Id,
                    AuthorUserId = m.AuthorUserId,
                    Content = m.Content,
                    IsInternal = m.IsInternal,
                    CreatedAt = m.CreatedAt,
                    Attachments = m.Attachments
                        .Select(a => new TicketAttachmentResponse
                        {
                            Id = a.Id,
                            FileName = a.FileName,
                            ContentType = a.ContentType,
                            FileSizeBytes = a.FileSizeBytes,
                            CreatedAt = a.CreatedAt
                        })
                        .ToList()
                })
                .ToList()
        };

        return response;
    }
}
