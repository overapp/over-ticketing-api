using Application.Abstractions.Authentication;
using Application.UnitTests.Abstractions;
using Application.Users.AdminResetPassword;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class AdminResetPasswordCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        using UserManager<User> userManager = CreateUserManager();
        var userId = Guid.NewGuid();

        IPasswordGenerator passwordGenerator = Substitute.For<IPasswordGenerator>();

        userManager.FindByIdAsync(userId.ToString()).Returns((User?)null);

        var handler = new AdminResetPasswordCommandHandler(userManager, passwordGenerator, context);
        var command = new AdminResetPasswordCommand(userId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(UserErrors.NotFound(userId).Code);
    }

    [Fact]
    public async Task Handle_Should_GenerateTemporaryPassword_SetMustChangePassword_RevokeTokens_AndRaiseEvent()
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
            MustChangePassword = false
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
        IPasswordGenerator passwordGenerator = Substitute.For<IPasswordGenerator>();
        passwordGenerator.Generate(Arg.Any<int>()).Returns("NewTempPass123!");

        userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        userManager.GeneratePasswordResetTokenAsync(user).Returns("reset-token");
        userManager.ResetPasswordAsync(user, "reset-token", "NewTempPass123!").Returns(IdentityResult.Success);
        userManager.UpdateAsync(user).Returns(IdentityResult.Success);
        userManager.UpdateSecurityStampAsync(user).Returns(IdentityResult.Success);

        var handler = new AdminResetPasswordCommandHandler(userManager, passwordGenerator, context);
        var command = new AdminResetPasswordCommand(user.Id);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        user.MustChangePassword.ShouldBeTrue();

        UserTemporaryPasswordAssignedDomainEvent? domainEvent = user.DomainEvents
            .OfType<UserTemporaryPasswordAssignedDomainEvent>()
            .SingleOrDefault();

        domainEvent.ShouldNotBeNull();
        domainEvent.UserId.ShouldBe(user.Id);
        domainEvent.TemporaryPassword.ShouldBe("NewTempPass123!");

        await userManager.Received(1).UpdateSecurityStampAsync(user);

        bool hasTokens = await context.RefreshTokens.AnyAsync(rt => rt.UserId == user.Id);
        hasTokens.ShouldBeFalse();
    }
}
