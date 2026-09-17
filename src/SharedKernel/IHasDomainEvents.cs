namespace SharedKernel;

/// <summary>
/// Marks a type as capable of raising and holding domain events, regardless of whether it can
/// inherit from <see cref="Entity"/> (e.g. types that must inherit from a base class from an
/// external library, such as ASP.NET Core Identity's IdentityUser).
/// </summary>
public interface IHasDomainEvents
{
    List<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();

    void Raise(IDomainEvent domainEvent);
}
