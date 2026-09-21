using Domain.Users;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Identity;

internal sealed class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("user_settings", Schemas.Identity);

        builder.HasKey(s => s.UserId);

        builder.Property(s => s.UserId)
            .ValueGeneratedNever();

        builder.Property(s => s.NotifyOnTicketCreated)
            .HasDefaultValue(true);

        builder.Property(s => s.NotifyOnTicketReply)
            .HasDefaultValue(true);

        builder.HasOne(s => s.User)
            .WithOne(u => u.Settings)
            .HasForeignKey<UserSettings>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
