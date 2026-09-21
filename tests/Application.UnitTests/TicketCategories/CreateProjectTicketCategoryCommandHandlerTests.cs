using Application.TicketCategories.CreateProject;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.TicketCategories;

public sealed class CreateProjectTicketCategoryCommandHandlerTests : BaseHandlerTest
{
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly DateTime _utcNow = new(2026, 9, 21, 14, 0, 0, DateTimeKind.Utc);

    public CreateProjectTicketCategoryCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(_utcNow);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenProjectDoesNotExist()
    {
        await using TestDbContext context = CreateDbContext();
        var handler = new CreateProjectTicketCategoryCommandHandler(context, _dateTimeProvider);
        var command = new CreateProjectTicketCategoryCommand(Guid.NewGuid(), "Billing");

        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProjectErrors.NotFound(command.ProjectId));
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenSameNameAlreadyExistsInSameProject()
    {
        await using TestDbContext context = CreateDbContext();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Project Alpha",
            Status = ProjectStatus.Active
        };
        context.Projects.Add(project);

        context.TicketCategories.Add(new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = "Billing",
            Status = TicketCategoryStatus.Active,
            CreatedAt = _utcNow
        });
        await context.SaveChangesAsync();

        var handler = new CreateProjectTicketCategoryCommandHandler(context, _dateTimeProvider);
        var command = new CreateProjectTicketCategoryCommand(project.Id, "billing");

        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketCategoryErrors.NameNotUnique("billing"));
    }

    [Fact]
    public async Task Handle_Should_CreateProjectCategory_WhenValid()
    {
        await using TestDbContext context = CreateDbContext();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Project Alpha",
            Status = ProjectStatus.Active
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var handler = new CreateProjectTicketCategoryCommandHandler(context, _dateTimeProvider);
        var command = new CreateProjectTicketCategoryCommand(
            project.Id,
            "Urgent Support",
            "Urgent queries",
            "#EF4444",
            "#FFFFFF");

        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        TicketCategory? created = await context.TicketCategories.SingleOrDefaultAsync(tc => tc.Id == result.Value);
        created.ShouldNotBeNull();
        created.ProjectId.ShouldBe(project.Id);
        created.Name.ShouldBe("Urgent Support");
        created.BackgroundColor.ShouldBe("#EF4444");
        created.ForegroundColor.ShouldBe("#FFFFFF");
        created.Status.ShouldBe(TicketCategoryStatus.Active);
        created.DomainEvents.ShouldContain(e => e is TicketCategoryCreatedDomainEvent);
    }
}
