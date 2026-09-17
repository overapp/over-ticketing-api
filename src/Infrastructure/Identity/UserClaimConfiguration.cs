using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Users;
using Infrastructure.Database;

namespace Infrastructure.Identity;

internal sealed class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder)
    {
        builder.ToTable("user_claims", Schemas.Identity);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(uc => uc.UserId)
            .HasConstraintName("fk_user_claims_users_user_id")
            .IsRequired();
    }
}
