using Application.Abstractions.Authentication;
using Application.Common;
using Application.UnitTests.Abstractions;
using Application.Wiki.CreateProjectWikiPage;
using Domain.Projects;
using Domain.Users;
using Domain.Wiki;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Wiki;

public sealed class CreateProjectWikiPageCommandHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);

    public CreateProjectWikiPageCommandHandlerTests()
    {
        _userContext.UserId.Returns(_userId);
        _dateTimeProvider.UtcNow.Returns(_utcNow);
    }

    [Fact]
    public async Task Handle_Should_ReturnProjectNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var command = new CreateProjectWikiPageCommand(
            Guid.NewGuid(),
            "Title",
            null,
            "Content",
            null,
            false);

        var handler = new CreateProjectWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProjectErrors.NotFound(command.ProjectId));
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserIsNotAdminAndNotAssignedWithSupportOrAdminRole()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project 1" };
        context.Projects.Add(project);

        // Standard user assignment
        context.ProjectAssignments.Add(new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.User
        });
        await context.SaveChangesAsync();

        var command = new CreateProjectWikiPageCommand(
            project.Id,
            "Getting Started",
            null,
            "Content",
            null,
            false);

        var handler = new CreateProjectWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.UnauthorizedAccess);
    }

    [Fact]
    public async Task Handle_Should_ReturnParentNotFound_WhenParentPageDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project 1" };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.Support
        });
        await context.SaveChangesAsync();

        var nonExistentParentId = Guid.NewGuid();
        var command = new CreateProjectWikiPageCommand(
            project.Id,
            "Sub Page",
            null,
            "Content",
            nonExistentParentId,
            false);

        var handler = new CreateProjectWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.ParentNotFound(nonExistentParentId));
    }

    [Fact]
    public async Task Handle_Should_ReturnSlugAlreadyExists_WhenSlugAlreadyExistsInProject()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project 1" };
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
            Title = "Existing Page",
            Slug = "existing-page",
            Content = "Test",
            Status = WikiPageStatus.Published,
            CreatedAt = _utcNow,
            CreatedByUserId = _userId
        });
        await context.SaveChangesAsync();

        var command = new CreateProjectWikiPageCommand(
            project.Id,
            "Existing Page",
            null,
            "Different Content",
            null,
            false);

        var handler = new CreateProjectWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.SlugAlreadyExists("existing-page"));
    }

    [Fact]
    public async Task Handle_Should_CreateDraftWikiPageAndRaiseDomainEvent_WhenValid()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project 1" };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.Support
        });
        await context.SaveChangesAsync();

        var command = new CreateProjectWikiPageCommand(
            project.Id,
            "Installation & Setup",
            "setup-guide",
            "# Setup Guide",
            null,
            true);

        var handler = new CreateProjectWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        WikiPage page = await context.WikiPages.SingleAsync(wp => wp.Id == result.Value);
        page.ProjectId.ShouldBe(project.Id);
        page.Title.ShouldBe("Installation & Setup");
        page.Slug.ShouldBe("setup-guide");
        page.Content.ShouldBe("# Setup Guide");
        page.Status.ShouldBe(WikiPageStatus.Draft);
        page.IsInternalOnly.ShouldBeTrue();
        page.CreatedByUserId.ShouldBe(_userId);
        page.CreatedAt.ShouldBe(_utcNow);
        page.DomainEvents.ShouldContain(e => e is WikiPageCreatedDomainEvent);
    }
}
