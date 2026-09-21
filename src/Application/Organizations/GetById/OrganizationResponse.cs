using Domain.Organizations;

namespace Application.Organizations.GetById;

public sealed record OrganizationResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Logo { get; init; } = string.Empty;

    public OrganizationStatus Status { get; init; }
}
