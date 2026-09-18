using Domain.Projects;
using Domain.Users;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Projects;

internal sealed class ProjectAssignmentConfiguration : IEntityTypeConfiguration<ProjectAssignment>
{
    public void Configure(EntityTypeBuilder<ProjectAssignment> builder)
    {
        builder.ToTable("project_assignments", Schemas.Default);

        builder.HasKey(projectAssignment => projectAssignment.Id);

        builder.Property(projectAssignment => projectAssignment.Role)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasOne(projectAssignment => projectAssignment.Project)
            .WithMany(project => project.ProjectAssignments)
            .HasForeignKey(projectAssignment => projectAssignment.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(projectAssignment => projectAssignment.User)
            .WithMany(user => user.ProjectAssignments)
            .HasForeignKey(projectAssignment => projectAssignment.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(projectAssignment => new { projectAssignment.ProjectId, projectAssignment.UserId })
            .IsUnique();
    }
}
