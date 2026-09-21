using Application.Abstractions.Authentication;
using Application.Common;
using Application.Tickets.Get;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Tickets;

public sealed class GetTicketsQueryHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly Guid _userId = Guid.NewGuid();

    public GetTicketsQueryHandlerTests()
    {
        _userContext.UserId.Returns(_userId);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserNotInProject()
    {
        await using TestDbContext context = CreateDbContext();
        var handler = new GetTicketsQueryHandler(context, _userContext);
        var query = new GetTicketsQuery(Guid.NewGuid());

        Result<PagedResponse<TicketSummaryResponse>> result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.UserNotInProject(_userId, query.ProjectId));
    }

    [Fact]
    public async Task Handle_Should_FilterByCategoryId_AndPopulateCategoryResponse()
    {
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.Support
        };
        var category1 = new TicketCategory
        {
            Id = Guid.NewGuid(),
            Name = "Bug",
            BackgroundColor = "#FF0000",
            ForegroundColor = "#FFFFFF",
            Status = TicketCategoryStatus.Active
        };
        var category2 = new TicketCategory
        {
            Id = Guid.NewGuid(),
            Name = "Feature",
            BackgroundColor = "#00FF00",
            ForegroundColor = "#000000",
            Status = TicketCategoryStatus.Active
        };
        var ticket1 = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CategoryId = category1.Id,
            Title = "Ticket 1",
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow
        };
        var ticket2 = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CategoryId = category2.Id,
            Title = "Ticket 2",
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow.AddMinutes(1)
        };
        var ticket3 = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CategoryId = null,
            Title = "Ticket 3",
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow.AddMinutes(2)
        };

        context.Projects.Add(project);
        context.ProjectAssignments.Add(assignment);
        context.TicketCategories.AddRange(category1, category2);
        context.Tickets.AddRange(ticket1, ticket2, ticket3);
        await context.SaveChangesAsync();

        var handler = new GetTicketsQueryHandler(context, _userContext);
        var query = new GetTicketsQuery(project.Id, CategoryId: category1.Id);

        Result<PagedResponse<TicketSummaryResponse>> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        TicketSummaryResponse item = result.Value.Items.Single();
        item.Id.ShouldBe(ticket1.Id);
        item.Category.ShouldNotBeNull();
        item.Category.Id.ShouldBe(category1.Id);
        item.Category.Name.ShouldBe("Bug");
        item.Category.BackgroundColor.ShouldBe("#FF0000");
    }
}
