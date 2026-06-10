using EnglishTestWeb.Api.Domain.TestTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Configurations;

public sealed class TestTemplateConfiguration : IEntityTypeConfiguration<TestTemplate>
{
    public void Configure(EntityTypeBuilder<TestTemplate> builder)
    {
        builder.ToTable("TestTemplates");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.TeacherId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(entity => entity.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.Skill)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entity => entity.Description)
            .HasMaxLength(2000);

        builder.Property(entity => entity.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.Property(entity => entity.UpdatedAt)
            .IsRequired();

        builder.HasIndex(entity => new { entity.TeacherId, entity.Status });

        builder.HasIndex(entity => new { entity.TeacherId, entity.Title });

        builder.HasOne<Domain.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(entity => entity.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
