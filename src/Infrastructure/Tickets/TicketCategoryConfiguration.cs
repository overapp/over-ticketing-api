using Domain.Projects;
using Domain.Tickets;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Tickets;

internal sealed class TicketCategoryConfiguration : IEntityTypeConfiguration<TicketCategory>
{
    public void Configure(EntityTypeBuilder<TicketCategory> builder)
    {
        builder.ToTable("ticket_categories", Schemas.Default);

        builder.HasKey(tc => tc.Id);

        builder.Property(tc => tc.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(tc => tc.Description)
            .HasMaxLength(500)
            .HasDefaultValue(string.Empty);

        builder.Property(tc => tc.BackgroundColor)
            .HasMaxLength(9)
            .HasDefaultValue("#64748B")
            .IsRequired();

        builder.Property(tc => tc.ForegroundColor)
            .HasMaxLength(9)
            .HasDefaultValue("#FFFFFF")
            .IsRequired();

        builder.Property(tc => tc.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(tc => tc.ProjectId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(tc => new { tc.ProjectId, tc.Name })
            .HasFilter("[project_id] IS NOT NULL")
            .IsUnique();

        builder.HasIndex(tc => tc.Name)
            .HasFilter("[project_id] IS NULL")
            .IsUnique();
    }
}
