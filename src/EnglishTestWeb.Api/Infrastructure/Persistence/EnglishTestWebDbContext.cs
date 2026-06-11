using EnglishTestWeb.Api.Domain.Assignments;
using EnglishTestWeb.Api.Domain.Classes;
using EnglishTestWeb.Api.Domain.Files;
using EnglishTestWeb.Api.Domain.Identity;
using EnglishTestWeb.Api.Domain.LiveExams;
using EnglishTestWeb.Api.Domain.TestTemplates;
using EnglishTestWeb.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EnglishTestWeb.Api.Infrastructure.Persistence;

public sealed class EnglishTestWebDbContext(DbContextOptions<EnglishTestWebDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<SchoolClass> Classes => Set<SchoolClass>();

    public DbSet<ClassMembership> ClassMemberships => Set<ClassMembership>();

    public DbSet<TestTemplate> TestTemplates => Set<TestTemplate>();

    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    public DbSet<TestMaterial> TestMaterials => Set<TestMaterial>();

    public DbSet<AnswerKeyVersion> AnswerKeyVersions => Set<AnswerKeyVersion>();

    public DbSet<HomeworkAssignment> HomeworkAssignments => Set<HomeworkAssignment>();

    public DbSet<LiveExamSession> LiveExamSessions => Set<LiveExamSession>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(EnglishTestWebDbContext).Assembly);
        builder.Entity<IdentityRole>().HasData(IdentityRoleDefinitions.All);
    }
}
