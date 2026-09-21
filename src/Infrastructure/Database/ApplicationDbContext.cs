using System.Text.Json;
using Application.Abstractions.Data;
using Domain.Organizations;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Domain.Wiki;
using Infrastructure.DomainEvents;
using Infrastructure.Outbox;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Infrastructure.Database;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDomainEventsDispatcher domainEventsDispatcher)
    : IdentityDbContext<
        User,
        Role,
        Guid,
        IdentityUserClaim<Guid>,
        IdentityUserRole<Guid>,
        IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>,
        IdentityUserToken<Guid>>(options), IApplicationDbContext
{
    public DbSet<Organization> Organizations { get; set; }

    public DbSet<Project> Projects { get; set; }

    public DbSet<ProjectAssignment> ProjectAssignments { get; set; }

    public DbSet<Ticket> Tickets { get; set; }

    public DbSet<TicketCategory> TicketCategories { get; set; }

    public DbSet<TicketMessage> TicketMessages { get; set; }

    public DbSet<TicketAttachment> TicketAttachments { get; set; }

    public DbSet<WikiPage> WikiPages { get; set; }

    public DbSet<UserSettings> UserSettings { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.HasDefaultSchema(Schemas.Default);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // When should you publish domain events?
        //
        // 1. BEFORE calling SaveChangesAsync
        //     - domain events are part of the same transaction
        //     - immediate consistency
        // 2. AFTER calling SaveChangesAsync
        //     - domain events are a separate transaction
        //     - eventual consistency
        //     - handlers can fail

        List<IDomainEvent> domainEvents = ExtractDomainEvents();

        InsertOutboxMessages(domainEvents);

        int result = await base.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync(domainEvents);

        return result;
    }

    private void InsertOutboxMessages(List<IDomainEvent> domainEvents)
    {
        if (domainEvents.Count == 0)
        {
            return;
        }

        DateTime utcNow = DateTime.UtcNow;

        foreach (IDomainEvent domainEvent in domainEvents)
        {
            Type eventType = domainEvent.GetType();
            OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = eventType.AssemblyQualifiedName ?? eventType.FullName ?? eventType.Name,
                Content = JsonSerializer.Serialize(domainEvent, eventType),
                OccurredOnUtc = utcNow
            });
        }
    }

    private async Task PublishDomainEventsAsync(IEnumerable<IDomainEvent> domainEvents)
    {
        await domainEventsDispatcher.DispatchAsync(domainEvents);
    }

    private List<IDomainEvent> ExtractDomainEvents()
    {
        var domainEvents = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                List<IDomainEvent> domainEvents = entity.DomainEvents;

                entity.ClearDomainEvents();

                return domainEvents;
            })
            .ToList();
        return domainEvents;
    }
}
