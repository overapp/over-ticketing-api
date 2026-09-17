using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Infrastructure.Database;

namespace Infrastructure.Identity;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", Schemas.Identity);

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.NormalizedName).IsUnique().HasDatabaseName("role_name_index");
    }
}
