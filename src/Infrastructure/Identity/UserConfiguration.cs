using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Infrastructure.Database;

namespace Infrastructure.Identity;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", Schemas.Identity);

        builder.HasKey(u => u.Id);

        builder.HasIndex(u => u.NormalizedEmail).HasDatabaseName("email_index");

        builder.HasIndex(u => u.NormalizedUserName).IsUnique().HasDatabaseName("user_name_index");
    }
}
