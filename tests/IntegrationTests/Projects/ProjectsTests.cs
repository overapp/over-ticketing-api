using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.ProjectEndpoints;

public sealed class ProjectsTests(IntegrationTestAppHost appHost) : BaseIntegrationTest(appHost)
{
    [Fact]
    public async Task Create_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "projects",
            new { organizationId = Guid.NewGuid(), name = "API", description = "Ticketing API" });

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
            "projects",
            new { organizationId = Guid.NewGuid(), name = "API", description = "Ticketing API" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync(
            $"projects/{Guid.NewGuid()}",
            new { name = "Updated API", description = "Updated description" });

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
            $"projects/{Guid.NewGuid()}",
            new { name = "Updated API", description = "Updated description" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"projects/{Guid.NewGuid()}");

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
        HttpResponseMessage response = await HttpClient.DeleteAsync($"projects/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Archive_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.PutAsync($"projects/{Guid.NewGuid()}/archive", null);

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
        HttpResponseMessage response = await HttpClient.PutAsync($"projects/{Guid.NewGuid()}/archive", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unarchive_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.PutAsync($"projects/{Guid.NewGuid()}/unarchive", null);

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
        HttpResponseMessage response = await HttpClient.PutAsync($"projects/{Guid.NewGuid()}/unarchive", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("projects");

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
        HttpResponseMessage response = await HttpClient.GetAsync("projects");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetById_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"projects/{Guid.NewGuid()}");

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
        HttpResponseMessage response = await HttpClient.GetAsync($"projects/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
