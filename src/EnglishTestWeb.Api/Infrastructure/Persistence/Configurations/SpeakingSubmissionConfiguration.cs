using EnglishTestWeb.Api.Domain.Speaking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Configurations;

public sealed class SpeakingSubmissionConfiguration : IEntityTypeConfiguration<SpeakingSubmission>
{
    public void Configure(EntityTypeBuilder<SpeakingSubmission> entity)
    {
        entity.HasKey(s => s.Id);
        entity.Property(s => s.StudentId).HasMaxLength(450).IsRequired();
        entity.Property(s => s.Status).HasMaxLength(50).IsRequired();
        entity.Property(s => s.RowVersion).IsRowVersion();

        entity.ToTable(t => t.HasCheckConstraint(
            "CK_SpeakingSubmissions_ExactlyOneSource",
            "(HomeworkAssignmentId IS NOT NULL AND LiveExamSessionId IS NULL) OR " +
            "(HomeworkAssignmentId IS NULL AND LiveExamSessionId IS NOT NULL)"));

        entity.HasOne(s => s.HomeworkAssignment)
            .WithMany()
            .HasForeignKey(s => s.HomeworkAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(s => s.LiveExamSession)
            .WithMany()
            .HasForeignKey(s => s.LiveExamSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(s => s.DraftStoredFile)
            .WithMany()
            .HasForeignKey(s => s.DraftStoredFileId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasIndex(s => new { s.StudentId, s.HomeworkAssignmentId })
            .HasFilter("[HomeworkAssignmentId] IS NOT NULL")
            .IsUnique();

        entity.HasIndex(s => new { s.StudentId, s.LiveExamSessionId })
            .HasFilter("[LiveExamSessionId] IS NOT NULL")
            .IsUnique();
    }
}
