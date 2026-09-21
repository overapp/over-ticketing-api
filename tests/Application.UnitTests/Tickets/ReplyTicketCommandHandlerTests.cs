using Application.Abstractions.Authentication;
using Application.Abstractions.Storage;
using Application.Tickets.Reply;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;

namespace Application.UnitTests.Tickets;

public sealed class ReplyTicketCommandHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IFileStorageService _fileStorageService = Substitute.For<IFileStorageService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public ReplyTicketCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTicketDoesNotExist()
    {
        await using TestDbContext context = CreateDbContext();
        _userContext.UserId.Returns(Guid.NewGuid());

        var handler = new ReplyTicketCommandHandler(context, _userContext, _fileStorageService, _dateTimeProvider);
        var command = new ReplyTicketCommand(Guid.NewGuid(), "A reply");

        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.NotFound(command.TicketId));
    }

    [Fact]
    public async Task Handle_Should_SetWaitingForCustomerAndFirstResponseAt_WhenSupportRepliesPublicly()
    {
        await using TestDbContext context = CreateDbContext();
        var supportUserId = Guid.NewGuid();
        var creatorUserId = Guid.NewGuid();
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
            CreatedByUserId = creatorUserId,
            Title = "Issue",
            Status = TicketStatus.InProgress,
            CreatedAt = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new ReplyTicketCommandHandler(context, _userContext, _fileStorageService, _dateTimeProvider);
        var command = new ReplyTicketCommand(ticket.Id, "Support reply", IsInternal: false);

        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        Ticket updatedTicket = await context.Tickets.SingleAsync(t => t.Id == ticket.Id);
        updatedTicket.Status.ShouldBe(TicketStatus.WaitingForCustomer);
        updatedTicket.FirstResponseAt.ShouldBe(new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Handle_Should_ReopenTicketToWaitingForSupport_WhenUserRepliesToResolvedTicket()
    {
        await using TestDbContext context = CreateDbContext();
        var creatorUserId = Guid.NewGuid();
        _userContext.UserId.Returns(creatorUserId);

        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        context.Projects.Add(project);

        context.ProjectAssignments.Add(new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = creatorUserId,
            Role = RoleNames.User
        });

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CreatedByUserId = creatorUserId,
            Title = "Issue",
            Status = TicketStatus.Resolved,
            ResolvedAt = new DateTime(2026, 9, 21, 11, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new ReplyTicketCommandHandler(context, _userContext, _fileStorageService, _dateTimeProvider);
        var command = new ReplyTicketCommand(ticket.Id, "Still broken", IsInternal: false);

        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        Ticket updatedTicket = await context.Tickets.SingleAsync(t => t.Id == ticket.Id);
        updatedTicket.Status.ShouldBe(TicketStatus.WaitingForSupport);
        updatedTicket.ResolvedAt.ShouldBeNull();
    }
}
