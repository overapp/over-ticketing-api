using Application.Common;
using Application.Organizations;
using Application.Organizations.Get;
using Application.UnitTests.Abstractions;
using Domain.Organizations;
using SharedKernel;

namespace Application.UnitTests.Organizations;

public sealed class GetOrganizationsQueryHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnPagedOrganizations_WhenOrganizationsExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var org1 = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Alpha Corp",
            Logo = "https://example.com/alpha.png",
            Status = OrganizationStatus.Active
        };
        var org2 = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Beta Ltd",
            Logo = "https://example.com/beta.png",
            Status = OrganizationStatus.Active
        };

        context.Organizations.AddRange(org1, org2);
        await context.SaveChangesAsync();

        var handler = new GetOrganizationsQueryHandler(context);
        var query = new GetOrganizationsQuery(Page: 1, PageSize: 10);

        // Act
        Result<PagedResponse<OrganizationResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Items.Count.ShouldBe(2);

        OrganizationResponse first = result.Value.Items.First(o => o.Id == org1.Id);
        first.Name.ShouldBe("Alpha Corp");
        first.Logo.ShouldBe("https://example.com/alpha.png");
        first.Status.ShouldBe(OrganizationStatus.Active);
    }

    [Fact]
    public async Task Handle_Should_FilterBySearchTerm()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var org1 = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Contoso Solutions",
            Logo = "https://example.com/contoso.png",
            Status = OrganizationStatus.Active
        };
        var org2 = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Fabrikam Industries",
            Logo = "https://example.com/fabrikam.png",
            Status = OrganizationStatus.Active
        };

        context.Organizations.AddRange(org1, org2);
        await context.SaveChangesAsync();

        var handler = new GetOrganizationsQueryHandler(context);
        var query = new GetOrganizationsQuery(Page: 1, PageSize: 10, SearchTerm: "Contoso");

        // Act
        Result<PagedResponse<OrganizationResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.First().Id.ShouldBe(org1.Id);
    }

    [Fact]
    public async Task Handle_Should_FilterByStatus()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var activeOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Active Org",
            Logo = "https://example.com/active.png",
            Status = OrganizationStatus.Active
        };
        var archivedOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Archived Org",
            Logo = "https://example.com/archived.png",
            Status = OrganizationStatus.Archived
        };

        context.Organizations.AddRange(activeOrg, archivedOrg);
        await context.SaveChangesAsync();

        var handler = new GetOrganizationsQueryHandler(context);
        var query = new GetOrganizationsQuery(Page: 1, PageSize: 10, Status: OrganizationStatus.Archived);

        // Act
        Result<PagedResponse<OrganizationResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.First().Id.ShouldBe(archivedOrg.Id);
    }

    [Fact]
    public async Task Handle_Should_ApplyPagination()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        for (int i = 1; i <= 5; i++)
        {
            context.Organizations.Add(new Organization
            {
                Id = Guid.NewGuid(),
                Name = $"Org {i:D2}",
                Logo = $"https://example.com/{i}.png",
                Status = OrganizationStatus.Active
            });
        }
        await context.SaveChangesAsync();

        var handler = new GetOrganizationsQueryHandler(context);
        var query = new GetOrganizationsQuery(Page: 2, PageSize: 2);

        // Act
        Result<PagedResponse<OrganizationResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(5);
        result.Value.Items.Count.ShouldBe(2);
        result.Value.Page.ShouldBe(2);
        result.Value.PageSize.ShouldBe(2);
        result.Value.TotalPages.ShouldBe(3);
        result.Value.HasPreviousPage.ShouldBeTrue();
        result.Value.HasNextPage.ShouldBeTrue();
    }
}
