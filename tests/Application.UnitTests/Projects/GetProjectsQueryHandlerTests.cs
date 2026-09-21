using Application.Common;
using Application.Projects.Get;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using SharedKernel;

namespace Application.UnitTests.Projects;

public sealed class GetProjectsQueryHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnPagedProjects_WhenProjectsExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var orgId = Guid.NewGuid();

        var project1 = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Name = "Alpha Project",
            Description = "Alpha Description",
            Status = ProjectStatus.Active
        };
        var project2 = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Name = "Beta Project",
            Description = "Beta Description",
            Status = ProjectStatus.Active
        };

        context.Projects.AddRange(project1, project2);
        await context.SaveChangesAsync();

        var handler = new GetProjectsQueryHandler(context);
        var query = new GetProjectsQuery(Page: 1, PageSize: 10);

        // Act
        Result<PagedResponse<ProjectResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Items.Count.ShouldBe(2);

        ProjectResponse first = result.Value.Items.First(p => p.Id == project1.Id);
        first.Name.ShouldBe("Alpha Project");
        first.Description.ShouldBe("Alpha Description");
        first.OrganizationId.ShouldBe(orgId);
        first.Status.ShouldBe(ProjectStatus.Active);
    }

    [Fact]
    public async Task Handle_Should_FilterBySearchTerm()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var project1 = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Billing Service",
            Description = "Handles billing",
            Status = ProjectStatus.Active
        };
        var project2 = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Auth Service",
            Description = "Handles authentication",
            Status = ProjectStatus.Active
        };

        context.Projects.AddRange(project1, project2);
        await context.SaveChangesAsync();

        var handler = new GetProjectsQueryHandler(context);
        var query = new GetProjectsQuery(Page: 1, PageSize: 10, SearchTerm: "billing");

        // Act
        Result<PagedResponse<ProjectResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.First().Id.ShouldBe(project1.Id);
    }

    [Fact]
    public async Task Handle_Should_FilterByOrganizationId()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var targetOrgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();

        var project1 = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = targetOrgId,
            Name = "Target Org Project",
            Description = "Target Description",
            Status = ProjectStatus.Active
        };
        var project2 = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = otherOrgId,
            Name = "Other Org Project",
            Description = "Other Description",
            Status = ProjectStatus.Active
        };

        context.Projects.AddRange(project1, project2);
        await context.SaveChangesAsync();

        var handler = new GetProjectsQueryHandler(context);
        var query = new GetProjectsQuery(Page: 1, PageSize: 10, OrganizationId: targetOrgId);

        // Act
        Result<PagedResponse<ProjectResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.First().Id.ShouldBe(project1.Id);
    }

    [Fact]
    public async Task Handle_Should_FilterByStatus()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var activeProject = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Active Project",
            Description = "Active Description",
            Status = ProjectStatus.Active
        };
        var archivedProject = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Archived Project",
            Description = "Archived Description",
            Status = ProjectStatus.Archived
        };

        context.Projects.AddRange(activeProject, archivedProject);
        await context.SaveChangesAsync();

        var handler = new GetProjectsQueryHandler(context);
        var query = new GetProjectsQuery(Page: 1, PageSize: 10, Status: ProjectStatus.Archived);

        // Act
        Result<PagedResponse<ProjectResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.First().Id.ShouldBe(archivedProject.Id);
    }

    [Fact]
    public async Task Handle_Should_ApplyPagination()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var orgId = Guid.NewGuid();

        for (int i = 1; i <= 5; i++)
        {
            context.Projects.Add(new Project
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                Name = $"Project {i:D2}",
                Description = $"Description {i}",
                Status = ProjectStatus.Active
            });
        }
        await context.SaveChangesAsync();

        var handler = new GetProjectsQueryHandler(context);
        var query = new GetProjectsQuery(Page: 2, PageSize: 2);

        // Act
        Result<PagedResponse<ProjectResponse>> result = await handler.Handle(query, CancellationToken.None);

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
