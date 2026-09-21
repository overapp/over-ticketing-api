using Application.Abstractions.Authentication;
using Application.Abstractions.Storage;
using Application.Tickets.Create;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;

namespace Application.UnitTests.Tickets;

public sealed class CreateTicketCommandHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IFileStorageService _fileStorageService = Substitute.For<IFileStorageService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CreateTicketCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Handle_Should_ReturnProjectNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);

        var handler = new CreateTicketCommandHandler(context, _userContext, _fileStorageService, _dateTimeProvider);
        var command = new CreateTicketCommand(Guid.NewGuid(), "Ticket title", "Ticket message");

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProjectErrors.NotFound(command.ProjectId));
    }

    [Fact]
    public async Task Handle_Should_ReturnUserNotInProject_WhenUserIsNotAssignedToProject()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var handler = new CreateTicketCommandHandler(context, _userContext, _fileStorageService, _dateTimeProvider);
        var command = new CreateTicketCommand(project.Id, "Ticket title", "Ticket message");

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.UserNotInProject(userId, project.Id));
    }

    [Fact]
    public async Task Handle_Should_CreateTicketWithFirstMessageAndSla_WhenValid()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        context.Projects.Add(project);

        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            Role = RoleNames.User
        };
        context.ProjectAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var handler = new CreateTicketCommandHandler(context, _userContext, _fileStorageService, _dateTimeProvider);
        var command = new CreateTicketCommand(project.Id, "Ticket title", "Ticket message", TicketPriority.Urgent);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Ticket ticket = await context.Tickets
            .Include(t => t.Messages)
            .SingleAsync(t => t.Id == result.Value);

        ticket.ProjectId.ShouldBe(project.Id);
        ticket.CreatedByUserId.ShouldBe(userId);
        ticket.Title.ShouldBe("Ticket title");
        ticket.Status.ShouldBe(TicketStatus.New);
        ticket.Priority.ShouldBe(TicketPriority.Urgent);
        ticket.CreatedAt.ShouldBe(new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc));
        ticket.FirstResponseDueAt.ShouldBe(new DateTime(2026, 9, 21, 11, 0, 0, DateTimeKind.Utc));
        ticket.ResolutionDueAt.ShouldBe(new DateTime(2026, 9, 21, 14, 0, 0, DateTimeKind.Utc));

        ticket.Messages.Count.ShouldBe(1);
        TicketMessage message = ticket.Messages.First();
        message.Content.ShouldBe("Ticket message");
        message.AuthorUserId.ShouldBe(userId);
        message.IsInternal.ShouldBeFalse();

        ticket.DomainEvents.ShouldContain(e => e is TicketCreatedDomainEvent);
    }
}
