using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Settings;

public sealed class SettingsTests(IntegrationTestAppHost appHost) : BaseIntegrationTest(appHost)
{
    private sealed record EmailNotificationSettingsResponse(
        bool NotifyOnTicketCreated,
        bool NotifyOnTicketReply);

    private sealed record UserSettingsResponse(
        EmailNotificationSettingsResponse EmailNotifications);

    [Fact]
    public async Task Get_Should_ReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("settings");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_Should_ReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync(
            "settings",
            new
            {
                emailNotifications = new
                {
                    notifyOnTicketCreated = false,
                    notifyOnTicketReply = false
                }
            });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Should_ReturnDefaultSettings_WhenAuthenticatedUserHasNoSettingsRow()
    {
        // Arrange
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("settings");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        UserSettingsResponse? settings = await response.Content.ReadFromJsonAsync<UserSettingsResponse>();
        settings.ShouldNotBeNull();
        settings.EmailNotifications.ShouldNotBeNull();
        settings.EmailNotifications.NotifyOnTicketCreated.ShouldBeTrue();
        settings.EmailNotifications.NotifyOnTicketReply.ShouldBeTrue();
    }

    [Fact]
    public async Task Put_Should_UpdateSettingsAndPersist()
    {
        // Arrange
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act - Update settings
        HttpResponseMessage putResponse = await HttpClient.PutAsJsonAsync(
            "settings",
            new
            {
                emailNotifications = new
                {
                    notifyOnTicketCreated = false,
                    notifyOnTicketReply = false
                }
            });

        // Assert - Put returns NoContent
        putResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act - Fetch updated settings
        HttpResponseMessage getResponse = await HttpClient.GetAsync("settings");

        // Assert - Get returns updated values
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        UserSettingsResponse? updated = await getResponse.Content.ReadFromJsonAsync<UserSettingsResponse>();
        updated.ShouldNotBeNull();
        updated.EmailNotifications.ShouldNotBeNull();
        updated.EmailNotifications.NotifyOnTicketCreated.ShouldBeFalse();
        updated.EmailNotifications.NotifyOnTicketReply.ShouldBeFalse();
    }
}
