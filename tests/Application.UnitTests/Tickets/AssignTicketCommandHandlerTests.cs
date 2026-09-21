using Application.Abstractions.Authentication;
using Application.Tickets.Assign;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;

namespace Application.UnitTests.Tickets;

public sealed class AssignTicketCommandHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public AssignTicketCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Handle_Should_AssignTicketAndSetInProgress_WhenValid()
    {
        await using TestDbContext context = CreateDbContext();
        var supportUserId = Guid.NewGuid();
        _userContext.UserId.Returns(supportUserId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        context.Projects.Add(project);

        context.ProjectAssignments.Add(new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = supportUserId,
            Role = RoleNames.Support
        });

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CreatedByUserId = Guid.NewGuid(),
            Title = "Ticket",
            Status = TicketStatus.New,
            CreatedAt = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Utc)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new AssignTicketCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new AssignTicketCommand(ticket.Id, supportUserId);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        Ticket updatedTicket = await context.Tickets.SingleAsync(t => t.Id == ticket.Id);
        updatedTicket.AssignedToUserId.ShouldBe(supportUserId);
        updatedTicket.Status.ShouldBe(TicketStatus.InProgress);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTicketDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        _userContext.UserId.Returns(Guid.NewGuid());

        var handler = new AssignTicketCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new AssignTicketCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.NotFound(command.TicketId));
    }

    [Fact]
    public async Task Handle_Should_ReturnClosed_WhenTicketIsClosed()
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

        var handler = new AssignTicketCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new AssignTicketCommand(ticket.Id, userId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.Closed(ticket.Id));
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerNotInProject()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var callerId = Guid.NewGuid();
        _userContext.UserId.Returns(callerId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Ticket",
            Status = TicketStatus.New
        };
        context.Projects.Add(project);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new AssignTicketCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new AssignTicketCommand(ticket.Id, callerId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.UserNotInProject(callerId, project.Id));
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenCallerIsNotSupportOrAdmin()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var callerId = Guid.NewGuid();
        _userContext.UserId.Returns(callerId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Ticket",
            Status = TicketStatus.New
        };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = callerId,
            Role = RoleNames.User
        });
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new AssignTicketCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new AssignTicketCommand(ticket.Id, callerId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.UnauthorizedAccess(ticket.Id));
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenTargetAssigneeNotInProject()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var supportId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _userContext.UserId.Returns(supportId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = supportId,
            Role = RoleNames.Support
        });

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Ticket",
            Status = TicketStatus.New
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new AssignTicketCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new AssignTicketCommand(ticket.Id, targetId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.UserNotInProject(targetId, project.Id));
    }

    [Fact]
    public async Task Handle_Should_ReturnAssigneeNotSupport_WhenTargetAssigneeIsRegularUser()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var supportId = Guid.NewGuid();
        var regularUserId = Guid.NewGuid();
        _userContext.UserId.Returns(supportId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        context.Projects.Add(project);
        context.ProjectAssignments.AddRange(
            new ProjectAssignment { Id = Guid.NewGuid(), ProjectId = project.Id, UserId = supportId, Role = RoleNames.Support },
            new ProjectAssignment { Id = Guid.NewGuid(), ProjectId = project.Id, UserId = regularUserId, Role = RoleNames.User });

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Ticket",
            Status = TicketStatus.New
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new AssignTicketCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new AssignTicketCommand(ticket.Id, regularUserId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.AssigneeNotSupport(regularUserId));
    }

    [Fact]
    public async Task Handle_Should_UnassignTicket_WhenAssignedToUserIdIsNull()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var supportId = Guid.NewGuid();
        _userContext.UserId.Returns(supportId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = supportId,
            Role = RoleNames.Support
        });

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Ticket",
            Status = TicketStatus.InProgress,
            AssignedToUserId = supportId
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new AssignTicketCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new AssignTicketCommand(ticket.Id, null);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Ticket updated = await context.Tickets.SingleAsync(t => t.Id == ticket.Id);
        updated.AssignedToUserId.ShouldBeNull();
        updated.DomainEvents.ShouldContain(e => e is TicketAssignedDomainEvent);
    }
}
