using Application.Projects.Update;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Projects;

public sealed class UpdateProjectCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var command = new UpdateProjectCommand(Guid.NewGuid(), "Updated", "Updated description");
        var handler = new UpdateProjectCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProjectErrors.NotFound(command.ProjectId));
    }

    [Fact]
    public async Task Handle_Should_UpdateFieldsAndPreserveStatus_WhenProjectExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var project = new Project
        {
            Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Name = "Previous",
            Description = "Previous description", Status = ProjectStatus.Archived
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        var command = new UpdateProjectCommand(project.Id, "Updated", "Updated description");
        var handler = new UpdateProjectCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Project updatedProject = await context.Projects.SingleAsync();
        updatedProject.Name.ShouldBe(command.Name);
        updatedProject.Description.ShouldBe(command.Description);
        updatedProject.Status.ShouldBe(ProjectStatus.Archived);
        updatedProject.DomainEvents.ShouldContain(domainEvent => domainEvent is ProjectUpdatedDomainEvent);
    }
}
