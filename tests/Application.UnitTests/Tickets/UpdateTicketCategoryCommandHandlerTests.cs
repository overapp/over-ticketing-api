using Application.Abstractions.Authentication;
using Application.Tickets.UpdateCategory;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Tickets;

public sealed class UpdateTicketCategoryCommandHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 9, 21, 14, 0, 0, DateTimeKind.Utc);

    public UpdateTicketCategoryCommandHandlerTests()
    {
        _userContext.UserId.Returns(_userId);
        _dateTimeProvider.UtcNow.Returns(_utcNow);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTicketDoesNotExist()
    {
        await using TestDbContext context = CreateDbContext();
        var handler = new UpdateTicketCategoryCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketCategoryCommand(Guid.NewGuid(), null);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.NotFound(command.TicketId));
    }

    [Fact]
    public async Task Handle_Should_ReturnClosed_WhenTicketIsClosed()
    {
        await using TestDbContext context = CreateDbContext();
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Status = TicketStatus.Closed
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new UpdateTicketCategoryCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketCategoryCommand(ticket.Id, null);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.Closed(ticket.Id));
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserIsNotSupportOrAdmin()
    {
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Status = TicketStatus.New
        };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.User
        };
        context.Projects.Add(project);
        context.Tickets.Add(ticket);
        context.ProjectAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var handler = new UpdateTicketCategoryCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketCategoryCommand(ticket.Id, null);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.UnauthorizedAccess(ticket.Id));
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenCategoryDoesNotExist()
    {
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Status = TicketStatus.New
        };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.Support
        };
        context.Projects.Add(project);
        context.Tickets.Add(ticket);
        context.ProjectAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var missingCategoryId = Guid.NewGuid();
        var handler = new UpdateTicketCategoryCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketCategoryCommand(ticket.Id, missingCategoryId);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketCategoryErrors.NotFound(missingCategoryId));
    }

    [Fact]
    public async Task Handle_Should_UpdateCategoryAndRaiseDomainEvent_WhenValid()
    {
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var oldCategory = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = "Old Category",
            Status = TicketCategoryStatus.Active
        };
        var newCategory = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Name = "Global Bug",
            Status = TicketCategoryStatus.Active
        };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CategoryId = oldCategory.Id,
            Status = TicketStatus.InProgress
        };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.Support
        };
        context.Projects.Add(project);
        context.TicketCategories.AddRange(oldCategory, newCategory);
        context.Tickets.Add(ticket);
        context.ProjectAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var handler = new UpdateTicketCategoryCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketCategoryCommand(ticket.Id, newCategory.Id);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        Ticket updatedTicket = await context.Tickets.SingleAsync(t => t.Id == ticket.Id);
        updatedTicket.CategoryId.ShouldBe(newCategory.Id);
        updatedTicket.UpdatedAt.ShouldBe(_utcNow);

        TicketCategoryChangedDomainEvent? domainEvent = updatedTicket.DomainEvents.OfType<TicketCategoryChangedDomainEvent>().SingleOrDefault();
        domainEvent.ShouldNotBeNull();
        domainEvent.TicketId.ShouldBe(ticket.Id);
        domainEvent.PreviousCategoryId.ShouldBe(oldCategory.Id);
        domainEvent.NewCategoryId.ShouldBe(newCategory.Id);
    }

    [Fact]
    public async Task Handle_Should_RemoveCategory_WhenCategoryIdIsNull()
    {
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var oldCategory = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = "Old Category",
            Status = TicketCategoryStatus.Active
        };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CategoryId = oldCategory.Id,
            Status = TicketStatus.InProgress
        };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.Admin
        };
        context.Projects.Add(project);
        context.TicketCategories.Add(oldCategory);
        context.Tickets.Add(ticket);
        context.ProjectAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var handler = new UpdateTicketCategoryCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketCategoryCommand(ticket.Id, null);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        Ticket updatedTicket = await context.Tickets.SingleAsync(t => t.Id == ticket.Id);
        updatedTicket.CategoryId.ShouldBeNull();

        TicketCategoryChangedDomainEvent? domainEvent = updatedTicket.DomainEvents.OfType<TicketCategoryChangedDomainEvent>().SingleOrDefault();
        domainEvent.ShouldNotBeNull();
        domainEvent.TicketId.ShouldBe(ticket.Id);
        domainEvent.PreviousCategoryId.ShouldBe(oldCategory.Id);
        domainEvent.NewCategoryId.ShouldBeNull();
    }
}
