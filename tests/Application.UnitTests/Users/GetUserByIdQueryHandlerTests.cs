using Application.UnitTests.Abstractions;
using Application.Users.GetById;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class GetUserByIdQueryHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new GetUserByIdQueryHandler(context);
        var query = new GetUserByIdQuery(Guid.NewGuid());

        // Act
        Result<UserResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.NotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnUserWithRoles_WhenUserExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var role = new Role(RoleNames.Admin) { Id = Guid.NewGuid() };
        context.Roles.Add(role);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };
        context.Users.Add(user);
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id });

        await context.SaveChangesAsync();

        var handler = new GetUserByIdQueryHandler(context);
        var query = new GetUserByIdQuery(user.Id);

        // Act
        Result<UserResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(user.Id);
        result.Value.Email.ShouldBe("test@example.com");
        result.Value.Roles.ShouldContain(RoleNames.Admin);
        result.Value.EmailConfirmed.ShouldBeTrue();
    }
}
