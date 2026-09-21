using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Tickets;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets", Schemas.Default);

        builder.HasKey(ticket => ticket.Id);

        builder.Property(ticket => ticket.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(ticket => ticket.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(ticket => ticket.Priority)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(ticket => ticket.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ticket => ticket.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ticket => ticket.AssignedToUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<TicketCategory>()
            .WithMany()
            .HasForeignKey(ticket => ticket.CategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(ticket => ticket.Messages)
            .WithOne()
            .HasForeignKey(message => message.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
