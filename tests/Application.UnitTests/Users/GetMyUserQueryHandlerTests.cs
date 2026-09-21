using Application.Abstractions.Authentication;
using Application.UnitTests.Abstractions;
using Application.Users.GetById;
using Application.Users.GetMe;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class GetMyUserQueryHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnCurrentUserProfile()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var currentUserId = Guid.NewGuid();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(currentUserId);

        var role = new Role(RoleNames.User) { Id = Guid.NewGuid() };
        context.Roles.Add(role);

        var user = new User
        {
            Id = currentUserId,
            Email = "me@example.com",
            FirstName = "Current",
            LastName = "User",
            EmailConfirmed = true
        };
        context.Users.Add(user);
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id });

        await context.SaveChangesAsync();

        var handler = new GetMyUserQueryHandler(context, userContext);
        var query = new GetMyUserQuery();

        // Act
        Result<UserResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(currentUserId);
        result.Value.Email.ShouldBe("me@example.com");
        result.Value.Roles.ShouldContain(RoleNames.User);
    }
}
