using System.Net;
using System.Text;
using System.Text.Json;
using EnglishTestWeb.Api.Tests.Auth;

namespace EnglishTestWeb.Api.Tests.Auth;

public sealed class StudentLoginTests
{
    [Fact]
    public async Task StudentLogin_WithValidMember_ReturnsActiveClass()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();

        var response = await StudentLoginAsync(
            client,
            AuthTestHelper.StudentEmail,
            AuthTestHelper.StudentPassword,
            Classes.ClassesTestHelper.ClassCode);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Contains(
            "Student",
            document.RootElement.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
        Assert.Equal(
            Classes.ClassesTestHelper.ClassCode,
            document.RootElement.GetProperty("activeClass").GetProperty("classCode").GetString());
    }

    [Fact]
    public async Task StudentLogin_WithInvalidPassword_ReturnsLoginInvalid()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();

        var response = await StudentLoginAsync(
            client,
            AuthTestHelper.StudentEmail,
            "wrong-password",
            Classes.ClassesTestHelper.ClassCode);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.loginInvalid", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task StudentLogin_WithNonMemberStudent_ReturnsLoginInvalid()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();

        var response = await StudentLoginAsync(
            client,
            Classes.ClassesTestHelper.NonMemberStudentEmail,
            Classes.ClassesTestHelper.NonMemberStudentPassword,
            Classes.ClassesTestHelper.ClassCode);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.loginInvalid", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task StudentLogin_WithInactiveClass_ReturnsCodeInactive()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        await Classes.ClassesTestHelper.SeedInactiveClassAsync(factory);
        using var client = factory.CreateClient();

        var response = await StudentLoginAsync(
            client,
            AuthTestHelper.StudentEmail,
            AuthTestHelper.StudentPassword,
            "INACTIVE1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("classes.codeInactive", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task StudentLogin_WithTeacherCredentials_ReturnsLoginInvalid()
    {
        await using var factory = new TestApiFactory();
        await Classes.ClassesTestHelper.SeedDemoClassAsync(factory);
        using var client = factory.CreateClient();

        var response = await StudentLoginAsync(
            client,
            AuthTestHelper.TeacherEmail,
            AuthTestHelper.TeacherPassword,
            Classes.ClassesTestHelper.ClassCode);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.loginInvalid", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    internal static async Task<HttpResponseMessage> StudentLoginAsync(
        HttpClient client,
        string identifier,
        string password,
        string classCode,
        bool rememberMe = false)
    {
        await AuthTestHelper.EnsureXsrfAsync(client);

        var payload = JsonSerializer.Serialize(new
        {
            identifier,
            password,
            classCode,
            rememberMe
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        return await client.PostAsync("/api/auth/student/login", content);
    }
}
