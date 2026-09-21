using Application.UnitTests.Abstractions;
using Application.Users.ForgotPassword;
using Application.Users.ResetPassword;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class PasswordResetCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task ForgotPassword_Should_ReturnSuccess_EvenWhenUserDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        using UserManager<User> userManager = CreateUserManager();
        userManager.FindByEmailAsync(Arg.Any<string>()).Returns((User?)null);

        var handler = new ForgotPasswordCommandHandler(userManager, context);
        var command = new ForgotPasswordCommand("nonexistent@example.com");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await userManager.DidNotReceive().GeneratePasswordResetTokenAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task ForgotPassword_Should_GenerateTokenAndRaiseDomainEvent_WhenUserExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            UserName = "user@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        using UserManager<User> userManager = CreateUserManager();
        userManager.FindByEmailAsync("user@example.com").Returns(user);
        userManager.GeneratePasswordResetTokenAsync(user).Returns("generated-reset-token");

        var handler = new ForgotPasswordCommandHandler(userManager, context);
        var command = new ForgotPasswordCommand("user@example.com");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await userManager.Received(1).GeneratePasswordResetTokenAsync(user);

        UserPasswordResetRequestedDomainEvent? domainEvent = user.DomainEvents
            .OfType<UserPasswordResetRequestedDomainEvent>()
            .SingleOrDefault();

        domainEvent.ShouldNotBeNull();
        domainEvent.UserId.ShouldBe(user.Id);
        domainEvent.Email.ShouldBe("user@example.com");
        domainEvent.ResetToken.ShouldBe("generated-reset-token");
    }

    [Fact]
    public async Task ResetPassword_Should_ReturnFailure_WhenUserNotFound()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        using UserManager<User> userManager = CreateUserManager();
        userManager.FindByEmailAsync(Arg.Any<string>()).Returns((User?)null);

        var handler = new ResetPasswordCommandHandler(userManager, context);
        var command = new ResetPasswordCommand("unknown@example.com", "token", "NewPass123!", "NewPass123!");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.InvalidPasswordResetToken);
    }

    [Fact]
    public async Task ResetPassword_Should_ReturnFailure_WhenResetTokenIsInvalid()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            UserName = "user@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        using UserManager<User> userManager = CreateUserManager();
        userManager.FindByEmailAsync("user@example.com").Returns(user);
        userManager.ResetPasswordAsync(user, "bad-token", "NewPass123!")
            .Returns(IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidToken",
                Description = "Invalid token."
            }));

        var handler = new ResetPasswordCommandHandler(userManager, context);
        var command = new ResetPasswordCommand("user@example.com", "bad-token", "NewPass123!", "NewPass123!");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.InvalidPasswordResetToken);
    }

    [Fact]
    public async Task ResetPassword_Should_Succeed_AndRevokeActiveRefreshTokens()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            UserName = "user@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        context.Users.Add(user);

        var activeToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "existing-token",
            UserId = user.Id,
            ExpiresOnUtc = DateTime.UtcNow.AddDays(7)
        };
        context.RefreshTokens.Add(activeToken);
        await context.SaveChangesAsync();

        using UserManager<User> userManager = CreateUserManager();
        userManager.FindByEmailAsync("user@example.com").Returns(user);
        userManager.ResetPasswordAsync(user, "valid-token", "NewPass123!")
            .Returns(IdentityResult.Success);
        userManager.UpdateSecurityStampAsync(user).Returns(IdentityResult.Success);

        var handler = new ResetPasswordCommandHandler(userManager, context);
        var command = new ResetPasswordCommand("user@example.com", "valid-token", "NewPass123!", "NewPass123!");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await userManager.Received(1).UpdateSecurityStampAsync(user);

        bool tokensExist = await context.RefreshTokens.AnyAsync(rt => rt.UserId == user.Id);
        tokensExist.ShouldBeFalse();
    }
}
