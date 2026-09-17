using Application.Organizations.Create;
using Application.UnitTests.Abstractions;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Organizations;

public sealed class CreateOrganizationCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_CreateActiveOrganizationAndRaiseDomainEvent_WhenCommandIsValid()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var command = new CreateOrganizationCommand("Overapp", "https://example.com/logo.png");
        var handler = new CreateOrganizationCommandHandler(context);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        Organization organization = await context.Organizations.SingleAsync();
        organization.Id.ShouldBe(result.Value);
        organization.Name.ShouldBe(command.Name);
        organization.Logo.ShouldBe(command.Logo);
        organization.Status.ShouldBe(OrganizationStatus.Active);
        organization.DomainEvents.ShouldContain(domainEvent => domainEvent is OrganizationCreatedDomainEvent);
    }
}
