using Application.Abstractions.Authentication;
using Application.Tickets.UpdateStatus;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;

namespace Application.UnitTests.Tickets;

public sealed class UpdateTicketStatusCommandHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public UpdateTicketStatusCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 9, 21, 15, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Handle_Should_SetResolved_WhenCustomerMarksResolved()
    {
        await using TestDbContext context = CreateDbContext();
        var customerUserId = Guid.NewGuid();
        _userContext.UserId.Returns(customerUserId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        context.Projects.Add(project);

        context.ProjectAssignments.Add(new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = customerUserId,
            Role = RoleNames.User
        });

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CreatedByUserId = customerUserId,
            Title = "Ticket",
            Status = TicketStatus.InProgress,
            CreatedAt = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new UpdateTicketStatusCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketStatusCommand(ticket.Id, TicketStatus.Resolved);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        Ticket updatedTicket = await context.Tickets.SingleAsync(t => t.Id == ticket.Id);
        updatedTicket.Status.ShouldBe(TicketStatus.Resolved);
        updatedTicket.ResolvedAt.ShouldBe(new DateTime(2026, 9, 21, 15, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTicketDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        _userContext.UserId.Returns(Guid.NewGuid());

        var handler = new UpdateTicketStatusCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketStatusCommand(Guid.NewGuid(), TicketStatus.Closed);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.NotFound(command.TicketId));
    }

    [Fact]
    public async Task Handle_Should_ReturnClosed_WhenTicketAlreadyClosed()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Title = "Closed",
            Status = TicketStatus.Closed
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new UpdateTicketStatusCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketStatusCommand(ticket.Id, TicketStatus.Resolved);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.Closed(ticket.Id));
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserNotInProject()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Ticket",
            Status = TicketStatus.InProgress
        };
        context.Projects.Add(project);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new UpdateTicketStatusCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketStatusCommand(ticket.Id, TicketStatus.Closed);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.UserNotInProject(userId, project.Id));
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserIsNotSupportOrAdminAndNotCreator()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            Role = RoleNames.User
        };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CreatedByUserId = Guid.NewGuid(), // different user
            Title = "Ticket",
            Status = TicketStatus.InProgress
        };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(assignment);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new UpdateTicketStatusCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketStatusCommand(ticket.Id, TicketStatus.Closed);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.UnauthorizedAccess(ticket.Id));
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidStatusTransition_WhenUserTriesOtherStatus()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            Role = RoleNames.User
        };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CreatedByUserId = userId,
            Title = "Ticket",
            Status = TicketStatus.WaitingForSupport
        };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(assignment);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new UpdateTicketStatusCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketStatusCommand(ticket.Id, TicketStatus.InProgress);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.InvalidStatusTransition(ticket.Status, command.Status));
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenStatusIsAlreadySame()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            Role = RoleNames.Support
        };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CreatedByUserId = userId,
            Title = "Ticket",
            Status = TicketStatus.InProgress
        };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(assignment);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new UpdateTicketStatusCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketStatusCommand(ticket.Id, TicketStatus.InProgress);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_Should_SetClosedAndRaiseEvent_WhenCustomerMarksClosed()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var customerUserId = Guid.NewGuid();
        _userContext.UserId.Returns(customerUserId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = customerUserId,
            Role = RoleNames.User
        });

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CreatedByUserId = customerUserId,
            Title = "Ticket",
            Status = TicketStatus.InProgress
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new UpdateTicketStatusCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketStatusCommand(ticket.Id, TicketStatus.Closed);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Ticket updatedTicket = await context.Tickets.SingleAsync(t => t.Id == ticket.Id);
        updatedTicket.Status.ShouldBe(TicketStatus.Closed);
        updatedTicket.ClosedAt.ShouldBe(new DateTime(2026, 9, 21, 15, 0, 0, DateTimeKind.Utc));
        updatedTicket.DomainEvents.ShouldContain(e => e is TicketStatusChangedDomainEvent);
    }
}
