using Domain.Projects;
using Domain.Users;
using Domain.Wiki;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Wiki;

internal sealed class WikiPageConfiguration : IEntityTypeConfiguration<WikiPage>
{
    public void Configure(EntityTypeBuilder<WikiPage> builder)
    {
        builder.ToTable("wiki_pages", Schemas.Default);

        builder.HasKey(wp => wp.Id);

        builder.Property(wp => wp.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(wp => wp.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(wp => wp.Content)
            .IsRequired();

        builder.Property(wp => wp.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(wp => wp.IsInternalOnly)
            .IsRequired();

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(wp => wp.ProjectId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<WikiPage>()
            .WithMany()
            .HasForeignKey(wp => wp.ParentPageId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(wp => wp.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(wp => wp.UpdatedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index for project wiki pages by slug
        builder.HasIndex(wp => new { wp.ProjectId, wp.Slug })
            .HasFilter("[project_id] IS NOT NULL")
            .IsUnique();

        // Unique index for global wiki pages by slug
        builder.HasIndex(wp => wp.Slug)
            .HasFilter("[project_id] IS NULL")
            .IsUnique();
    }
}
