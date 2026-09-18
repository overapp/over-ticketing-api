using Domain.Users;
using SharedKernel;

namespace Domain.Projects;

public sealed class Project : Entity
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    public ICollection<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();

    public IEnumerable<User> Users => ProjectAssignments.Select(pa => pa.User);
}
