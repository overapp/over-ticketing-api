using Domain.Projects;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace Domain.Users;

/// <summary>
/// The application user, built on top of ASP.NET Core Identity's <see cref="IdentityUser{TKey}"/>.
/// Identity already provides Email, UserName, PasswordHash, PhoneNumber, lockout, etc., so only
/// the properties that are specific to this application are added here.
/// </summary>
public sealed class User : IdentityUser<Guid>, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public ICollection<ProjectAssignment> ProjectAssignments { get; set; } = [];

    public List<IDomainEvent> DomainEvents => [.. _domainEvents];

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
