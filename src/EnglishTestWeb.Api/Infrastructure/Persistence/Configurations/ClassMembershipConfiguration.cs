using EnglishTestWeb.Api.Domain.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnglishTestWeb.Api.Infrastructure.Persistence.Configurations;

public sealed class ClassMembershipConfiguration : IEntityTypeConfiguration<ClassMembership>
{
    public void Configure(EntityTypeBuilder<ClassMembership> builder)
    {
        builder.ToTable("ClassMemberships");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.StudentId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(entity => entity.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.HasIndex(entity => new { entity.ClassId, entity.StudentId })
            .IsUnique();

        builder.HasOne(entity => entity.Class)
            .WithMany(entity => entity.Memberships)
            .HasForeignKey(entity => entity.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(entity => entity.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
