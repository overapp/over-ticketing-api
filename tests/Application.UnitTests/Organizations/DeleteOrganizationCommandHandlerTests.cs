using Application.Organizations.Delete;
using Application.UnitTests.Abstractions;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Organizations;

public sealed class DeleteOrganizationCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenOrganizationDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var command = new DeleteOrganizationCommand(Guid.NewGuid());
        var handler = new DeleteOrganizationCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrganizationErrors.NotFound(command.OrganizationId));
    }

    [Fact]
    public async Task Handle_Should_DeleteOrganizationAndRaiseDomainEvent_WhenOrganizationExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Overapp",
            Logo = "https://example.com/logo.png"
        };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();

        var command = new DeleteOrganizationCommand(organization.Id);
        var handler = new DeleteOrganizationCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        (await context.Organizations.AnyAsync()).ShouldBeFalse();
        organization.DomainEvents.ShouldContain(
            domainEvent => domainEvent is OrganizationDeletedDomainEvent);
    }
}
