using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Organizations;

public sealed class OrganizationsTests(IntegrationTestAppHost appHost) : BaseIntegrationTest(appHost)
{
    [Fact]
    public async Task Create_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "organizations",
            new { name = "Overapp", logo = "https://example.com/logo.png" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_Should_ReturnForbidden_WhenUserLacksCreatePermission()
    {
        // Arrange
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "organizations",
            new { name = "Overapp", logo = "https://example.com/logo.png" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync(
            $"organizations/{Guid.NewGuid()}",
            new { name = "Updated Overapp", logo = "https://example.com/updated-logo.png" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_Should_ReturnForbidden_WhenUserLacksEditPermission()
    {
        // Arrange
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync(
            $"organizations/{Guid.NewGuid()}",
            new { name = "Updated Overapp", logo = "https://example.com/updated-logo.png" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"organizations/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_Should_ReturnForbidden_WhenUserLacksDeletePermission()
    {
        // Arrange
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"organizations/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Archive_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.PutAsync(
            $"organizations/{Guid.NewGuid()}/archive",
            null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Archive_Should_ReturnForbidden_WhenUserLacksArchivePermission()
    {
        // Arrange
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.PutAsync(
            $"organizations/{Guid.NewGuid()}/archive",
            null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unarchive_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.PutAsync(
            $"organizations/{Guid.NewGuid()}/unarchive",
            null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unarchive_Should_ReturnForbidden_WhenUserLacksArchivePermission()
    {
        // Arrange
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.PutAsync(
            $"organizations/{Guid.NewGuid()}/unarchive",
            null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

}
