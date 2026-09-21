using Application.Abstractions.Messaging;

namespace Application.TicketCategories.GetByProject;

public sealed record GetProjectTicketCategoriesQuery(
    Guid ProjectId) : IQuery<IReadOnlyCollection<TicketCategoryResponse>>;
