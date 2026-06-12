using EnglishTestWeb.Api.Domain.Assignments;
using EnglishTestWeb.Api.Domain.Classes;
using EnglishTestWeb.Api.Domain.TestTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Configurations;

public sealed class HomeworkAssignmentConfiguration : IEntityTypeConfiguration<HomeworkAssignment>
{
    public void Configure(EntityTypeBuilder<HomeworkAssignment> builder)
    {
        builder.ToTable("HomeworkAssignments");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.TeacherId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(entity => entity.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entity => entity.DeadlineAt)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.Property(entity => entity.UpdatedAt)
            .IsRequired();

        builder.HasIndex(entity => new { entity.TeacherId, entity.ClassId });

        builder.HasIndex(entity => new { entity.TeacherId, entity.TestTemplateId });

        builder.HasIndex(entity => entity.ClassId);

        builder.HasOne<Domain.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(entity => entity.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Template)
            .WithMany()
            .HasForeignKey(entity => entity.TestTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SchoolClass>()
            .WithMany()
            .HasForeignKey(entity => entity.ClassId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
