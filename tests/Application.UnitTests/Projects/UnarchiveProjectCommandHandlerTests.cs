using Application.Projects.Unarchive;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Projects;

public sealed class UnarchiveProjectCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var command = new UnarchiveProjectCommand(Guid.NewGuid());
        var handler = new UnarchiveProjectCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProjectErrors.NotFound(command.ProjectId));
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyActive_WhenProjectIsActive()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        Project project = CreateProject(ProjectStatus.Active);
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        var handler = new UnarchiveProjectCommandHandler(context);

        // Act
        Result result = await handler.Handle(new UnarchiveProjectCommand(project.Id), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProjectErrors.AlreadyActive(project.Id));
    }

    [Fact]
    public async Task Handle_Should_UnarchiveProjectAndRaiseDomainEvent_WhenProjectIsArchived()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        Project project = CreateProject(ProjectStatus.Archived);
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        var handler = new UnarchiveProjectCommandHandler(context);

        // Act
        Result result = await handler.Handle(new UnarchiveProjectCommand(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        project.Status.ShouldBe(ProjectStatus.Active);
        project.DomainEvents.ShouldContain(domainEvent => domainEvent is ProjectUnarchivedDomainEvent);
    }

    private static Project CreateProject(ProjectStatus status) => new()
    {
        Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Name = "API", Description = "Description", Status = status
    };
}
