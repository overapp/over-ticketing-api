using Application.Projects.Archive;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Projects;

public sealed class ArchiveProjectCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var command = new ArchiveProjectCommand(Guid.NewGuid());
        var handler = new ArchiveProjectCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProjectErrors.NotFound(command.ProjectId));
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyArchived_WhenProjectIsArchived()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        Project project = CreateProject(ProjectStatus.Archived);
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        var handler = new ArchiveProjectCommandHandler(context);

        // Act
        Result result = await handler.Handle(new ArchiveProjectCommand(project.Id), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProjectErrors.AlreadyArchived(project.Id));
    }

    [Fact]
    public async Task Handle_Should_ArchiveProjectAndRaiseDomainEvent_WhenProjectIsActive()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        Project project = CreateProject(ProjectStatus.Active);
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        var handler = new ArchiveProjectCommandHandler(context);

        // Act
        Result result = await handler.Handle(new ArchiveProjectCommand(project.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        project.Status.ShouldBe(ProjectStatus.Archived);
        project.DomainEvents.ShouldContain(domainEvent => domainEvent is ProjectArchivedDomainEvent);
    }

    private static Project CreateProject(ProjectStatus status) => new()
    {
        Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Name = "API", Description = "Description", Status = status
    };
}
