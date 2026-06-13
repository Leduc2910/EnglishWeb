using System.Net;
using System.Text.Json;
using EnglishTestWeb.Api.Domain.Files;
using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Domain.Speaking;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using EnglishTestWeb.Api.Tests.Auth;
using EnglishTestWeb.Api.Tests.Classes;
using EnglishTestWeb.Api.Tests.TestTemplates;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishTestWeb.Api.Tests.Speaking;

public sealed class SpeakingSubmissionsTests
{
    // ---- POST /api/speaking-submissions ----

    [Fact]
    public async Task Post_AsAnonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/speaking-submissions", new
        {
            homeworkAssignmentId = Guid.NewGuid(),
            liveExamSessionId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Post_AsTeacher_Returns403()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/speaking-submissions", new
        {
            homeworkAssignmentId = Guid.NewGuid(),
            liveExamSessionId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Post_ValidSpeakingHomework_ReturnsDto()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/speaking-submissions", new
        {
            homeworkAssignmentId = homeworkId,
            liveExamSessionId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        Assert.NotEqual(Guid.Empty, doc.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("draft", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("homework", doc.RootElement.GetProperty("mode").GetString());
        Assert.Equal("speaking", doc.RootElement.GetProperty("templateSkill").GetString());
        Assert.True(doc.RootElement.GetProperty("isSourceOpen").GetBoolean());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("draftFile").ValueKind);
    }

    [Fact]
    public async Task Post_ValidSpeakingHomework_Idempotent_ReturnsSameId()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var request = new { homeworkAssignmentId = homeworkId, liveExamSessionId = (Guid?)null };

        var first = await AuthTestHelper.PostJsonAsync(client, "/api/speaking-submissions", request);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        await using var firstBody = await first.Content.ReadAsStreamAsync();
        using var firstDoc = await JsonDocument.ParseAsync(firstBody);
        var firstId = firstDoc.RootElement.GetProperty("id").GetGuid();

        var second = await AuthTestHelper.PostJsonAsync(client, "/api/speaking-submissions", request);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        await using var secondBody = await second.Content.ReadAsStreamAsync();
        using var secondDoc = await JsonDocument.ParseAsync(secondBody);
        var secondId = secondDoc.RootElement.GetProperty("id").GetGuid();

        Assert.Equal(firstId, secondId);
    }

    [Fact]
    public async Task Post_ValidOpenLiveExam_ReturnsDto()
    {
        await using var factory = new TestApiFactory();
        var (sessionId, classId) = await SpeakingTestHelper.SeedSpeakingLiveExamAsync(
            factory, status: LiveExamSessionStatuses.Open);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/speaking-submissions", new
        {
            homeworkAssignmentId = (Guid?)null,
            liveExamSessionId = sessionId
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        Assert.Equal("live-exam", doc.RootElement.GetProperty("mode").GetString());
        Assert.True(doc.RootElement.GetProperty("isSourceOpen").GetBoolean());
    }

    [Fact]
    public async Task Post_ClosedLiveExam_ReturnsDto_IsSourceOpenFalse()
    {
        await using var factory = new TestApiFactory();
        var (sessionId, classId) = await SpeakingTestHelper.SeedSpeakingLiveExamAsync(
            factory, status: LiveExamSessionStatuses.Closed);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/speaking-submissions", new
        {
            homeworkAssignmentId = (Guid?)null,
            liveExamSessionId = sessionId
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        Assert.False(doc.RootElement.GetProperty("isSourceOpen").GetBoolean());
    }

    [Fact]
    public async Task Post_BothSourcesProvided_Returns422()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/speaking-submissions", new
        {
            homeworkAssignmentId = homeworkId,
            liveExamSessionId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("speaking.invalidSource", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Post_NeitherSourceProvided_Returns422()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/speaking-submissions", new
        {
            homeworkAssignmentId = (Guid?)null,
            liveExamSessionId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("speaking.invalidSource", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Post_HomeworkNotInStudentClass_Returns422()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, _) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        using var client = factory.CreateClient();
        // Sign in with a different class (same classId but homework belongs to same class in this test)
        // Use a random class id that doesn't match
        await AuthTestHelper.SignInStudentWithClassAsync(client, Guid.NewGuid());

        var response = await AuthTestHelper.PostJsonAsync(client, "/api/speaking-submissions", new
        {
            homeworkAssignmentId = homeworkId,
            liveExamSessionId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("speaking.sourceUnavailable", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    // ---- GET /api/speaking-submissions/{id} ----

    [Fact]
    public async Task Get_AsAnonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/speaking-submissions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Get_AsTeacher_Returns403()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await client.GetAsync($"/api/speaking-submissions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Get_AfterCreation_ReturnsFullDto()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SpeakingTestHelper.CreateSpeakingSubmissionAsync(
            client, homeworkId, null);

        var response = await client.GetAsync($"/api/speaking-submissions/{submissionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        Assert.Equal(submissionId, doc.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("draft", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("homework", doc.RootElement.GetProperty("mode").GetString());
        Assert.Equal("speaking", doc.RootElement.GetProperty("templateSkill").GetString());
    }

    [Fact]
    public async Task Get_NonExistent_Returns404()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await client.GetAsync($"/api/speaking-submissions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("speaking.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    // ---- POST /api/speaking-submissions/{id}/upload-draft ----

    [Fact]
    public async Task UploadDraft_AsAnonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await AuthTestHelper.PostMultipartAsync(
            client,
            $"/api/speaking-submissions/{Guid.NewGuid()}/upload-draft",
            content =>
            {
                var fileBytes = new byte[128];
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
                content.Add(fileContent, "file", "rec.webm");
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task UploadDraft_AsTeacher_Returns403()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await AuthTestHelper.PostMultipartAsync(
            client,
            $"/api/speaking-submissions/{Guid.NewGuid()}/upload-draft",
            content =>
            {
                var fileContent = new ByteArrayContent(new byte[128]);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
                content.Add(fileContent, "file", "rec.webm");
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task UploadDraft_ValidAudioFile_ReturnsUpdatedDto()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SpeakingTestHelper.CreateSpeakingSubmissionAsync(
            client, homeworkId, null);

        var response = await AuthTestHelper.PostMultipartAsync(
            client,
            $"/api/speaking-submissions/{submissionId}/upload-draft",
            content =>
            {
                var fileContent = new ByteArrayContent(new byte[1024]);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
                content.Add(fileContent, "file", "recording.webm");
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        Assert.Equal(submissionId, doc.RootElement.GetProperty("id").GetGuid());
        Assert.NotEqual(JsonValueKind.Null, doc.RootElement.GetProperty("draftFile").ValueKind);
        Assert.Equal("recording.webm",
            doc.RootElement.GetProperty("draftFile").GetProperty("originalFileName").GetString());
    }

    [Fact]
    public async Task UploadDraft_SecondUpload_ReplacesFirst()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SpeakingTestHelper.CreateSpeakingSubmissionAsync(
            client, homeworkId, null);

        // First upload
        await AuthTestHelper.PostMultipartAsync(
            client,
            $"/api/speaking-submissions/{submissionId}/upload-draft",
            content =>
            {
                var fc = new ByteArrayContent(new byte[512]);
                fc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
                content.Add(fc, "file", "first.webm");
            });

        // Second upload — should replace
        var response = await AuthTestHelper.PostMultipartAsync(
            client,
            $"/api/speaking-submissions/{submissionId}/upload-draft",
            content =>
            {
                var fc = new ByteArrayContent(new byte[1024]);
                fc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mp4");
                content.Add(fc, "file", "second.mp4");
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        Assert.Equal("second.mp4",
            doc.RootElement.GetProperty("draftFile").GetProperty("originalFileName").GetString());
    }

    [Fact]
    public async Task UploadDraft_InvalidMimeType_Returns422()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SpeakingTestHelper.CreateSpeakingSubmissionAsync(
            client, homeworkId, null);

        var response = await AuthTestHelper.PostMultipartAsync(
            client,
            $"/api/speaking-submissions/{submissionId}/upload-draft",
            content =>
            {
                var fileContent = new ByteArrayContent(new byte[128]);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
                content.Add(fileContent, "file", "bad.pdf");
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("speaking.invalidFileType", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task UploadDraft_DisallowedExtension_Returns422()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SpeakingTestHelper.CreateSpeakingSubmissionAsync(
            client, homeworkId, null);

        // Valid MIME type but wrong extension
        var response = await AuthTestHelper.PostMultipartAsync(
            client,
            $"/api/speaking-submissions/{submissionId}/upload-draft",
            content =>
            {
                var fileContent = new ByteArrayContent(new byte[128]);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
                content.Add(fileContent, "file", "recording.exe");
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("speaking.invalidFileType", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task UploadDraft_EmptyFile_Returns422()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SpeakingTestHelper.CreateSpeakingSubmissionAsync(
            client, homeworkId, null);

        var response = await AuthTestHelper.PostMultipartAsync(
            client,
            $"/api/speaking-submissions/{submissionId}/upload-draft",
            content =>
            {
                var fileContent = new ByteArrayContent(Array.Empty<byte>());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
                content.Add(fileContent, "file", "empty.webm");
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("speaking.emptyFile", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task UploadDraft_ClosedSource_Returns422()
    {
        await using var factory = new TestApiFactory();
        // Seed homework with a past deadline
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(
            factory, deadlineAt: DateTimeOffset.UtcNow.AddDays(-1));
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        // Create the submission first (allowed even for expired source)
        var submissionId = await SpeakingTestHelper.CreateSpeakingSubmissionAsync(
            client, homeworkId, null);

        // Try to upload — source is now closed
        var response = await AuthTestHelper.PostMultipartAsync(
            client,
            $"/api/speaking-submissions/{submissionId}/upload-draft",
            content =>
            {
                var fileContent = new ByteArrayContent(new byte[512]);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
                content.Add(fileContent, "file", "rec.webm");
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("speaking.sourceUnavailable", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task UploadDraft_AlreadySubmitted_Returns409()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SpeakingTestHelper.CreateSpeakingSubmissionAsync(
            client, homeworkId, null);

        // Mark the submission as submitted directly in DB
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
            var sub = await db.SpeakingSubmissions.FindAsync(submissionId);
            sub!.Status = SpeakingSubmissionStatuses.Submitted;
            await db.SaveChangesAsync();
        }

        var response = await AuthTestHelper.PostMultipartAsync(
            client,
            $"/api/speaking-submissions/{submissionId}/upload-draft",
            content =>
            {
                var fileContent = new ByteArrayContent(new byte[512]);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
                content.Add(fileContent, "file", "rec.webm");
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("speaking.alreadySubmitted", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Get_OtherStudentsSubmission_Returns404()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        // Seed a submission owned by a different student directly in the DB
        Guid otherSubmissionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
            var otherSubmission = new SpeakingSubmission
            {
                Id = Guid.NewGuid(),
                StudentId = "other-student-get-99",
                HomeworkAssignmentId = homeworkId,
                Status = SpeakingSubmissionStatuses.Draft,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            db.SpeakingSubmissions.Add(otherSubmission);
            await db.SaveChangesAsync();
            otherSubmissionId = otherSubmission.Id;
        }

        var response = await client.GetAsync($"/api/speaking-submissions/{otherSubmissionId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("speaking.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task UploadDraft_WrongSubmission_Returns404()
    {
        await using var factory = new TestApiFactory();
        await ClassesTestHelper.SeedDemoClassAsync(factory);
        var classId = await ClassesTestHelper.GetDemoClassIdAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var response = await AuthTestHelper.PostMultipartAsync(
            client,
            $"/api/speaking-submissions/{Guid.NewGuid()}/upload-draft",
            content =>
            {
                var fileContent = new ByteArrayContent(new byte[128]);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
                content.Add(fileContent, "file", "rec.webm");
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("speaking.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task UploadDraft_OtherStudentsSubmission_Returns404()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        // Seed a submission owned by a different student directly in the DB
        Guid otherSubmissionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
            var otherSubmission = new SpeakingSubmission
            {
                Id = Guid.NewGuid(),
                StudentId = "other-student-99",
                HomeworkAssignmentId = homeworkId,
                Status = SpeakingSubmissionStatuses.Draft,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            db.SpeakingSubmissions.Add(otherSubmission);
            await db.SaveChangesAsync();
            otherSubmissionId = otherSubmission.Id;
        }

        var response = await AuthTestHelper.PostMultipartAsync(
            client,
            $"/api/speaking-submissions/{otherSubmissionId}/upload-draft",
            content =>
            {
                var fileContent = new ByteArrayContent(new byte[512]);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
                content.Add(fileContent, "file", "rec.webm");
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("speaking.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task UploadDraft_SecondUpload_ArchivesFirstFileInDb()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SpeakingTestHelper.CreateSpeakingSubmissionAsync(client, homeworkId, null);

        // First upload — capture the draft file ID
        var firstResponse = await AuthTestHelper.PostMultipartAsync(
            client,
            $"/api/speaking-submissions/{submissionId}/upload-draft",
            content =>
            {
                var fc = new ByteArrayContent(new byte[512]);
                fc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
                content.Add(fc, "file", "first.webm");
            });
        firstResponse.EnsureSuccessStatusCode();
        await using var firstBody = await firstResponse.Content.ReadAsStreamAsync();
        using var firstDoc = await JsonDocument.ParseAsync(firstBody);
        var firstFileId = firstDoc.RootElement.GetProperty("draftFile").GetProperty("fileId").GetGuid();

        // Second upload replaces the first
        var secondResponse = await AuthTestHelper.PostMultipartAsync(
            client,
            $"/api/speaking-submissions/{submissionId}/upload-draft",
            content =>
            {
                var fc = new ByteArrayContent(new byte[1024]);
                fc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mp4");
                content.Add(fc, "file", "second.mp4");
            });
        secondResponse.EnsureSuccessStatusCode();

        // Assert the first StoredFile is archived in the database
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var archivedFile = await db.StoredFiles.FindAsync(firstFileId);
        Assert.NotNull(archivedFile);
        Assert.Equal(StoredFileStatuses.Archived, archivedFile!.Status);
    }

    // ---- POST /api/speaking-submissions/{id}/final-submit ----

    [Fact]
    public async Task FinalSubmit_AsAnonymous_Returns401()
    {
        await using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/speaking-submissions/{Guid.NewGuid()}/final-submit",
            new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.unauthorized", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task FinalSubmit_AsTeacher_Returns403()
    {
        await using var factory = new TestApiFactory();
        await TestTemplatesTestHelper.SeedDemoTemplatesAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInTeacherAsync(client);

        var response = await client.PostAsync($"/api/speaking-submissions/{Guid.NewGuid()}/final-submit",
            new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("auth.forbidden", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task FinalSubmit_NoDraftFile_Returns422()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        // Create submission but do NOT upload a draft file
        var submissionId = await SpeakingTestHelper.CreateSpeakingSubmissionAsync(client, homeworkId, null);

        var response = await client.PostAsync($"/api/speaking-submissions/{submissionId}/final-submit",
            new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("speaking.fileRequired", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task FinalSubmit_WithDraftFile_Returns200WithSubmittedStatus()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SpeakingTestHelper.CreateSpeakingSubmissionAsync(client, homeworkId, null);

        // Upload draft file
        var uploadContent = SpeakingTestHelper.CreateAudioFormFile();
        var uploadResponse = await client.PostAsync(
            $"/api/speaking-submissions/{submissionId}/upload-draft", uploadContent);
        uploadResponse.EnsureSuccessStatusCode();

        // Final submit
        var response = await client.PostAsync(
            $"/api/speaking-submissions/{submissionId}/final-submit", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var body = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(body);
        var root = doc.RootElement;

        Assert.Equal("submitted", root.GetProperty("status").GetString());
        Assert.False(root.GetProperty("submittedAt").ValueKind == JsonValueKind.Null,
            "submittedAt should be set after final submit");
        Assert.NotEqual(JsonValueKind.Null, root.GetProperty("draftFile").ValueKind);
    }

    [Fact]
    public async Task FinalSubmit_Idempotent_ReturnsSameSubmittedAt()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        var submissionId = await SpeakingTestHelper.CreateSpeakingSubmissionAsync(client, homeworkId, null);
        var uploadContent = SpeakingTestHelper.CreateAudioFormFile();
        (await client.PostAsync($"/api/speaking-submissions/{submissionId}/upload-draft", uploadContent))
            .EnsureSuccessStatusCode();

        // First submit
        var resp1 = await client.PostAsync(
            $"/api/speaking-submissions/{submissionId}/final-submit", new StringContent(string.Empty));
        resp1.EnsureSuccessStatusCode();
        await using var body1 = await resp1.Content.ReadAsStreamAsync();
        using var doc1 = await JsonDocument.ParseAsync(body1);
        var submittedAt1 = doc1.RootElement.GetProperty("submittedAt").GetString();

        // Second submit (idempotent)
        var resp2 = await client.PostAsync(
            $"/api/speaking-submissions/{submissionId}/final-submit", new StringContent(string.Empty));
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
        await using var body2 = await resp2.Content.ReadAsStreamAsync();
        using var doc2 = await JsonDocument.ParseAsync(body2);
        var submittedAt2 = doc2.RootElement.GetProperty("submittedAt").GetString();

        Assert.Equal("submitted", doc2.RootElement.GetProperty("status").GetString());
        Assert.Equal(submittedAt1, submittedAt2);
    }

    [Fact]
    public async Task FinalSubmit_OtherStudent_Returns404()
    {
        await using var factory = new TestApiFactory();
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(factory);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        // Seed a submission owned by a different student directly
        var otherStudentId = "other-student-" + Guid.NewGuid().ToString("N");
        var submissionId = await SpeakingTestHelper.SeedSubmissionWithDraftAsync(
            factory, homeworkId, otherStudentId);

        var response = await client.PostAsync(
            $"/api/speaking-submissions/{submissionId}/final-submit", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("speaking.notFound", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task FinalSubmit_SourceClosed_Returns422()
    {
        await using var factory = new TestApiFactory();
        // Seed homework with deadline already passed
        var (homeworkId, classId) = await SpeakingTestHelper.SeedSpeakingHomeworkAsync(
            factory, deadlineAt: DateTimeOffset.UtcNow.AddDays(-1));
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        // Seed submission with draft file directly (can't upload because source is closed)
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var studentId = db.Users.First(u => u.Email == AuthTestHelper.StudentEmail).Id;
        var submissionId = await SpeakingTestHelper.SeedSubmissionWithDraftAsync(
            factory, homeworkId, studentId);

        var response = await client.PostAsync(
            $"/api/speaking-submissions/{submissionId}/final-submit", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("speaking.sourceUnavailable", await AuthTestHelper.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task FinalSubmit_LiveExamClosed_Returns422()
    {
        await using var factory = new TestApiFactory();
        var (sessionId, classId) = await SpeakingTestHelper.SeedSpeakingLiveExamAsync(
            factory, status: LiveExamSessionStatuses.Closed);
        using var client = factory.CreateClient();
        await AuthTestHelper.SignInStudentWithClassAsync(client, classId);

        // Seed submission with draft directly since source is closed
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnglishTestWebDbContext>();
        var studentId = db.Users.First(u => u.Email == AuthTestHelper.StudentEmail).Id;

        // Seed SpeakingSubmission with LiveExamSessionId
        var now = DateTimeOffset.UtcNow;
        var draftFile = new EnglishTestWeb.Api.Domain.Files.StoredFile
        {
            Id = Guid.NewGuid(),
            StorageKey = $"draft-{Guid.NewGuid()}.webm",
            OriginalFileName = "recording.webm",
            ContentType = "audio/webm",
            SizeBytes = 1024,
            OwnerUserId = studentId,
            Status = StoredFileStatuses.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.StoredFiles.Add(draftFile);
        var submission = new SpeakingSubmission
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            HomeworkAssignmentId = null,
            LiveExamSessionId = sessionId,
            DraftStoredFileId = draftFile.Id,
            Status = SpeakingSubmissionStatuses.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.SpeakingSubmissions.Add(submission);
        await db.SaveChangesAsync();

        var response = await client.PostAsync(
            $"/api/speaking-submissions/{submission.Id}/final-submit", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("speaking.sourceUnavailable", await AuthTestHelper.ReadProblemCodeAsync(response));
    }
}
