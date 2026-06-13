using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishTestWeb.Api.Application.Identity;
using EnglishTestWeb.Api.Domain.Identity;
using EnglishTestWeb.Api.Infrastructure.Identity;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using EnglishTestWeb.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.Auth;

internal static class AuthTestHelper
{
    internal const string TeacherEmail = "teacher@test.local";
    internal const string TeacherPassword = "Teacher123!";
    internal const string StudentEmail = "student@test.local";
    internal const string StudentPassword = "Student123!";

    internal static async Task SeedRolesAndUsersAsync(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<IIdentityRoleSeeder>().SeedAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(userManager, TeacherEmail, TeacherPassword, IdentityRoleNames.Teacher);
        await EnsureUserAsync(userManager, StudentEmail, StudentPassword, IdentityRoleNames.Student);
    }

    internal static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        await using var body = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(body);
        if (document.RootElement.TryGetProperty("code", out var rootCode))
        {
            return rootCode.GetString();
        }

        if (document.RootElement.TryGetProperty("extensions", out var extensions)
            && extensions.TryGetProperty("code", out var extensionCode))
        {
            return extensionCode.GetString();
        }

        return null;
    }

    internal static async Task EnsureXsrfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/security/xsrf-token");
        response.EnsureSuccessStatusCode();

        if (!client.DefaultRequestHeaders.Contains(XsrfDefaults.HeaderName))
        {
            var token = ReadXsrfToken(response);
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Add(XsrfDefaults.HeaderName, token);
            }
        }
    }

    internal static async Task SignInUserAsync(
        HttpClient client,
        string email,
        string password,
        Guid? activeClassId = null)
    {
        await EnsureXsrfAsync(client);

        var payload = JsonSerializer.Serialize(new { email, password, activeClassId });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/auth/testing/sign-in", content);
        response.EnsureSuccessStatusCode();
    }

    internal static async Task<string> GetCurrentUserIdAsync(HttpClient client)
    {
        var resp = await client.GetAsync("/api/auth/me");
        resp.EnsureSuccessStatusCode();
        await using var body = await resp.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        return doc.RootElement.GetProperty("userId").GetString()!;
    }

    internal static Task SignInTeacherAsync(HttpClient client) =>
        SignInUserAsync(client, TeacherEmail, TeacherPassword);

    internal static Task SignInStudentAsync(HttpClient client) =>
        SignInUserAsync(client, StudentEmail, StudentPassword);

    internal static Task SignInStudentWithClassAsync(HttpClient client, Guid activeClassId) =>
        SignInUserAsync(client, StudentEmail, StudentPassword, activeClassId);

    internal static async Task<HttpResponseMessage> PostJsonAsync(
        HttpClient client,
        string url,
        object body)
    {
        await EnsureXsrfAsync(client);
        var payload = JsonSerializer.Serialize(body);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        return await client.PostAsync(url, content);
    }

    internal static async Task<HttpResponseMessage> PostMultipartAsync(
        HttpClient client,
        string url,
        Action<MultipartFormDataContent> configure)
    {
        await EnsureXsrfAsync(client);
        using var content = new MultipartFormDataContent();
        configure(content);
        return await client.PostAsync(url, content);
    }

    internal static async Task<HttpResponseMessage> DeleteAsync(HttpClient client, string url)
    {
        await EnsureXsrfAsync(client);
        return await client.DeleteAsync(url);
    }

    internal static async Task<HttpResponseMessage> PutJsonAsync(
        HttpClient client,
        string url,
        object body)
    {
        await EnsureXsrfAsync(client);
        var payload = JsonSerializer.Serialize(body);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        return await client.PutAsync(url, content);
    }

    internal static async Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string identifier,
        string password,
        bool rememberMe = false)
    {
        await EnsureXsrfAsync(client);

        var payload = JsonSerializer.Serialize(new
        {
            identifier,
            password,
            rememberMe
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        return await client.PostAsync("/api/auth/login", content);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, role))
            {
                await userManager.AddToRoleAsync(existing, role);
            }

            return;
        }

        var user = new ApplicationUser
        {
            UserName = email.Split('@')[0],
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(error => $"{error.Code}: {error.Description}"));
            throw new InvalidOperationException($"Failed to create test user '{email}'. {errors}");
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(error => $"{error.Code}: {error.Description}"));
            throw new InvalidOperationException($"Failed to assign role '{role}' to '{email}'. {errors}");
        }
    }

    private static string? ReadXsrfToken(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            foreach (var header in setCookieHeaders)
            {
                const string prefix = $"{XsrfDefaults.CookieName}=";
                if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = header[prefix.Length..];
                var end = value.IndexOf(';');
                return end >= 0 ? value[..end] : value;
            }
        }

        return null;
    }
}
