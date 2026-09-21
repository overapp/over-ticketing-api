using Application.Abstractions.Authentication;
using Application.UnitTests.Abstractions;
using Application.Wiki.GetGlobalWikiPageBySlugOrId;
using Domain.Users;
using Domain.Wiki;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace Application.UnitTests.Wiki;

public sealed class GetGlobalWikiPageBySlugOrIdQueryHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly Guid _userId = Guid.NewGuid();

    public GetGlobalWikiPageBySlugOrIdQueryHandlerTests()
    {
        _userContext.UserId.Returns(_userId);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenPageDoesNotExist_ById()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new GetGlobalWikiPageBySlugOrIdQueryHandler(context, _userContext);
        var pageId = Guid.NewGuid();
        var query = new GetGlobalWikiPageBySlugOrIdQuery(pageId.ToString());

        // Act
        Result<GlobalWikiPageResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.NotFound(pageId));
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenPageDoesNotExist_BySlug()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new GetGlobalWikiPageBySlugOrIdQueryHandler(context, _userContext);
        var query = new GetGlobalWikiPageBySlugOrIdQuery("non-existent-slug");

        // Act
        Result<GlobalWikiPageResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.NotFoundBySlug("non-existent-slug"));
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenStandardUserAndPageIsDraft()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var page = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Title = "Draft Page",
            Slug = "draft-page",
            Content = "Draft content",
            Status = WikiPageStatus.Draft,
            IsInternalOnly = false,
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow
        };
        context.WikiPages.Add(page);
        await context.SaveChangesAsync();

        var handler = new GetGlobalWikiPageBySlugOrIdQueryHandler(context, _userContext);
        var query = new GetGlobalWikiPageBySlugOrIdQuery(page.Slug);

        // Act
        Result<GlobalWikiPageResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.UnauthorizedAccess);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenStandardUserAndPageIsInternalOnly()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var page = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Title = "Internal Page",
            Slug = "internal-page",
            Content = "Internal content",
            Status = WikiPageStatus.Published,
            IsInternalOnly = true,
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow
        };
        context.WikiPages.Add(page);
        await context.SaveChangesAsync();

        var handler = new GetGlobalWikiPageBySlugOrIdQueryHandler(context, _userContext);
        var query = new GetGlobalWikiPageBySlugOrIdQuery(page.Slug);

        // Act
        Result<GlobalWikiPageResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.UnauthorizedAccess);
    }

    [Fact]
    public async Task Handle_Should_ReturnPage_WhenStandardUserAndPageIsPublishedAndNotInternal_BySlug()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var page = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Title = "Public Page",
            Slug = "public-page",
            Content = "Public content",
            Status = WikiPageStatus.Published,
            IsInternalOnly = false,
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow
        };
        context.WikiPages.Add(page);
        await context.SaveChangesAsync();

        var handler = new GetGlobalWikiPageBySlugOrIdQueryHandler(context, _userContext);
        var query = new GetGlobalWikiPageBySlugOrIdQuery(page.Slug);

        // Act
        Result<GlobalWikiPageResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Slug.ShouldBe("public-page");
        result.Value.Title.ShouldBe("Public Page");
    }

    [Fact]
    public async Task Handle_Should_ReturnPage_WhenStandardUserAndPageIsPublishedAndNotInternal_ById()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var page = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Title = "Public Page",
            Slug = "public-page-2",
            Content = "Public content",
            Status = WikiPageStatus.Published,
            IsInternalOnly = false,
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow
        };
        context.WikiPages.Add(page);
        await context.SaveChangesAsync();

        var handler = new GetGlobalWikiPageBySlugOrIdQueryHandler(context, _userContext);
        var query = new GetGlobalWikiPageBySlugOrIdQuery(page.Id.ToString());

        // Act
        Result<GlobalWikiPageResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(page.Id);
    }

    [Fact]
    public async Task Handle_Should_ReturnPage_WhenAdminOrSupport_EvenIfDraftOrInternalOnly()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var adminRole = new Role { Id = Guid.NewGuid(), Name = RoleNames.Admin, NormalizedName = RoleNames.Admin.ToUpperInvariant() };
        context.Roles.Add(adminRole);
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = _userId, RoleId = adminRole.Id });

        var page = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Title = "Admin Page",
            Slug = "admin-page",
            Content = "Admin secret content",
            Status = WikiPageStatus.Draft,
            IsInternalOnly = true,
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow
        };
        context.WikiPages.Add(page);
        await context.SaveChangesAsync();

        var handler = new GetGlobalWikiPageBySlugOrIdQueryHandler(context, _userContext);
        var query = new GetGlobalWikiPageBySlugOrIdQuery(page.Slug);

        // Act
        Result<GlobalWikiPageResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Slug.ShouldBe("admin-page");
        result.Value.IsInternalOnly.ShouldBeTrue();
    }
}
