using Application.Organizations.Archive;
using Application.UnitTests.Abstractions;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Organizations;

public sealed class ArchiveOrganizationCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenOrganizationDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var command = new ArchiveOrganizationCommand(Guid.NewGuid());
        var handler = new ArchiveOrganizationCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrganizationErrors.NotFound(command.OrganizationId));
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyArchived_WhenOrganizationIsArchived()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        Organization organization = CreateOrganization(OrganizationStatus.Archived);
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        var command = new ArchiveOrganizationCommand(organization.Id);
        var handler = new ArchiveOrganizationCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrganizationErrors.AlreadyArchived(command.OrganizationId));
    }

    [Fact]
    public async Task Handle_Should_ArchiveOrganizationAndRaiseDomainEvent_WhenOrganizationIsActive()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        Organization organization = CreateOrganization(OrganizationStatus.Active);
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        var command = new ArchiveOrganizationCommand(organization.Id);
        var handler = new ArchiveOrganizationCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        organization.Status.ShouldBe(OrganizationStatus.Archived);
        organization.DomainEvents.ShouldContain(
            domainEvent => domainEvent is OrganizationArchivedDomainEvent);
    }

    private static Organization CreateOrganization(OrganizationStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Overapp",
        Logo = "https://example.com/logo.png",
        Status = status
    };
}
