using Application.Abstractions.Authentication;
using Application.Common;
using Application.UnitTests.Abstractions;
using Application.Wiki.GetProjectWikiPages;
using Domain.Projects;
using Domain.Users;
using Domain.Wiki;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Wiki;

public sealed class GetProjectWikiPagesQueryHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly Guid _userId = Guid.NewGuid();

    public GetProjectWikiPagesQueryHandlerTests()
    {
        _userContext.UserId.Returns(_userId);
    }

    [Fact]
    public async Task Handle_Should_ReturnProjectNotFound_WhenProjectDoesNotExist()
    {
        await using TestDbContext context = CreateDbContext();
        var query = new GetProjectWikiPagesQuery(Guid.NewGuid(), 1, 10, null, null, null);
        var handler = new GetProjectWikiPagesQueryHandler(context, _userContext);

        Result<PagedResponse<WikiPageSummaryResponse>> result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProjectErrors.NotFound(query.ProjectId));
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserIsNotAssignedToProjectAndNotAdmin()
    {
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "P1" };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var query = new GetProjectWikiPagesQuery(project.Id, 1, 10, null, null, null);
        var handler = new GetProjectWikiPagesQueryHandler(context, _userContext);

        Result<PagedResponse<WikiPageSummaryResponse>> result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.UnauthorizedAccess);
    }

    [Fact]
    public async Task Handle_Should_FilterDraftsAndInternalPages_ForStandardUser()
    {
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "P1" };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.User
        });

        // 1. Published and public (visible)
        context.WikiPages.Add(new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Public Guide",
            Slug = "public-guide",
            Status = WikiPageStatus.Published,
            IsInternalOnly = false
        });

        // 2. Draft (not visible to User)
        context.WikiPages.Add(new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Draft Guide",
            Slug = "draft-guide",
            Status = WikiPageStatus.Draft,
            IsInternalOnly = false
        });

        // 3. Internal only (not visible to User)
        context.WikiPages.Add(new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Internal Guide",
            Slug = "internal-guide",
            Status = WikiPageStatus.Published,
            IsInternalOnly = true
        });

        await context.SaveChangesAsync();

        var query = new GetProjectWikiPagesQuery(project.Id, 1, 10, null, null, null);
        var handler = new GetProjectWikiPagesQueryHandler(context, _userContext);

        Result<PagedResponse<WikiPageSummaryResponse>> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.Single().Title.ShouldBe("Public Guide");
    }

    [Fact]
    public async Task Handle_Should_ReturnAllPages_ForSupportRole()
    {
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "P1" };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.Support
        });

        context.WikiPages.Add(new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Public Guide",
            Slug = "public-guide",
            Status = WikiPageStatus.Published,
            IsInternalOnly = false
        });

        context.WikiPages.Add(new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Internal Guide",
            Slug = "internal-guide",
            Status = WikiPageStatus.Published,
            IsInternalOnly = true
        });

        context.WikiPages.Add(new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Draft Guide",
            Slug = "draft-guide",
            Status = WikiPageStatus.Draft,
            IsInternalOnly = false
        });

        await context.SaveChangesAsync();

        var query = new GetProjectWikiPagesQuery(project.Id, 1, 10, null, null, null);
        var handler = new GetProjectWikiPagesQueryHandler(context, _userContext);

        Result<PagedResponse<WikiPageSummaryResponse>> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(3);
    }
}
