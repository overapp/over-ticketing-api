using Application.Abstractions.Data;
using Domain.Organizations;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Domain.Wiki;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.UnitTests.Abstractions;

/// <summary>
/// A lightweight in-memory <see cref="DbContext"/> that implements <see cref="IApplicationDbContext"/>
/// so Application handlers can be unit tested without referencing the Infrastructure layer.
/// </summary>
public sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Organization> Organizations { get; set; }

    public DbSet<Project> Projects { get; set; }

    public DbSet<ProjectAssignment> ProjectAssignments { get; set; }

    public DbSet<Ticket> Tickets { get; set; }

    public DbSet<TicketCategory> TicketCategories { get; set; }

    public DbSet<TicketMessage> TicketMessages { get; set; }

    public DbSet<TicketAttachment> TicketAttachments { get; set; }

    public DbSet<WikiPage> WikiPages { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<UserSettings> UserSettings { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<IdentityUserRole<Guid>> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityUserRole<Guid>>().HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<Project>().Ignore(p => p.Users);

        modelBuilder.Entity<ProjectAssignment>()
            .HasOne(pa => pa.Project)
            .WithMany(p => p.ProjectAssignments)
            .HasForeignKey(pa => pa.ProjectId);

        modelBuilder.Entity<ProjectAssignment>()
            .HasOne(pa => pa.User)
            .WithMany(u => u.ProjectAssignments)
            .HasForeignKey(pa => pa.UserId);

        modelBuilder.Entity<UserSettings>()
            .HasKey(s => s.UserId);

        modelBuilder.Entity<UserSettings>()
            .HasOne(s => s.User)
            .WithOne(u => u.Settings)
            .HasForeignKey<UserSettings>(s => s.UserId);
    }
}
