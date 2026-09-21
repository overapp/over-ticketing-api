using Application.Abstractions.Authentication;
using Application.Common;
using Application.UnitTests.Abstractions;
using Application.Wiki.CreateGlobalWikiPage;
using Domain.Users;
using Domain.Wiki;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Wiki;

public sealed class CreateGlobalWikiPageCommandHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 9, 21, 14, 0, 0, DateTimeKind.Utc);

    public CreateGlobalWikiPageCommandHandlerTests()
    {
        _userContext.UserId.Returns(_userId);
        _dateTimeProvider.UtcNow.Returns(_utcNow);
    }

    [Fact]
    public async Task Handle_Should_ReturnGlobalPagesRequireAdmin_WhenUserIsNotAdmin()
    {
        await using TestDbContext context = CreateDbContext();
        var command = new CreateGlobalWikiPageCommand(
            "Global FAQ",
            "faq",
            "Content",
            null,
            false);

        var handler = new CreateGlobalWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.GlobalPagesRequireAdmin);
    }

    [Fact]
    public async Task Handle_Should_CreateGlobalDraftWikiPage_WhenUserIsAdmin()
    {
        await using TestDbContext context = CreateDbContext();
        var adminRole = new Role(RoleNames.Admin) { Id = Guid.NewGuid() };
        context.Roles.Add(adminRole);
        context.UserRoles.Add(new IdentityUserRole<Guid>
        {
            UserId = _userId,
            RoleId = adminRole.Id
        });
        await context.SaveChangesAsync();

        var command = new CreateGlobalWikiPageCommand(
            "Company Policies",
            "company-policies",
            "# Company Policies Content",
            null,
            false);

        var handler = new CreateGlobalWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        WikiPage page = await context.WikiPages.SingleAsync(wp => wp.Id == result.Value);
        page.ProjectId.ShouldBeNull();
        page.Title.ShouldBe("Company Policies");
        page.Slug.ShouldBe("company-policies");
        page.Status.ShouldBe(WikiPageStatus.Draft);
        page.CreatedByUserId.ShouldBe(_userId);
        page.CreatedAt.ShouldBe(_utcNow);
        page.DomainEvents.ShouldContain(e => e is WikiPageCreatedDomainEvent);
    }

    [Fact]
    public async Task Handle_Should_ReturnParentNotFound_WhenParentDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var adminRole = new Role(RoleNames.Admin) { Id = Guid.NewGuid() };
        context.Roles.Add(adminRole);
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = _userId, RoleId = adminRole.Id });
        await context.SaveChangesAsync();

        var parentId = Guid.NewGuid();
        var command = new CreateGlobalWikiPageCommand("Child Page", "child", "Content", parentId, false);
        var handler = new CreateGlobalWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.ParentNotFound(parentId));
    }

    [Fact]
    public async Task Handle_Should_ReturnParentScopeMismatch_WhenParentIsProjectPage()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var adminRole = new Role(RoleNames.Admin) { Id = Guid.NewGuid() };
        context.Roles.Add(adminRole);
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = _userId, RoleId = adminRole.Id });

        var projectPage = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(), // project scope!
            Title = "Parent Project Page",
            Slug = "parent-page",
            Content = "Content"
        };
        context.WikiPages.Add(projectPage);
        await context.SaveChangesAsync();

        var command = new CreateGlobalWikiPageCommand("Child Page", "child", "Content", projectPage.Id, false);
        var handler = new CreateGlobalWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.ParentScopeMismatch);
    }

    [Fact]
    public async Task Handle_Should_ReturnSlugAlreadyExists_WhenSlugAlreadyExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var adminRole = new Role(RoleNames.Admin) { Id = Guid.NewGuid() };
        context.Roles.Add(adminRole);
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = _userId, RoleId = adminRole.Id });

        var existingPage = new WikiPage
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Title = "Existing Page",
            Slug = "existing-slug",
            Content = "Content"
        };
        context.WikiPages.Add(existingPage);
        await context.SaveChangesAsync();

        var command = new CreateGlobalWikiPageCommand("New Page", "existing-slug", "Content", null, false);
        var handler = new CreateGlobalWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(WikiPageErrors.SlugAlreadyExists("existing-slug"));
    }

    [Fact]
    public async Task Handle_Should_GenerateSlugFromTitle_WhenSlugIsEmpty()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var adminRole = new Role(RoleNames.Admin) { Id = Guid.NewGuid() };
        context.Roles.Add(adminRole);
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = _userId, RoleId = adminRole.Id });
        await context.SaveChangesAsync();

        var command = new CreateGlobalWikiPageCommand("Generated From Title", "", "Content", null, false);
        var handler = new CreateGlobalWikiPageCommandHandler(context, _userContext, _dateTimeProvider);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        WikiPage page = await context.WikiPages.SingleAsync(wp => wp.Id == result.Value);
        page.Slug.ShouldBe("generated-from-title");
    }
}
