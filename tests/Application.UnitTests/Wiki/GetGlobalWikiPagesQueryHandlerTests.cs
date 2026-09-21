using Application.Abstractions.Authentication;
using Application.Common;
using Application.UnitTests.Abstractions;
using Application.Wiki.GetGlobalWikiPages;
using Domain.Users;
using Domain.Wiki;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Wiki;

public sealed class GetGlobalWikiPagesQueryHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly Guid _userId = Guid.NewGuid();

    public GetGlobalWikiPagesQueryHandlerTests()
    {
        _userContext.UserId.Returns(_userId);
    }

    [Fact]
    public async Task Handle_Should_FilterDraftsAndInternalPages_ForStandardUser()
    {
        await using TestDbContext context = CreateDbContext();

        // 1. Published and public (visible)
        context.WikiPages.Add(new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Title = "Public Global Guide",
            Slug = "public-global-guide",
            Status = WikiPageStatus.Published,
            IsInternalOnly = false
        });

        // 2. Draft (not visible)
        context.WikiPages.Add(new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Title = "Draft Global Guide",
            Slug = "draft-global-guide",
            Status = WikiPageStatus.Draft,
            IsInternalOnly = false
        });

        // 3. Internal only (not visible)
        context.WikiPages.Add(new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Title = "Internal Global Guide",
            Slug = "internal-global-guide",
            Status = WikiPageStatus.Published,
            IsInternalOnly = true
        });

        // 4. Project-specific page (must NOT be returned in global query)
        context.WikiPages.Add(new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Title = "Project Guide",
            Slug = "project-guide",
            Status = WikiPageStatus.Published,
            IsInternalOnly = false
        });

        await context.SaveChangesAsync();

        var query = new GetGlobalWikiPagesQuery(1, 10, null, null, null);
        var handler = new GetGlobalWikiPagesQueryHandler(context, _userContext);

        Result<PagedResponse<GlobalWikiPageSummaryResponse>> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.Single().Title.ShouldBe("Public Global Guide");
    }
}
