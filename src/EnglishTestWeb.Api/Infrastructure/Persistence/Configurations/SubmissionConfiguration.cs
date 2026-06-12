using EnglishTestWeb.Api.Domain.Assignments;
using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Domain.Submissions;
using EnglishTestWeb.Api.Domain.TestTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Configurations;

public sealed class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> entity)
    {
        entity.HasKey(s => s.Id);

        entity.Property(s => s.StudentId).HasMaxLength(450).IsRequired();
        entity.Property(s => s.Status).HasMaxLength(50).IsRequired();
        entity.Property(s => s.AutoScore).HasColumnType("decimal(18,2)");
        entity.Property(s => s.RowVersion).IsRowVersion();

        entity.HasIndex(s => new { s.StudentId, s.HomeworkAssignmentId })
            .IsUnique()
            .HasFilter("[HomeworkAssignmentId] IS NOT NULL");

        entity.HasIndex(s => new { s.StudentId, s.LiveExamSessionId })
            .IsUnique()
            .HasFilter("[LiveExamSessionId] IS NOT NULL");

        entity.ToTable(t => t.HasCheckConstraint(
            "CK_Submissions_ExactlyOneSource",
            "([HomeworkAssignmentId] IS NOT NULL AND [LiveExamSessionId] IS NULL) OR ([HomeworkAssignmentId] IS NULL AND [LiveExamSessionId] IS NOT NULL)"));

        entity.HasOne(s => s.HomeworkAssignment)
            .WithMany()
            .HasForeignKey(s => s.HomeworkAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(s => s.LiveExamSession)
            .WithMany()
            .HasForeignKey(s => s.LiveExamSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<AnswerKeyVersion>()
            .WithMany()
            .HasForeignKey(s => s.AnswerKeyVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
