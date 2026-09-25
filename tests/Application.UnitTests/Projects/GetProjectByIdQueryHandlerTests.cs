using Application.Projects;
using Application.Projects.GetById;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using SharedKernel;

namespace Application.UnitTests.Projects;

public sealed class GetProjectByIdQueryHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new GetProjectByIdQueryHandler(context);
        var query = new GetProjectByIdQuery(Guid.NewGuid());

        // Act
        Result<ProjectResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Projects.NotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnProject_WhenProjectExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Over Ticketing",
            Description = "Ticketing system API",
            Status = ProjectStatus.Active
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var handler = new GetProjectByIdQueryHandler(context);
        var query = new GetProjectByIdQuery(project.Id);

        // Act
        Result<ProjectResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(project.Id);
        result.Value.OrganizationId.ShouldBe(project.OrganizationId);
        result.Value.Name.ShouldBe("Over Ticketing");
        result.Value.Description.ShouldBe("Ticketing system API");
        result.Value.Status.ShouldBe(ProjectStatus.Active);
    }
}
