using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Domain.Classes;
using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.Classes;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.AssignedTests;

public sealed class AssignedTestsControllerTests
{
    [Fact]
    public async Task GetAssignedTests_AsAnonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/assigned-tests");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task GetAssignedTests_AsTeacher_Returns403()
    {
        await using var factory = new TestApiFactory();
        await TestTemplates.TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await client.GetAsync("/api/assigned-tests");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task GetAssignedTests_AsStudent_WithHomework_ReturnsHomeworkItem()
    {
        await using var factory = new TestApiFactory();
        var (_, classId) = await AssignedTestsTestHelper.SeedHomeworkForStudentClassAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await client.GetAsync("/api/assigned-tests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        var items = document.RootElement.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        var item = items[0];
        Assert.Equal("homework", item.GetProperty("mode").GetString());
        Assert.Equal("available", item.GetProperty("studentStatus").GetString());
    }

    [Fact]
    public async Task GetAssignedTests_AsStudent_WithExpiredHomework_ReturnsExpiredStatus()
    {
        await using var factory = new TestApiFactory();
        var pastDeadline = DateTimeOffset.UtcNow.AddDays(-1);
        var (_, classId) = await AssignedTestsTestHelper.SeedHomeworkForStudentClassAsync(factory, deadlineAt: pastDeadline);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await client.GetAsync("/api/assigned-tests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        var items = document.RootElement.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("expired", items[0].GetProperty("studentStatus").GetString());
    }

    [Fact]
    public async Task GetAssignedTests_AsStudent_WithScheduledLiveExam_ReturnsNotOpenStatus()
    {
        await using var factory = new TestApiFactory();
        var (_, classId) = await AssignedTestsTestHelper.SeedLiveExamForStudentClassAsync(
            factory, status: LiveExamSessionStatuses.Scheduled);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await client.GetAsync("/api/assigned-tests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        var items = document.RootElement.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        var item = items[0];
        Assert.Equal("live-exam", item.GetProperty("mode").GetString());
        Assert.Equal("not-open", item.GetProperty("studentStatus").GetString());
    }

    [Fact]
    public async Task GetAssignedTests_AsStudent_WithOpenLiveExam_ReturnsAvailableStatus()
    {
        await using var factory = new TestApiFactory();
        var (_, classId) = await AssignedTestsTestHelper.SeedLiveExamForStudentClassAsync(
            factory, status: LiveExamSessionStatuses.Open);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await client.GetAsync("/api/assigned-tests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        var items = document.RootElement.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("available", items[0].GetProperty("studentStatus").GetString());
    }

    [Fact]
    public async Task GetAssignedTests_AsStudent_WithClosedLiveExam_ReturnsClosedStatus()
    {
        await using var factory = new TestApiFactory();
        var (_, classId) = await AssignedTestsTestHelper.SeedLiveExamForStudentClassAsync(
            factory, status: LiveExamSessionStatuses.Closed);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await client.GetAsync("/api/assigned-tests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        var items = document.RootElement.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("closed", items[0].GetProperty("studentStatus").GetString());
    }

    [Fact]
    public async Task GetAssignedTests_AsStudent_EmptyClass_ReturnsEmptyList()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await client.GetAsync("/api/assigned-tests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        var items = document.RootElement.GetProperty("items");
        Assert.Equal(0, items.GetArrayLength());
    }

    [Fact]
    public async Task GetAssignedTests_AsStudent_WithNoActiveClass_ReturnsEmptyList()
    {
        await using var factory = new TestApiFactory();
        await AuthTestHelper.SeedRolesAndUsersAsync(factory);
        using var client = factory.CreateClient();
        // Sign in without active class (activeClassId = null)
        await AuthTestHelper.SignInStudentAsync(client);

        var response = await client.GetAsync("/api/assigned-tests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        var items = document.RootElement.GetProperty("items");
        Assert.Equal(0, items.GetArrayLength());
    }

    [Fact]
    public async Task GetAssignedTests_AsStudentFromDifferentClass_ReturnsEmpty()
    {
        await using var factory = new TestApiFactory();
        // Seed homework for classA
        await AssignedTestsTestHelper.SeedHomeworkForStudentClassAsync(factory);
        // Seed classB with student membership but no homework
        var classBId = await ClassesTestHelper.SeedSecondClassWithoutMembershipAsync(factory);

        // Add student membership to classB
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
            var student = db.Users.First(u => u.Email == AuthTestHelper.StudentEmail);
            db.ClassMemberships.Add(new ClassMembership
            {
                Id = Guid.NewGuid(),
                ClassId = classBId,
                StudentId = student.Id,
                Status = ClassStatuses.Active,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        // Sign in with classB as active class; classB has no homework
        await AuthTestHelper.SignInStudentWithClassAsync(client, classBId);

        var response = await client.GetAsync("/api/assigned-tests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        var items = document.RootElement.GetProperty("items");
        Assert.Equal(0, items.GetArrayLength());
    }
}
