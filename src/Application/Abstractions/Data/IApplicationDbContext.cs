using Domain.Organizations;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectAssignment> ProjectAssignments { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketMessage> TicketMessages { get; }
    DbSet<TicketAttachment> TicketAttachments { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Role> Roles { get; }
    DbSet<IdentityUserRole<Guid>> UserRoles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
