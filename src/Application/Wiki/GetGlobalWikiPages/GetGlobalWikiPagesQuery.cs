using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Wiki;

namespace Application.Wiki.GetGlobalWikiPages;

public sealed record GetGlobalWikiPagesQuery(
    int Page,
    int PageSize,
    string? SearchTerm,
    WikiPageStatus? Status,
    Guid? ParentPageId) : IQuery<PagedResponse<GlobalWikiPageSummaryResponse>>;
