using Application.Abstractions.Authentication;
using Application.Common;
using Application.UnitTests.Abstractions;
using Application.Wiki.UpdateProjectWikiPage;
using Domain.Projects;
using Domain.Users;
using Domain.Wiki;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Wiki;

public sealed class UpdateProjectWikiPageCommandHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 9, 21, 14, 0, 0, DateTimeKind.Utc);

    public UpdateProjectWikiPageCommandHandlerTests()
    {
        _userContext.UserId.Returns(_userId);
        _dateTimeProvider.UtcNow.Returns(_utcNow);
    }

    [Fact]
    public async Task Handle_Should_ReturnCircularHierarchy_WhenParentPageIsSelf()
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

        var page = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Title",
            Slug = "title",
            Content = "Content",
            Status = WikiPageStatus.Published
        };
        context.WikiPages.Add(page);
        await context.SaveChangesAsync();

        var command = new UpdateProjectWikiPageCommand(
            project.Id,
            page.Id,
            "Updated Title",
            null,
            "Updated Content",
            page.Id, // Self as parent!
            false);

        var handler = new UpdateProjectWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.CircularHierarchy);
    }

    [Fact]
    public async Task Handle_Should_ReturnProjectNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var command = new UpdateProjectWikiPageCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Title",
            null,
            "Content",
            null,
            false);

        var handler = new UpdateProjectWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProjectErrors.NotFound(command.ProjectId));
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserNotSupportOrAdminInProject()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "P1" };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.User // Regular user
        });
        await context.SaveChangesAsync();

        var command = new UpdateProjectWikiPageCommand(
            project.Id,
            Guid.NewGuid(),
            "Title",
            null,
            "Content",
            null,
            false);

        var handler = new UpdateProjectWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.UnauthorizedAccess);
    }

    [Fact]
    public async Task Handle_Should_ReturnPageNotFound_WhenPageDoesNotExist()
    {
        // Arrange
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
        await context.SaveChangesAsync();

        var pageId = Guid.NewGuid();
        var command = new UpdateProjectWikiPageCommand(
            project.Id,
            pageId,
            "Title",
            null,
            "Content",
            null,
            false);

        var handler = new UpdateProjectWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.NotFound(pageId));
    }

    [Fact]
    public async Task Handle_Should_ReturnParentNotFound_WhenParentDoesNotExist()
    {
        // Arrange
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

        var page = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Title",
            Slug = "title",
            Content = "Content"
        };
        context.WikiPages.Add(page);
        await context.SaveChangesAsync();

        var parentId = Guid.NewGuid();
        var command = new UpdateProjectWikiPageCommand(
            project.Id,
            page.Id,
            "Updated Title",
            null,
            "Updated Content",
            parentId,
            false);

        var handler = new UpdateProjectWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.ParentNotFound(parentId));
    }

    [Fact]
    public async Task Handle_Should_ReturnSlugAlreadyExists_WhenSlugBelongsToAnotherPage()
    {
        // Arrange
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

        var page1 = new WikiPage { Id = Guid.NewGuid(), ProjectId = project.Id, Title = "Page 1", Slug = "page-1", Content = "C1" };
        var page2 = new WikiPage { Id = Guid.NewGuid(), ProjectId = project.Id, Title = "Page 2", Slug = "page-2", Content = "C2" };
        context.WikiPages.AddRange(page1, page2);
        await context.SaveChangesAsync();

        var command = new UpdateProjectWikiPageCommand(
            project.Id,
            page1.Id,
            "Page 1",
            "page-2", // Collides with page 2
            "Updated Content",
            null,
            false);

        var handler = new UpdateProjectWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.SlugAlreadyExists("page-2"));
    }

    [Fact]
    public async Task Handle_Should_UpdatePageAndRaiseEvent_WhenValid()
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

        var page = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Old Title",
            Slug = "old-title",
            Content = "Old Content",
            Status = WikiPageStatus.Draft,
            CreatedAt = _utcNow.AddDays(-1)
        };
        context.WikiPages.Add(page);
        await context.SaveChangesAsync();

        var command = new UpdateProjectWikiPageCommand(
            project.Id,
            page.Id,
            "New Title",
            "new-custom-slug",
            "New Content",
            null,
            true);

        var handler = new UpdateProjectWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        WikiPage updated = await context.WikiPages.SingleAsync(wp => wp.Id == page.Id);
        updated.Title.ShouldBe("New Title");
        updated.Slug.ShouldBe("new-custom-slug");
        updated.Content.ShouldBe("New Content");
        updated.IsInternalOnly.ShouldBeTrue();
        updated.UpdatedAt.ShouldBe(_utcNow);
        updated.UpdatedByUserId.ShouldBe(_userId);
        updated.DomainEvents.ShouldContain(e => e is WikiPageUpdatedDomainEvent);
    }
}
