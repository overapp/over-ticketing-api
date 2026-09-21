using Application.Abstractions.Authentication;
using Application.UnitTests.Abstractions;
using Application.Users.ChangePassword;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class ChangePasswordCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        using UserManager<User> userManager = CreateUserManager();
        var userId = Guid.NewGuid();

        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(userId);

        userManager.FindByIdAsync(userId.ToString()).Returns((User?)null);

        var handler = new ChangePasswordCommandHandler(userManager, userContext, context);
        var command = new ChangePasswordCommand("OldPass123!", "NewPass123!", "NewPass123!");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(UserErrors.NotFound(userId).Code);
    }

    [Fact]
    public async Task Handle_Should_ReturnLockedOut_WhenUserIsLockedOut()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        using UserManager<User> userManager = CreateUserManager();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(user.Id);

        userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        userManager.IsLockedOutAsync(user).Returns(true);

        var handler = new ChangePasswordCommandHandler(userManager, userContext, context);
        var command = new ChangePasswordCommand("OldPass123!", "NewPass123!", "NewPass123!");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.UserLockedOut);
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidCurrentPassword_AndCallAccessFailed_WhenPasswordMismatch()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        using UserManager<User> userManager = CreateUserManager();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(user.Id);

        userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        userManager.IsLockedOutAsync(user).Returns(false);
        userManager.ChangePasswordAsync(user, "WrongPass123!", "NewPass123!")
            .Returns(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordMismatch",
                Description = "Incorrect password."
            }));

        var handler = new ChangePasswordCommandHandler(userManager, userContext, context);
        var command = new ChangePasswordCommand("WrongPass123!", "NewPass123!", "NewPass123!");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.InvalidCurrentPassword);
        await userManager.Received(1).AccessFailedAsync(user);
    }

    [Fact]
    public async Task Handle_Should_Succeed_ResetMustChangePassword_RevokeTokens_AndRaiseDomainEvent()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            MustChangePassword = true
        };
        context.Users.Add(user);

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "active-token",
            UserId = user.Id,
            ExpiresOnUtc = DateTime.UtcNow.AddDays(7)
        };
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        using UserManager<User> userManager = CreateUserManager();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(user.Id);

        userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        userManager.IsLockedOutAsync(user).Returns(false);
        userManager.ChangePasswordAsync(user, "CurrentPass123!", "NewPass123!")
            .Returns(IdentityResult.Success);
        userManager.UpdateAsync(user).Returns(IdentityResult.Success);
        userManager.UpdateSecurityStampAsync(user).Returns(IdentityResult.Success);

        var handler = new ChangePasswordCommandHandler(userManager, userContext, context);
        var command = new ChangePasswordCommand("CurrentPass123!", "NewPass123!", "NewPass123!");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.MustChangePassword.ShouldBeFalse();
        user.DomainEvents.ShouldContain(e => e is UserPasswordChangedDomainEvent);

        await userManager.Received(1).ResetAccessFailedCountAsync(user);
        await userManager.Received(1).UpdateSecurityStampAsync(user);

        bool hasTokens = await context.RefreshTokens.AnyAsync(rt => rt.UserId == user.Id);
        hasTokens.ShouldBeFalse();
    }
}
