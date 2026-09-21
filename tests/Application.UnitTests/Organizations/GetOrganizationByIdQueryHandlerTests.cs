using Application.Organizations.GetById;
using Application.UnitTests.Abstractions;
using Domain.Organizations;
using SharedKernel;

namespace Application.UnitTests.Organizations;

public sealed class GetOrganizationByIdQueryHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenOrganizationDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new GetOrganizationByIdQueryHandler(context);
        var query = new GetOrganizationByIdQuery(Guid.NewGuid());

        // Act
        Result<OrganizationResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Organizations.NotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnOrganization_WhenOrganizationExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Overapp",
            Logo = "https://example.com/logo.png",
            Status = OrganizationStatus.Active
        };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();

        var handler = new GetOrganizationByIdQueryHandler(context);
        var query = new GetOrganizationByIdQuery(organization.Id);

        // Act
        Result<OrganizationResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(organization.Id);
        result.Value.Name.ShouldBe("Overapp");
        result.Value.Logo.ShouldBe("https://example.com/logo.png");
        result.Value.Status.ShouldBe(OrganizationStatus.Active);
    }
}
