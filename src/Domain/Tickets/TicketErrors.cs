using SharedKernel;

namespace Domain.Tickets;

public static class TicketErrors
{
    public static Error NotFound(Guid ticketId) => Error.NotFound(
        "Tickets.NotFound",
        $"The ticket with the Id = '{ticketId}' was not found");

    public static Error MessageNotFound(Guid messageId) => Error.NotFound(
        "Tickets.MessageNotFound",
        $"The message with the Id = '{messageId}' was not found");

    public static Error Closed(Guid ticketId) => Error.Problem(
        "Tickets.Closed",
        $"The ticket with the Id = '{ticketId}' is closed and cannot be modified");

    public static Error InvalidStatusTransition(TicketStatus currentStatus, TicketStatus newStatus) => Error.Problem(
        "Tickets.InvalidStatusTransition",
        $"Cannot transition ticket from status '{currentStatus}' to '{newStatus}'");

    public static Error AssigneeNotSupport(Guid userId) => Error.Problem(
        "Tickets.AssigneeNotSupport",
        $"The user with the Id = '{userId}' does not have the Support role in this project");

    public static Error UserNotInProject(Guid userId, Guid projectId) => Error.Problem(
        "Tickets.UserNotInProject",
        $"The user with the Id = '{userId}' is not assigned to the project with the Id = '{projectId}'");

    public static Error UnauthorizedAccess(Guid ticketId) => Error.Problem(
        "Tickets.UnauthorizedAccess",
        $"You do not have access to the ticket with the Id = '{ticketId}'");
}
