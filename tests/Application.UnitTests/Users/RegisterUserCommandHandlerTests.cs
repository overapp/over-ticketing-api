using Application.Users.Register;
using Application.UnitTests.Abstractions;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class RegisterUserCommandHandlerTests : BaseHandlerTest
{
    private static RegisterUserCommand Command =>
        new("test@example.com", "Test", "User", "Password123!");

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

        var handler = new RegisterUserCommandHandler(userManager);

        // Act
        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.DuplicateEmail);
    }

    [Fact]
    public async Task Handle_Should_CreateUserAndRaiseDomainEvent_WhenValid()
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
        userManager.AddToRoleAsync(Arg.Any<User>(), RoleNames.User)
            .Returns(IdentityResult.Success);

        var handler = new RegisterUserCommandHandler(userManager);

        // Act
        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        User user = await context.Users.SingleAsync(u => u.Id == result.Value);
        user.Email.ShouldBe(Command.Email);
        user.DomainEvents.ShouldContain(domainEvent => domainEvent is UserRegisteredDomainEvent);
        await userManager.Received(1).AddToRoleAsync(user, RoleNames.User);
    }
}
