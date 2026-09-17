using Application.Organizations.Update;
using Application.UnitTests.Abstractions;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Organizations;

public sealed class UpdateOrganizationCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenOrganizationDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var command = new UpdateOrganizationCommand(
            Guid.NewGuid(),
            "Overapp",
            "https://example.com/logo.png");
        var handler = new UpdateOrganizationCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(OrganizationErrors.NotFound(command.OrganizationId));
    }

    [Fact]
    public async Task Handle_Should_UpdateNameAndLogoAndPreserveStatus_WhenOrganizationExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Previous name",
            Logo = "https://example.com/previous-logo.png",
            Status = OrganizationStatus.Archived
        };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();

        var command = new UpdateOrganizationCommand(
            organization.Id,
            "Updated name",
            "https://example.com/updated-logo.png");
        var handler = new UpdateOrganizationCommandHandler(context);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Organization updatedOrganization = await context.Organizations.SingleAsync();
        updatedOrganization.Name.ShouldBe(command.Name);
        updatedOrganization.Logo.ShouldBe(command.Logo);
        updatedOrganization.Status.ShouldBe(OrganizationStatus.Archived);

        OrganizationUpdatedDomainEvent? domainEvent = updatedOrganization.DomainEvents
            .OfType<OrganizationUpdatedDomainEvent>()
            .SingleOrDefault();
        domainEvent.ShouldNotBeNull();
        domainEvent.OrganizationId.ShouldBe(command.OrganizationId);
    }
}
