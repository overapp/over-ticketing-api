using Application.Abstractions.Authentication;
using Application.Common;
using Application.UnitTests.Abstractions;
using Application.Wiki.UpdateWikiPageStatus;
using Domain.Projects;
using Domain.Users;
using Domain.Wiki;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Wiki;

public sealed class UpdateWikiPageStatusCommandHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 9, 21, 14, 0, 0, DateTimeKind.Utc);

    public UpdateWikiPageStatusCommandHandlerTests()
    {
        _userContext.UserId.Returns(_userId);
        _dateTimeProvider.UtcNow.Returns(_utcNow);
    }

    [Fact]
    public async Task Handle_Should_ReturnHasChildPages_WhenArchivingPageWithActiveChildPages()
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

        var parentPage = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Parent",
            Slug = "parent",
            Content = "Parent",
            Status = WikiPageStatus.Published
        };
        context.WikiPages.Add(parentPage);

        var childPage = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ParentPageId = parentPage.Id,
            Title = "Child",
            Slug = "child",
            Content = "Child",
            Status = WikiPageStatus.Published
        };
        context.WikiPages.Add(childPage);
        await context.SaveChangesAsync();

        var command = new UpdateWikiPageStatusCommand(parentPage.Id, WikiPageStatus.Archived);
        var handler = new UpdateWikiPageStatusCommandHandler(context, _userContext, _dateTimeProvider);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.HasChildPages(parentPage.Id));
    }

    [Fact]
    public async Task Handle_Should_ArchivePageAndRaiseEvent_WhenNoActiveChildPages()
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
            Title = "Solo Page",
            Slug = "solo-page",
            Content = "Solo",
            Status = WikiPageStatus.Published
        };
        context.WikiPages.Add(page);
        await context.SaveChangesAsync();

        var command = new UpdateWikiPageStatusCommand(page.Id, WikiPageStatus.Archived);
        var handler = new UpdateWikiPageStatusCommandHandler(context, _userContext, _dateTimeProvider);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        WikiPage updated = await context.WikiPages.SingleAsync(wp => wp.Id == page.Id);
        updated.Status.ShouldBe(WikiPageStatus.Archived);
        updated.UpdatedAt.ShouldBe(_utcNow);
        updated.UpdatedByUserId.ShouldBe(_userId);
        updated.DomainEvents.ShouldContain(e => e is WikiPageArchivedDomainEvent);
    }
}
