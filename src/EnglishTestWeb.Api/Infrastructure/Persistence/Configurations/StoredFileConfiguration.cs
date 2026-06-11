using EnglishTestWeb.Api.Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Configurations;

public sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFiles");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.StorageKey)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(entity => entity.OriginalFileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(entity => entity.ContentType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entity => entity.ChecksumSha256)
            .HasMaxLength(64);

        builder.Property(entity => entity.OwnerUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(entity => entity.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.Property(entity => entity.UpdatedAt)
            .IsRequired();

        builder.HasIndex(entity => entity.StorageKey)
            .IsUnique();

        builder.HasOne<Domain.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(entity => entity.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
