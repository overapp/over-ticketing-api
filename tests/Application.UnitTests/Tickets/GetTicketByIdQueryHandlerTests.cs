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
    public async Task Handle_Should_ReturnNotFound_WhenTicketDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new GetTicketByIdQueryHandler(context, _userContext);
        var query = new GetTicketByIdQuery(Guid.NewGuid());

        // Act
        Result<TicketDetailResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.NotFound(query.TicketId));
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserNotInProject()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Ticket",
            CreatedByUserId = _userId
        };
        context.Projects.Add(project);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new GetTicketByIdQueryHandler(context, _userContext);
        var query = new GetTicketByIdQuery(ticket.Id);

        // Act
        Result<TicketDetailResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.UserNotInProject(_userId, project.Id));
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserIsNotSupportOrAdminAndNotCreator()
    {
        // Arrange
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
            Title = "Ticket",
            CreatedByUserId = Guid.NewGuid() // Different creator
        };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(assignment);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new GetTicketByIdQueryHandler(context, _userContext);
        var query = new GetTicketByIdQuery(ticket.Id);

        // Act
        Result<TicketDetailResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.UnauthorizedAccess(ticket.Id));
    }

    [Fact]
    public async Task Handle_Should_FilterInternalMessages_WhenUserIsStandardUser()
    {
        // Arrange
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
            Title = "Ticket",
            CreatedByUserId = _userId
        };
        var publicMsg = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = _userId,
            Content = "Public msg",
            IsInternal = false,
            CreatedAt = DateTime.UtcNow
        };
        var internalMsg = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = Guid.NewGuid(),
            Content = "Internal msg",
            IsInternal = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(1)
        };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(assignment);
        context.Tickets.Add(ticket);
        context.TicketMessages.AddRange(publicMsg, internalMsg);
        await context.SaveChangesAsync();

        var handler = new GetTicketByIdQueryHandler(context, _userContext);
        var query = new GetTicketByIdQuery(ticket.Id);

        // Act
        Result<TicketDetailResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Messages.Count.ShouldBe(1);
        result.Value.Messages.First().Id.ShouldBe(publicMsg.Id);
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
