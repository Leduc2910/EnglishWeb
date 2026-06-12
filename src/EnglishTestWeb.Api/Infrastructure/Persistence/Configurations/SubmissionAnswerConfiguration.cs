using EnglishTestWeb.Api.Domain.Submissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Configurations;

public sealed class SubmissionAnswerConfiguration : IEntityTypeConfiguration<SubmissionAnswer>
{
    public void Configure(EntityTypeBuilder<SubmissionAnswer> entity)
    {
        entity.HasKey(a => a.Id);

        entity.Property(a => a.Answer).HasMaxLength(500);

        entity.HasIndex(a => new { a.SubmissionId, a.QuestionNumber }).IsUnique();

        entity.HasOne(a => a.Submission)
            .WithMany(s => s.Answers)
            .HasForeignKey(a => a.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
