using EnglishTestWeb.Api.Domain.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Configurations;

public sealed class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> builder)
    {
        builder.ToTable("Classes");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.ClassCode)
            .HasMaxLength(12)
            .IsRequired();

        builder.HasIndex(entity => entity.ClassCode)
            .IsUnique();

        builder.Property(entity => entity.TeacherId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(entity => entity.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.HasOne<Domain.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(entity => entity.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
