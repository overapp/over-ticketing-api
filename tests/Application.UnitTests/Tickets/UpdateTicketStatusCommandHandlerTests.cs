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
}
