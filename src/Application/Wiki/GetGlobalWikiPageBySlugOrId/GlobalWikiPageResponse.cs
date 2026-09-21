namespace Application.Wiki.GetGlobalWikiPageBySlugOrId;

public sealed class GlobalWikiPageResponse
{
    public Guid Id { get; set; }
    public Guid? ParentPageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsInternalOnly { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
