using System.Text.Json;
using EnglishTestWeb.Api.Application.TestTemplates;
using EnglishTestWeb.Api.Contracts.TestTemplates;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.TestTemplates;

public sealed class AnswerKeyService(EnglishTestWebDbContext dbContext) : IAnswerKeyService
{
    private static readonly JsonSerializerOptions RowsJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AnswerKeyAccessResult> GetAsync(
        Guid templateId,
        string teacherId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var answerKey = await dbContext.AnswerKeyVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.TemplateId == templateId, cancellationToken);

        if (answerKey is null)
        {
            return new AnswerKeyAccessResult(
                false,
                null,
                "answerKey.notFound",
                StatusCodes.Status404NotFound);
        }

        return new AnswerKeyAccessResult(true, MapResponse(answerKey), null);
    }

    public async Task<AnswerKeyAccessResult> UpsertDraftAsync(
        Guid templateId,
        string teacherId,
        UpsertAnswerKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        var template = await dbContext.TestTemplates
            .FirstOrDefaultAsync(entity => entity.Id == templateId, cancellationToken);

        if (template is null)
        {
            return new AnswerKeyAccessResult(
                false,
                null,
                "templates.notFound",
                StatusCodes.Status404NotFound);
        }

        if (string.Equals(template.Skill, TemplateSkill.Speaking, StringComparison.Ordinal))
        {
            return new AnswerKeyAccessResult(
                false,
                null,
                "answerKey.notApplicable",
                StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(template.Status, TemplateStatuses.Draft, StringComparison.Ordinal))
        {
            return new AnswerKeyAccessResult(
                false,
                null,
                "templates.notEditable",
                StatusCodes.Status409Conflict);
        }

        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return new AnswerKeyAccessResult(
                false,
                null,
                validationError,
                StatusCodes.Status400BadRequest);
        }

        var rows = (request.Rows ?? [])
            .Select(row => new AnswerKeyRow(
                row.QuestionNumber,
                row.CorrectAnswer?.Trim() ?? string.Empty,
                row.Score))
            .ToList();
        var rowsJson = JsonSerializer.Serialize(rows, RowsJsonOptions);
        var now = DateTimeOffset.UtcNow;

        var existing = await dbContext.AnswerKeyVersions
            .FirstOrDefaultAsync(entity => entity.TemplateId == templateId, cancellationToken);

        AnswerKeyVersion entity;
        if (existing is null)
        {
            entity = new AnswerKeyVersion
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                Status = AnswerKeyStatuses.Draft,
                ScoringMode = request.ScoringMode,
                QuestionCount = request.QuestionCount,
                TotalScore = request.TotalScore,
                RowsJson = rowsJson,
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.AnswerKeyVersions.Add(entity);
        }
        else
        {
            existing.ScoringMode = request.ScoringMode;
            existing.QuestionCount = request.QuestionCount;
            existing.TotalScore = request.TotalScore;
            existing.RowsJson = rowsJson;
            existing.UpdatedAt = now;
            entity = existing;
        }

        template.UpdatedAt = now;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new AnswerKeyAccessResult(
                false,
                null,
                "answerKey.concurrencyConflict",
                StatusCodes.Status409Conflict);
        }
        catch (DbUpdateException)
        {
            return new AnswerKeyAccessResult(
                false,
                null,
                "answerKey.saveFailed",
                StatusCodes.Status500InternalServerError);
        }

        return new AnswerKeyAccessResult(true, MapResponse(entity), null);
    }

    private static string? ValidateRequest(UpsertAnswerKeyRequest request)
    {
        if (request.QuestionCount is < 1 or > 200)
        {
            return "answerKey.invalid.questionCount";
        }

        if (!string.Equals(request.ScoringMode, ScoringModes.Equal, StringComparison.Ordinal)
            && !string.Equals(request.ScoringMode, ScoringModes.PerQuestion, StringComparison.Ordinal))
        {
            return "answerKey.invalid.scoringMode";
        }

        var rows = request.Rows ?? [];
        if (rows.Count > 0)
        {
            if (rows.Count != request.QuestionCount)
            {
                return "answerKey.invalid.rowCount";
            }

            var seen = new HashSet<int>();
            foreach (var row in rows)
            {
                if (row.QuestionNumber < 1 || row.QuestionNumber > request.QuestionCount)
                {
                    return "answerKey.invalid.rowNumber";
                }

                if (!seen.Add(row.QuestionNumber))
                {
                    return "answerKey.invalid.rowNumber";
                }
            }
        }

        return null;
    }

    private static AnswerKeyVersionResponse MapResponse(AnswerKeyVersion entity)
    {
        List<AnswerKeyRow> rows;
        try
        {
            rows = JsonSerializer.Deserialize<List<AnswerKeyRow>>(entity.RowsJson, RowsJsonOptions) ?? [];
        }
        catch (JsonException)
        {
            rows = [];
        }

        return new AnswerKeyVersionResponse(
            entity.Id,
            entity.TemplateId,
            entity.Status,
            entity.ScoringMode,
            entity.QuestionCount,
            entity.TotalScore,
            rows
                .Select(row => new AnswerKeyRowResponse(row.QuestionNumber, row.CorrectAnswer, row.Score))
                .ToList(),
            entity.UpdatedAt);
    }
}
