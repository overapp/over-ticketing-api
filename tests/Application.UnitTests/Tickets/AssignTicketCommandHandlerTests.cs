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
}
