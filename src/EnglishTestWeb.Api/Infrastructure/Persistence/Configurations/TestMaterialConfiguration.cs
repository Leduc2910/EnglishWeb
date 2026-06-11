using EnglishTestWeb.Api.Domain.TestTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Configurations;

public sealed class TestMaterialConfiguration : IEntityTypeConfiguration<TestMaterial>
{
    public void Configure(EntityTypeBuilder<TestMaterial> builder)
    {
        builder.ToTable("TestMaterials");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Role)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.HasIndex(entity => new { entity.TemplateId, entity.Role })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.HasOne(entity => entity.Template)
            .WithMany()
            .HasForeignKey(entity => entity.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.StoredFile)
            .WithMany()
            .HasForeignKey(entity => entity.StoredFileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
