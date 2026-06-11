using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Domain.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Configurations;

public sealed class LiveExamSessionConfiguration : IEntityTypeConfiguration<LiveExamSession>
{
    public void Configure(EntityTypeBuilder<LiveExamSession> builder)
    {
        builder.ToTable("LiveExamSessions");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.TeacherId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(entity => entity.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.Property(entity => entity.UpdatedAt)
            .IsRequired();

        builder.HasIndex(entity => new { entity.TeacherId, entity.ClassId });

        builder.HasIndex(entity => new { entity.TeacherId, entity.TestTemplateId });

        builder.HasOne<Domain.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(entity => entity.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TestTemplate>()
            .WithMany()
            .HasForeignKey(entity => entity.TestTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SchoolClass>()
            .WithMany()
            .HasForeignKey(entity => entity.ClassId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
