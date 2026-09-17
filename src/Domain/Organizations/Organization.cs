using SharedKernel;

namespace Domain.Organizations;

public sealed class Organization : Entity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Logo { get; set; } = string.Empty;

    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;
}
