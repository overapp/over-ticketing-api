using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Users;

public sealed class PasswordChangeFlowTests(IntegrationTestAppHost appHost) : BaseIntegrationTest(appHost)
{
    [Fact]
    public async Task SelfServicePasswordChange_Should_Succeed_AndAllowAccess()
    {
        // 1. Register a user (mustChangePassword should be false)
        string email = UniqueEmail();
        await RegisterUserAsync(email);
        AccessTokens loginTokens = await LoginAsync(email);

        loginTokens.MustChangePassword.ShouldBeFalse();

        Authenticate(loginTokens.AccessToken);

        // 2. Change password self-service
        HttpResponseMessage changeResponse = await HttpClient.PostAsJsonAsync(
            "auth/change-password",
            new
            {
                currentPassword = "Password123!",
                newPassword = "NewPassword123!",
                confirmNewPassword = "NewPassword123!"
            });

        changeResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // 3. Old password should fail login
        HttpResponseMessage oldLoginResponse = await HttpClient.PostAsJsonAsync(
            "auth/login",
            new { email, password = "Password123!" });

        oldLoginResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // 4. New password should succeed
        HttpResponseMessage newLoginResponse = await HttpClient.PostAsJsonAsync(
            "auth/login",
            new { email, password = "NewPassword123!" });

        newLoginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        AccessTokens? newTokens = await newLoginResponse.Content.ReadFromJsonAsync<AccessTokens>();
        newTokens.ShouldNotBeNull();
        newTokens.MustChangePassword.ShouldBeFalse();
    }

    [Fact]
    public async Task ChangePassword_Should_Fail_WhenCurrentPasswordIsWrong()
    {
        // Register & login
        string email = UniqueEmail();
        await RegisterUserAsync(email);
        AccessTokens tokens = await LoginAsync(email);

        Authenticate(tokens.AccessToken);

        HttpResponseMessage changeResponse = await HttpClient.PostAsJsonAsync(
            "auth/change-password",
            new
            {
                currentPassword = "WrongPassword123!",
                newPassword = "NewPassword123!",
                confirmNewPassword = "NewPassword123!"
            });

        changeResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
