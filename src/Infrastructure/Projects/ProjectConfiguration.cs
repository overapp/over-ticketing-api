using Domain.Organizations;
using Domain.Projects;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Projects;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects", Schemas.Default);

        builder.HasKey(project => project.Id);

        builder.Property(project => project.Status)
            .HasConversion<string>();

        builder.HasOne<Organization>()
            .WithMany(organization => organization.Projects)
            .HasForeignKey(project => project.OrganizationId);
    }
}
