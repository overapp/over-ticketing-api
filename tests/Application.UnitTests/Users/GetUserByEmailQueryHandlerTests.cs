using Application.Abstractions.Authentication;
using Application.UnitTests.Abstractions;
using Application.Users.GetByEmail;
using Domain.Users;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class GetUserByEmailQueryHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly Guid _userId = Guid.NewGuid();

    public GetUserByEmailQueryHandlerTests()
    {
        _userContext.UserId.Returns(_userId);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new GetUserByEmailQueryHandler(context, _userContext);
        var query = new GetUserByEmailQuery("missing@test.com");

        // Act
        Result<UserResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.NotFoundByEmail);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserDiffersFromUserContext()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "other@test.com",
            FirstName = "Other",
            LastName = "User"
        };
        context.Users.Add(otherUser);
        await context.SaveChangesAsync();

        var handler = new GetUserByEmailQueryHandler(context, _userContext);
        var query = new GetUserByEmailQuery("other@test.com");

        // Act
        Result<UserResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.Unauthorized());
    }

    [Fact]
    public async Task Handle_Should_ReturnUser_WhenUserMatchesUserContext()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var currentUser = new User
        {
            Id = _userId,
            Email = "me@test.com",
            FirstName = "John",
            LastName = "Doe"
        };
        context.Users.Add(currentUser);
        await context.SaveChangesAsync();

        var handler = new GetUserByEmailQueryHandler(context, _userContext);
        var query = new GetUserByEmailQuery("me@test.com");

        // Act
        Result<UserResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(_userId);
        result.Value.Email.ShouldBe("me@test.com");
        result.Value.FirstName.ShouldBe("John");
        result.Value.LastName.ShouldBe("Doe");
    }
}
