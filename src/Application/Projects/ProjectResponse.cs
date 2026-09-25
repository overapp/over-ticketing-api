using Domain.Projects;

namespace Application.Projects;

public sealed record ProjectResponse
{
    public Guid Id { get; init; }

    public Guid OrganizationId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public ProjectStatus Status { get; init; }
}
