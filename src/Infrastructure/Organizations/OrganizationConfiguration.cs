using Domain.Organizations;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Organizations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations", Schemas.Default);

        builder.HasKey(organization => organization.Id);

        builder.Property(organization => organization.Status)
            .HasConversion<string>();
    }
}
