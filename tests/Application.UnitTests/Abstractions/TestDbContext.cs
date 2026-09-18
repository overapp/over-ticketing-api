using Application.Abstractions.Data;
using Domain.Organizations;
using Domain.Projects;
using Domain.Users;
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

    public DbSet<User> Users { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
}
