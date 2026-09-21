using Domain.Tickets;
using Domain.Users;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Tickets;

internal sealed class TicketMessageConfiguration : IEntityTypeConfiguration<TicketMessage>
{
    public void Configure(EntityTypeBuilder<TicketMessage> builder)
    {
        builder.ToTable("ticket_messages", Schemas.Default);

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Content)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(message => message.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(message => message.Attachments)
            .WithOne()
            .HasForeignKey(attachment => attachment.TicketMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
