using Application.TicketCategories;
using Application.TicketCategories.GetByProject;
using Application.TicketCategories.GetGlobal;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Domain.Tickets;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.TicketCategories;

public sealed class GetTicketCategoriesQueryHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task GetGlobal_Should_ReturnOnlyGlobalActiveCategories_WhenIncludeArchivedFalse()
    {
        await using TestDbContext context = CreateDbContext();
        var globalActive = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Name = "Bug",
            Status = TicketCategoryStatus.Active
        };
        var globalArchived = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Name = "Deprecated",
            Status = TicketCategoryStatus.Archived
        };
        var projectCategory = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Project Specific",
            Status = TicketCategoryStatus.Active
        };
        context.TicketCategories.AddRange(globalActive, globalArchived, projectCategory);
        await context.SaveChangesAsync();

        var handler = new GetGlobalTicketCategoriesQueryHandler(context);
        var query = new GetGlobalTicketCategoriesQuery(IncludeArchived: false);

        Result<IReadOnlyCollection<TicketCategoryResponse>> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(1);
        result.Value.ShouldContain(c => c.Id == globalActive.Id);
    }

    [Fact]
    public async Task GetGlobal_Should_ReturnArchivedCategories_WhenIncludeArchivedTrue()
    {
        await using TestDbContext context = CreateDbContext();
        var globalActive = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Name = "Bug",
            Status = TicketCategoryStatus.Active
        };
        var globalArchived = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Name = "Old",
            Status = TicketCategoryStatus.Archived
        };
        context.TicketCategories.AddRange(globalActive, globalArchived);
        await context.SaveChangesAsync();

        var handler = new GetGlobalTicketCategoriesQueryHandler(context);
        var query = new GetGlobalTicketCategoriesQuery(IncludeArchived: true);

        Result<IReadOnlyCollection<TicketCategoryResponse>> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetByProject_Should_ReturnNotFound_WhenProjectDoesNotExist()
    {
        await using TestDbContext context = CreateDbContext();
        var handler = new GetProjectTicketCategoriesQueryHandler(context);
        var query = new GetProjectTicketCategoriesQuery(Guid.NewGuid());

        Result<IReadOnlyCollection<TicketCategoryResponse>> result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProjectErrors.NotFound(query.ProjectId));
    }

    [Fact]
    public async Task GetByProject_Should_ReturnUnionOfActiveGlobalAndActiveProjectCategories()
    {
        await using TestDbContext context = CreateDbContext();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Project Beta",
            Status = ProjectStatus.Active
        };
        var otherProject = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Project Gamma",
            Status = ProjectStatus.Active
        };
        context.Projects.AddRange(project, otherProject);

        var globalActive = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Name = "Global Bug",
            Status = TicketCategoryStatus.Active
        };
        var projectActive = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = "Custom Project Category",
            Status = TicketCategoryStatus.Active
        };
        var otherProjectCategory = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = otherProject.Id,
            Name = "Other Project Category",
            Status = TicketCategoryStatus.Active
        };
        var projectArchived = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = "Archived Custom",
            Status = TicketCategoryStatus.Archived
        };

        context.TicketCategories.AddRange(globalActive, projectActive, otherProjectCategory, projectArchived);
        await context.SaveChangesAsync();

        var handler = new GetProjectTicketCategoriesQueryHandler(context);
        var query = new GetProjectTicketCategoriesQuery(project.Id);

        Result<IReadOnlyCollection<TicketCategoryResponse>> result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldContain(c => c.Id == globalActive.Id);
        result.Value.ShouldContain(c => c.Id == projectActive.Id);
        result.Value.ShouldNotContain(c => c.Id == otherProjectCategory.Id);
        result.Value.ShouldNotContain(c => c.Id == projectArchived.Id);
    }
}
