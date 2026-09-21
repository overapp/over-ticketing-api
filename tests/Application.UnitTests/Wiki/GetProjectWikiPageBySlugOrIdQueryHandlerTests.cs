using Application.Abstractions.Authentication;
using Application.UnitTests.Abstractions;
using Application.Wiki.GetProjectWikiPageBySlugOrId;
using Domain.Projects;
using Domain.Users;
using Domain.Wiki;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Wiki;

public sealed class GetProjectWikiPageBySlugOrIdQueryHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly Guid _userId = Guid.NewGuid();

    public GetProjectWikiPageBySlugOrIdQueryHandlerTests()
    {
        _userContext.UserId.Returns(_userId);
    }

    [Fact]
    public async Task Handle_Should_ReturnPage_WhenQueriedBySlug()
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

        var page = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Overview",
            Slug = "overview",
            Content = "# Overview content",
            Status = WikiPageStatus.Published,
            IsInternalOnly = false
        };
        context.WikiPages.Add(page);
        await context.SaveChangesAsync();

        var query = new GetProjectWikiPageBySlugOrIdQuery(project.Id, "overview");
        var handler = new GetProjectWikiPageBySlugOrIdQueryHandler(context, _userContext);

        Result<ProjectWikiPageResponse> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(page.Id);
        result.Value.Title.ShouldBe("Overview");
        result.Value.Content.ShouldBe("# Overview content");
    }

    [Fact]
    public async Task Handle_Should_ReturnPage_WhenQueriedByGuid()
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

        var page = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Overview",
            Slug = "overview",
            Content = "# Overview content",
            Status = WikiPageStatus.Published,
            IsInternalOnly = false
        };
        context.WikiPages.Add(page);
        await context.SaveChangesAsync();

        var query = new GetProjectWikiPageBySlugOrIdQuery(project.Id, page.Id.ToString());
        var handler = new GetProjectWikiPageBySlugOrIdQueryHandler(context, _userContext);

        Result<ProjectWikiPageResponse> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(page.Id);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenStandardUserTriesToReadInternalPage()
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

        var page = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Internal Secrets",
            Slug = "internal-secrets",
            Content = "Secrets",
            Status = WikiPageStatus.Published,
            IsInternalOnly = true
        };
        context.WikiPages.Add(page);
        await context.SaveChangesAsync();

        var query = new GetProjectWikiPageBySlugOrIdQuery(project.Id, "internal-secrets");
        var handler = new GetProjectWikiPageBySlugOrIdQueryHandler(context, _userContext);

        Result<ProjectWikiPageResponse> result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.UnauthorizedAccess);
    }
}
