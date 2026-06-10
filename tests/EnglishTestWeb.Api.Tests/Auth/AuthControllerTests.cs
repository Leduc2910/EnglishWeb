using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.Auth;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Login_WithValidTeacherCredentials_ReturnsUserWithTeacherRole()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();

        var response = await AuthTestHelper.LoginAsync(
            client,
            AuthTestHelper.TeacherEmail,
            AuthTestHelper.TeacherPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Contains(
            "Teacher",
            document.RootElement.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsLoginInvalidProblemCode()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();

        var response = await AuthTestHelper.LoginAsync(
            client,
            AuthTestHelper.TeacherEmail,
            "wrong-password");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.loginInvalid", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Login_WithStudentCredentials_ReturnsLoginInvalidProblemCode()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();

        var response = await AuthTestHelper.LoginAsync(
            client,
            AuthTestHelper.StudentEmail,
            AuthTestHelper.StudentPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.loginInvalid", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_AfterTeacherLogin_ReturnsAuthenticatedUser()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();

        var loginResponse = await AuthTestHelper.LoginAsync(
            client,
            AuthTestHelper.TeacherEmail,
            AuthTestHelper.TeacherPassword);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Contains(
            "Teacher",
            document.RootElement.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
    }

    [Fact]
    public async Task TeacherPing_WithStudentSession_ReturnsForbiddenProblemCode()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();

        var loginResponse = await AuthTestHelper.LoginAsync(
            client,
            AuthTestHelper.StudentEmail,
            AuthTestHelper.StudentPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);

        await AuthTestHelper.SignInUserAsync(client, AuthTestHelper.StudentEmail, AuthTestHelper.StudentPassword);

        var response = await client.GetAsync("/api/auth/teacher/ping");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Logout_AfterLogin_ClearsAuthenticatedSession()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();

        var loginResponse = await AuthTestHelper.LoginAsync(
            client,
            AuthTestHelper.TeacherEmail,
            AuthTestHelper.TeacherPassword);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var meResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        await AuthTestHelper.EnsureXsrfAsync(client);
        var logoutResponse = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var meAfterLogout = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meAfterLogout.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidXsrfToken_AllowsPost()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();

        var response = await AuthTestHelper.LoginAsync(
            client,
            AuthTestHelper.TeacherEmail,
            AuthTestHelper.TeacherPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
