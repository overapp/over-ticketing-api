using Application.Abstractions.Messaging;

namespace Application.TicketCategories.GetGlobal;

public sealed record GetGlobalTicketCategoriesQuery(
    bool IncludeArchived = false) : IQuery<IReadOnlyCollection<TicketCategoryResponse>>;
