using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Users;
using Infrastructure.Database;

namespace Infrastructure.Identity;

internal sealed class RoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
    {
        builder.ToTable("role_claims", Schemas.Identity);

        builder
            .HasOne<Role>()
            .WithMany()
            .HasForeignKey(rc => rc.RoleId)
            .HasConstraintName("fk_role_claims_roles_role_id")
            .IsRequired();
    }
}
