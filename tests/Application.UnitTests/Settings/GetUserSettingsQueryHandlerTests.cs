using Application.Abstractions.Authentication;
using Application.Settings.Get;
using Application.UnitTests.Abstractions;
using Domain.Users;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Settings;

public sealed class GetUserSettingsQueryHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);

        var handler = new GetUserSettingsQueryHandler(context, _userContext);
        var query = new GetUserSettingsQuery();

        // Act
        Result<UserSettingsResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(UserErrors.NotFound(userId).Code);
    }

    [Fact]
    public async Task Handle_Should_ReturnDefaultSettings_WhenUserSettingsRecordDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        _userContext.UserId.Returns(user.Id);

        var handler = new GetUserSettingsQueryHandler(context, _userContext);
        var query = new GetUserSettingsQuery();

        // Act
        Result<UserSettingsResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.EmailNotifications.NotifyOnTicketCreated.ShouldBeTrue();
        result.Value.EmailNotifications.NotifyOnTicketReply.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnCustomSettings_WhenUserSettingsRecordExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        var settings = new UserSettings
        {
            UserId = user.Id,
            NotifyOnTicketCreated = false,
            NotifyOnTicketReply = false
        };

        context.Users.Add(user);
        context.UserSettings.Add(settings);
        await context.SaveChangesAsync();

        _userContext.UserId.Returns(user.Id);

        var handler = new GetUserSettingsQueryHandler(context, _userContext);
        var query = new GetUserSettingsQuery();

        // Act
        Result<UserSettingsResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.EmailNotifications.NotifyOnTicketCreated.ShouldBeFalse();
        result.Value.EmailNotifications.NotifyOnTicketReply.ShouldBeFalse();
    }
}
