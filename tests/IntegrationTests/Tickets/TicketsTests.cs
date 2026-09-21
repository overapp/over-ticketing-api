using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.Tickets;

public sealed class TicketsTests(IntegrationTestAppHost appHost) : BaseIntegrationTest(appHost)
{
    [Fact]
    public async Task Create_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        using var content = new MultipartFormDataContent();
        using var projectIdContent = new StringContent(Guid.NewGuid().ToString());
        using var titleContent = new StringContent("Title");
        using var messageContent = new StringContent("Message");
        content.Add(projectIdContent, "projectId");
        content.Add(titleContent, "title");
        content.Add(messageContent, "message");

        HttpResponseMessage response = await HttpClient.PostAsync("tickets", content);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_Should_ReturnForbidden_WhenUserLacksCreatePermission()
    {
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        using var content = new MultipartFormDataContent();
        using var projectIdContent = new StringContent(Guid.NewGuid().ToString());
        using var titleContent = new StringContent("Title");
        using var messageContent = new StringContent("Message");
        content.Add(projectIdContent, "projectId");
        content.Add(titleContent, "title");
        content.Add(messageContent, "message");

        HttpResponseMessage response = await HttpClient.PostAsync("tickets", content);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetById_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        HttpResponseMessage response = await HttpClient.GetAsync($"tickets/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_Should_ReturnForbidden_WhenUserLacksReadPermission()
    {
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        HttpResponseMessage response = await HttpClient.GetAsync($"tickets/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reply_Should_ReturnUnauthorized_WhenRequestIsNotAuthenticated()
    {
        using var content = new MultipartFormDataContent();
        using var replyContent = new StringContent("Reply");
        content.Add(replyContent, "content");

        HttpResponseMessage response = await HttpClient.PostAsync($"tickets/{Guid.NewGuid()}/messages", content);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reply_Should_ReturnForbidden_WhenUserLacksReplyPermission()
    {
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        using var content = new MultipartFormDataContent();
        using var replyContent = new StringContent("Reply");
        content.Add(replyContent, "content");

        HttpResponseMessage response = await HttpClient.PostAsync($"tickets/{Guid.NewGuid()}/messages", content);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
