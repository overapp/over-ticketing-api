using Domain.Users;
using SharedKernel;

namespace Domain.Projects;

public sealed class ProjectAssignment : Entity
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Role { get; set; } = RoleNames.User;

    public bool IsAllowedFor(IEnumerable<string> identityRoles)
        => RoleNames.CanAssignRole(identityRoles, Role);
}
