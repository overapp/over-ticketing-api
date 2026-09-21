using Application.Common;
using Application.UnitTests.Abstractions;
using Application.Users.Get;
using Application.Users.GetById;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class GetUsersQueryHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnPagedUsers_WithRoles()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var roleUser = new Role(RoleNames.User) { Id = Guid.NewGuid(), NormalizedName = RoleNames.User.ToUpperInvariant() };
        var roleAdmin = new Role(RoleNames.Admin) { Id = Guid.NewGuid(), NormalizedName = RoleNames.Admin.ToUpperInvariant() };
        context.Roles.AddRange(roleUser, roleAdmin);

        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "alfa@example.com",
            FirstName = "Alfa",
            LastName = "Rossi",
            EmailConfirmed = true
        };
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "beta@example.com",
            FirstName = "Beta",
            LastName = "Bianchi",
            EmailConfirmed = false
        };

        context.Users.AddRange(user1, user2);
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user1.Id, RoleId = roleAdmin.Id });
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user1.Id, RoleId = roleUser.Id });
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user2.Id, RoleId = roleUser.Id });

        await context.SaveChangesAsync();

        var handler = new GetUsersQueryHandler(context);
        var query = new GetUsersQuery(Page: 1, PageSize: 10);

        // Act
        Result<PagedResponse<UserResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Items.Count.ShouldBe(2);

        UserResponse firstUser = result.Value.Items.First(u => u.Id == user1.Id);
        firstUser.Email.ShouldBe("alfa@example.com");
        firstUser.Roles.ShouldContain(RoleNames.Admin);
        firstUser.Roles.ShouldContain(RoleNames.User);
        firstUser.EmailConfirmed.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_Should_FilterBySearchTerm()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "searchmatch@example.com",
            FirstName = "Mario",
            LastName = "Verdi"
        };
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "nomatch@example.com",
            FirstName = "Luigi",
            LastName = "Neri"
        };

        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var handler = new GetUsersQueryHandler(context);
        var query = new GetUsersQuery(Page: 1, PageSize: 10, SearchTerm: "searchmatch");

        // Act
        Result<PagedResponse<UserResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.First().Id.ShouldBe(user1.Id);
    }

    [Fact]
    public async Task Handle_Should_NormalizePaging_WhenNegativeOrExceedsMax()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new GetUsersQueryHandler(context);
        var query = new GetUsersQuery(Page: -1, PageSize: 500);

        // Act
        Result<PagedResponse<UserResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(100);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_WhenFilterRoleDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new GetUsersQueryHandler(context);
        var query = new GetUsersQuery(Page: 1, PageSize: 10, Role: "NonExistentRole");

        // Act
        Result<PagedResponse<UserResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(0);
        result.Value.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_Should_FilterByRole_WhenRoleExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var supportRole = new Role(RoleNames.Support) { Id = Guid.NewGuid(), NormalizedName = RoleNames.Support.ToUpperInvariant() };
        var userRole = new Role(RoleNames.User) { Id = Guid.NewGuid(), NormalizedName = RoleNames.User.ToUpperInvariant() };
        context.Roles.AddRange(supportRole, userRole);

        var supportUser = new User { Id = Guid.NewGuid(), Email = "supp@test.com", FirstName = "Supp", LastName = "User" };
        var regularUser = new User { Id = Guid.NewGuid(), Email = "reg@test.com", FirstName = "Reg", LastName = "User" };
        context.Users.AddRange(supportUser, regularUser);

        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = supportUser.Id, RoleId = supportRole.Id });
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = regularUser.Id, RoleId = userRole.Id });
        await context.SaveChangesAsync();

        var handler = new GetUsersQueryHandler(context);
        var query = new GetUsersQuery(Page: 1, PageSize: 10, Role: RoleNames.Support);

        // Act
        Result<PagedResponse<UserResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Items.First().Id.ShouldBe(supportUser.Id);
    }
}
