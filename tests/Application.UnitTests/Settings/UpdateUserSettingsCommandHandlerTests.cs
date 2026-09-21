using Application.Abstractions.Authentication;
using Application.Settings.Update;
using Application.UnitTests.Abstractions;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Settings;

public sealed class UpdateUserSettingsCommandHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);

        var handler = new UpdateUserSettingsCommandHandler(context, _userContext);
        var command = new UpdateUserSettingsCommand(
            new UpdateEmailNotificationSettings(NotifyOnTicketCreated: false, NotifyOnTicketReply: false));

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(UserErrors.NotFound(userId).Code);
    }

    [Fact]
    public async Task Handle_Should_InsertSettings_WhenRecordDoesNotExist()
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

        var handler = new UpdateUserSettingsCommandHandler(context, _userContext);
        var command = new UpdateUserSettingsCommand(
            new UpdateEmailNotificationSettings(NotifyOnTicketCreated: false, NotifyOnTicketReply: false));

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        UserSettings? settings = await context.UserSettings.SingleOrDefaultAsync(s => s.UserId == user.Id);
        settings.ShouldNotBeNull();
        settings.NotifyOnTicketCreated.ShouldBeFalse();
        settings.NotifyOnTicketReply.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_Should_UpdateSettings_WhenRecordAlreadyExists()
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
        var initialSettings = new UserSettings
        {
            UserId = user.Id,
            NotifyOnTicketCreated = false,
            NotifyOnTicketReply = false
        };

        context.Users.Add(user);
        context.UserSettings.Add(initialSettings);
        await context.SaveChangesAsync();

        _userContext.UserId.Returns(user.Id);

        var handler = new UpdateUserSettingsCommandHandler(context, _userContext);
        var command = new UpdateUserSettingsCommand(
            new UpdateEmailNotificationSettings(NotifyOnTicketCreated: true, NotifyOnTicketReply: true));

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        UserSettings? settings = await context.UserSettings.SingleOrDefaultAsync(s => s.UserId == user.Id);
        settings.ShouldNotBeNull();
        settings.NotifyOnTicketCreated.ShouldBeTrue();
        settings.NotifyOnTicketReply.ShouldBeTrue();
    }
}
