using Application.Abstractions.Authentication;
using Application.Tickets.UpdatePriority;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;

namespace Application.UnitTests.Tickets;

public sealed class UpdateTicketPriorityCommandHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public UpdateTicketPriorityCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 9, 21, 10, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Handle_Should_RecalculateSla_WhenPriorityIsChangedBySupport()
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

        var createdAt = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc);
        (DateTime initialFirstDue, DateTime initialResDue) = TicketSlaCalculator.CalculateDueDates(createdAt, TicketPriority.Low);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CreatedByUserId = Guid.NewGuid(),
            Title = "Ticket",
            Status = TicketStatus.New,
            Priority = TicketPriority.Low,
            CreatedAt = createdAt,
            FirstResponseDueAt = initialFirstDue,
            ResolutionDueAt = initialResDue
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new UpdateTicketPriorityCommandHandler(context, _userContext, _dateTimeProvider);
        var command = new UpdateTicketPriorityCommand(ticket.Id, TicketPriority.Urgent);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        Ticket updatedTicket = await context.Tickets.SingleAsync(t => t.Id == ticket.Id);
        updatedTicket.Priority.ShouldBe(TicketPriority.Urgent);
        // Recalculated from createdAt (10:00) with Urgent rules (+1h, +4h)
        updatedTicket.FirstResponseDueAt.ShouldBe(createdAt.AddHours(1));
        updatedTicket.ResolutionDueAt.ShouldBe(createdAt.AddHours(4));
    }
}
