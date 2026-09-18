using Application.Projects.Create;
using Application.UnitTests.Abstractions;
using Domain.Organizations;
using Domain.Projects;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Projects;

public sealed class CreateProjectCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnOrganizationNotFound_WhenOrganizationDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var command = new CreateProjectCommand(Guid.NewGuid(), "Overapp API", "Ticketing API");
        var handler = new CreateProjectCommandHandler(context);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrganizationErrors.NotFound(command.OrganizationId));
    }

    [Fact]
    public async Task Handle_Should_CreateActiveProjectForOrganizationAndRaiseDomainEvent_WhenOrganizationExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var organization = new Organization { Id = Guid.NewGuid(), Name = "Overapp", Logo = "logo" };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        var command = new CreateProjectCommand(organization.Id, "Overapp API", "Ticketing API");
        var handler = new CreateProjectCommandHandler(context);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Project project = await context.Projects.SingleAsync();
        project.Id.ShouldBe(result.Value);
        project.OrganizationId.ShouldBe(organization.Id);
        project.Name.ShouldBe(command.Name);
        project.Description.ShouldBe(command.Description);
        project.Status.ShouldBe(ProjectStatus.Active);
        project.DomainEvents.ShouldContain(domainEvent => domainEvent is ProjectCreatedDomainEvent);
    }
}
