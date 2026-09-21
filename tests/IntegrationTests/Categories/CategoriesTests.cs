using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Categories;

public sealed class CategoriesTests(IntegrationTestAppHost appHost) : BaseIntegrationTest(appHost)
{
    [Fact]
    public async Task GetGlobal_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        HttpResponseMessage response = await HttpClient.GetAsync("categories");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetGlobal_Should_ReturnForbidden_WhenUserLacksManagePermission()
    {
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        HttpResponseMessage response = await HttpClient.GetAsync("categories");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateGlobal_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("categories", new
        {
            name = "Bug",
            description = "Defects and errors"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateGlobal_Should_ReturnForbidden_WhenUserLacksManagePermission()
    {
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("categories", new
        {
            name = "Bug",
            description = "Defects and errors"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetByProject_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        HttpResponseMessage response = await HttpClient.GetAsync($"projects/{Guid.NewGuid()}/categories");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProjectCategory_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"projects/{Guid.NewGuid()}/categories", new
        {
            name = "Project Category"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProjectCategory_Should_ReturnForbidden_WhenUserLacksManagePermission()
    {
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync($"projects/{Guid.NewGuid()}/categories", new
        {
            name = "Project Category"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"categories/{Guid.NewGuid()}", new
        {
            name = "Updated Name"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_Should_ReturnForbidden_WhenUserLacksManagePermission()
    {
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"categories/{Guid.NewGuid()}", new
        {
            name = "Updated Name"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Archive_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        HttpResponseMessage response = await HttpClient.PutAsync($"categories/{Guid.NewGuid()}/archive", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Archive_Should_ReturnForbidden_WhenUserLacksManagePermission()
    {
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        HttpResponseMessage response = await HttpClient.PutAsync($"categories/{Guid.NewGuid()}/archive", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unarchive_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        HttpResponseMessage response = await HttpClient.PutAsync($"categories/{Guid.NewGuid()}/unarchive", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unarchive_Should_ReturnForbidden_WhenUserLacksManagePermission()
    {
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        HttpResponseMessage response = await HttpClient.PutAsync($"categories/{Guid.NewGuid()}/unarchive", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateTicketCategory_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"tickets/{Guid.NewGuid()}/category", new
        {
            categoryId = (Guid?)null
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateTicketCategory_Should_ReturnForbidden_WhenUserLacksCategoryUpdatePermission()
    {
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        HttpResponseMessage response = await HttpClient.PutAsJsonAsync($"tickets/{Guid.NewGuid()}/category", new
        {
            categoryId = (Guid?)null
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
