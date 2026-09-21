using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Users;

public sealed class UsersTests(IntegrationTestAppHost appHost) : BaseIntegrationTest(appHost)
{
    [Fact]
    public async Task Create_Should_ReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "users",
            new { email = UniqueEmail(), firstName = "Mario", lastName = "Rossi", password = "Password123!" });

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
            "users",
            new { email = UniqueEmail(), firstName = "Mario", lastName = "Rossi", password = "Password123!" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Should_ReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("users");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Should_ReturnForbidden_WhenUserLacksReadPermission()
    {
        // Arrange
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("users");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetById_Should_ReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_Should_ReturnForbidden_WhenUserLacksReadPermission()
    {
        // Arrange
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_Should_ReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync(
            $"users/{Guid.NewGuid()}",
            new { email = UniqueEmail(), firstName = "Mario", lastName = "Rossi" });

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
            $"users/{Guid.NewGuid()}",
            new { email = UniqueEmail(), firstName = "Mario", lastName = "Rossi" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_Should_ReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"users/{Guid.NewGuid()}");

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
        HttpResponseMessage response = await HttpClient.DeleteAsync($"users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
