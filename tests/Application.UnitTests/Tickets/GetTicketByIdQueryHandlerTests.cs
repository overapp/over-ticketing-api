using Application.Abstractions.Authentication;
using Application.Tickets.GetById;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Tickets;

public sealed class GetTicketByIdQueryHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly Guid _userId = Guid.NewGuid();

    public GetTicketByIdQueryHandlerTests()
    {
        _userContext.UserId.Returns(_userId);
    }

    [Fact]
    public async Task Handle_Should_PopulateCategory_WhenTicketHasCategory()
    {
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.User
        };
        var category = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = "Support Question",
            BackgroundColor = "#3B82F6",
            ForegroundColor = "#FFFFFF",
            Status = TicketCategoryStatus.Active
        };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CategoryId = category.Id,
            Title = "My Ticket",
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        context.Projects.Add(project);
        context.ProjectAssignments.Add(assignment);
        context.TicketCategories.Add(category);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new GetTicketByIdQueryHandler(context, _userContext);
        var query = new GetTicketByIdQuery(ticket.Id);

        Result<TicketDetailResponse> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Category.ShouldNotBeNull();
        result.Value.Category.Id.ShouldBe(category.Id);
        result.Value.Category.Name.ShouldBe("Support Question");
        result.Value.Category.BackgroundColor.ShouldBe("#3B82F6");
    }

    [Fact]
    public async Task Handle_Should_HaveNullCategory_WhenTicketHasNoCategory()
    {
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.User
        };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CategoryId = null,
            Title = "My Ticket",
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow
        };

        context.Projects.Add(project);
        context.ProjectAssignments.Add(assignment);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new GetTicketByIdQueryHandler(context, _userContext);
        var query = new GetTicketByIdQuery(ticket.Id);

        Result<TicketDetailResponse> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Category.ShouldBeNull();
    }
}
