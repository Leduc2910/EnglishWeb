using EnglishTestWeb.Api.Domain.TestTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Configurations;

public sealed class AnswerKeyVersionConfiguration : IEntityTypeConfiguration<AnswerKeyVersion>
{
    public void Configure(EntityTypeBuilder<AnswerKeyVersion> builder)
    {
        builder.ToTable("AnswerKeyVersions");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entity => entity.ScoringMode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entity => entity.TotalScore)
            .HasPrecision(9, 2);

        builder.Property(entity => entity.RowsJson)
            .HasMaxLength(-1)
            .IsRequired();

        builder.Property(entity => entity.RowVersion)
            .IsRowVersion();

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.Property(entity => entity.UpdatedAt)
            .IsRequired();

        builder.HasIndex(entity => entity.TemplateId)
            .IsUnique();

        builder.HasOne(entity => entity.Template)
            .WithMany()
            .HasForeignKey(entity => entity.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
