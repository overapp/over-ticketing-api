using Application.Abstractions.Authentication;
using Application.Users;
using Application.Users.Login;
using Application.UnitTests.Abstractions;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class LoginUserCommandHandlerTests : BaseHandlerTest
{
    private const string Email = "test@example.com";
    private const string Password = "Password123!";

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        using UserManager<User> userManager = CreateUserManager();
        userManager.FindByEmailAsync(Email).Returns((User?)null);

        SignInManager<User> signInManager = CreateSignInManager(userManager);

        var handler = new LoginUserCommandHandler(
            userManager,
            signInManager,
            context,
            Substitute.For<ITokenProvider>(),
            Substitute.For<IDateTimeProvider>());

        // Act
        Result<AccessTokensResponse> result = await handler.Handle(
            new LoginUserCommand(Email, Password),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.NotFoundByEmail);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenPasswordIsInvalid()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        User user = await SeedUserAsync(context);

        using UserManager<User> userManager = CreateUserManager();
        userManager.FindByEmailAsync(Email).Returns(user);

        SignInManager<User> signInManager = CreateSignInManager(userManager);
        signInManager.CheckPasswordSignInAsync(user, Password, true).Returns(SignInResult.Failed);

        var handler = new LoginUserCommandHandler(
            userManager,
            signInManager,
            context,
            Substitute.For<ITokenProvider>(),
            Substitute.For<IDateTimeProvider>());

        // Act
        Result<AccessTokensResponse> result = await handler.Handle(
            new LoginUserCommand(Email, Password),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.NotFoundByEmail);
    }

    [Fact]
    public async Task Handle_Should_ReturnTokensAndPersistRefreshToken_WhenCredentialsAreValid()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        User user = await SeedUserAsync(context);

        using UserManager<User> userManager = CreateUserManager();
        userManager.FindByEmailAsync(Email).Returns(user);
        userManager.GetRolesAsync(user).Returns((IList<string>)[]);

        SignInManager<User> signInManager = CreateSignInManager(userManager);
        signInManager.CheckPasswordSignInAsync(user, Password, true).Returns(SignInResult.Success);

        ITokenProvider tokenProvider = Substitute.For<ITokenProvider>();
        tokenProvider.Create(user, Arg.Any<IEnumerable<string>>()).Returns("access-token");
        tokenProvider.GenerateRefreshToken().Returns("refresh-token");

        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var handler = new LoginUserCommandHandler(userManager, signInManager, context, tokenProvider, dateTimeProvider);

        // Act
        Result<AccessTokensResponse> result = await handler.Handle(
            new LoginUserCommand(Email, Password),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
        result.Value.RefreshToken.ShouldBe("refresh-token");

        RefreshToken refreshToken = await context.RefreshTokens.SingleAsync();
        refreshToken.Token.ShouldBe("refresh-token");
        refreshToken.ExpiresOnUtc.ShouldBeGreaterThan(dateTimeProvider.UtcNow);
    }

    private static async Task<User> SeedUserAsync(TestDbContext context)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = Email,
            FirstName = "Test",
            LastName = "User"
        };

        context.Users.Add(user);

        await context.SaveChangesAsync();

        return user;
    }
}
