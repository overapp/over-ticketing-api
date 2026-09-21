using Application.UnitTests.Abstractions;
using Application.Users.Create;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class CreateUserCommandHandlerTests : BaseHandlerTest
{
    private static CreateUserCommand Command =>
        new("newuser@example.com", "Mario", "Rossi", "Password123!", [RoleNames.Support]);

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenEmailIsNotUnique()
    {
        // Arrange
        using UserManager<User> userManager = CreateUserManager();
        userManager.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateEmail",
                Description = "Email already taken."
            }));

        var handler = new CreateUserCommandHandler(userManager);

        // Act
        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.DuplicateEmail);
    }

    [Fact]
    public async Task Handle_Should_CreateUserWithBaseAndSpecifiedRoles_AndRaiseDomainEvent()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        using UserManager<User> userManager = CreateUserManager();
        userManager.CreateAsync(Arg.Any<User>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                User user = callInfo.ArgAt<User>(0);
                context.Users.Add(user);
                context.SaveChanges();

                return IdentityResult.Success;
            });

        userManager.AddToRolesAsync(Arg.Any<User>(), Arg.Any<IEnumerable<string>>())
            .Returns(IdentityResult.Success);

        var handler = new CreateUserCommandHandler(userManager);

        // Act
        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        User user = await context.Users.SingleAsync(u => u.Id == result.Value);
        user.Email.ShouldBe(Command.Email);
        user.UserName.ShouldBe(Command.Email);
        user.EmailConfirmed.ShouldBeTrue();
        user.DomainEvents.ShouldContain(e => e is UserCreatedDomainEvent);

        await userManager.Received(1).AddToRolesAsync(
            Arg.Is<User>(u => u.Id == result.Value),
            Arg.Is<IEnumerable<string>>(roles =>
                roles.Contains(RoleNames.User) && roles.Contains(RoleNames.Support)));
    }
}
