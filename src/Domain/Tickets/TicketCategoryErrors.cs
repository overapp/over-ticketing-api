using SharedKernel;

namespace Domain.Tickets;

public static class TicketCategoryErrors
{
    public static Error NotFound(Guid categoryId) => Error.NotFound(
        "TicketCategories.NotFound",
        $"The ticket category with the Id = '{categoryId}' was not found");

    public static Error NameNotUnique(string name) => Error.Conflict(
        "TicketCategories.NameNotUnique",
        $"A ticket category with the name '{name}' already exists in this scope");

    public static Error InvalidForProject(Guid categoryId, Guid projectId) => Error.Problem(
        "TicketCategories.InvalidForProject",
        $"The ticket category with the Id = '{categoryId}' is not available for the project with the Id = '{projectId}'");

    public static Error CategoryArchived(Guid categoryId) => Error.Problem(
        "TicketCategories.CategoryArchived",
        $"The ticket category with the Id = '{categoryId}' is archived and cannot be assigned");

    public static Error AlreadyArchived(Guid categoryId) => Error.Problem(
        "TicketCategories.AlreadyArchived",
        $"The ticket category with the Id = '{categoryId}' is already archived");

    public static Error AlreadyActive(Guid categoryId) => Error.Problem(
        "TicketCategories.AlreadyActive",
        $"The ticket category with the Id = '{categoryId}' is already active");
}
