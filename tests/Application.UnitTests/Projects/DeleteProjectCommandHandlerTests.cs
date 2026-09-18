using Application.Projects.Delete;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Projects;

public sealed class DeleteProjectCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var command = new DeleteProjectCommand(Guid.NewGuid());
        var handler = new DeleteProjectCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProjectErrors.NotFound(command.ProjectId));
    }

    [Fact]
    public async Task Handle_Should_DeleteProjectAndRaiseDomainEvent_WhenProjectExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Name = "API", Description = "Description" };
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        var command = new DeleteProjectCommand(project.Id);
        var handler = new DeleteProjectCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        (await context.Projects.AnyAsync()).ShouldBeFalse();
        project.DomainEvents.ShouldContain(domainEvent => domainEvent is ProjectDeletedDomainEvent);
    }
}
