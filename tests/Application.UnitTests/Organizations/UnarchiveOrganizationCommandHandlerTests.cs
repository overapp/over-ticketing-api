using Application.Organizations.Unarchive;
using Application.UnitTests.Abstractions;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Organizations;

public sealed class UnarchiveOrganizationCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenOrganizationDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var command = new UnarchiveOrganizationCommand(Guid.NewGuid());
        var handler = new UnarchiveOrganizationCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrganizationErrors.NotFound(command.OrganizationId));
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyActive_WhenOrganizationIsActive()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        Organization organization = CreateOrganization(OrganizationStatus.Active);
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        var command = new UnarchiveOrganizationCommand(organization.Id);
        var handler = new UnarchiveOrganizationCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrganizationErrors.AlreadyActive(command.OrganizationId));
    }

    [Fact]
    public async Task Handle_Should_UnarchiveOrganizationAndRaiseDomainEvent_WhenOrganizationIsArchived()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        Organization organization = CreateOrganization(OrganizationStatus.Archived);
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        var command = new UnarchiveOrganizationCommand(organization.Id);
        var handler = new UnarchiveOrganizationCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        organization.Status.ShouldBe(OrganizationStatus.Active);
        organization.DomainEvents.ShouldContain(
            domainEvent => domainEvent is OrganizationUnarchivedDomainEvent);
    }

    private static Organization CreateOrganization(OrganizationStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Overapp",
        Logo = "https://example.com/logo.png",
        Status = status
    };
}
