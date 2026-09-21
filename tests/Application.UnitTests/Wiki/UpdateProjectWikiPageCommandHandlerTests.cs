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
