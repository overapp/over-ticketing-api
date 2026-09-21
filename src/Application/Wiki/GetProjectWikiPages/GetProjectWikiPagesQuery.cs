using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Wiki;

namespace Application.Wiki.GetProjectWikiPages;

public sealed record GetProjectWikiPagesQuery(
    Guid ProjectId,
    int Page,
    int PageSize,
    string? SearchTerm,
    WikiPageStatus? Status,
    Guid? ParentPageId) : IQuery<PagedResponse<WikiPageSummaryResponse>>;
