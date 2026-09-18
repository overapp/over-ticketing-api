using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Identity;

namespace Domain.Users;

public sealed class Role : IdentityRole<Guid>
{
    public Role()
    {
    }

    public Role(string name)
        : base(name)
    {
    }
}

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Support = "Support";

    public static readonly IReadOnlyCollection<string> All =
    [Admin, User, Support];

    public static readonly IReadOnlyCollection<string> ProjectRoles =
    [User, Support];

    public static bool IsProjectRole(string roleName) =>
        ProjectRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);

    public static bool CanAssignRole(IEnumerable<string> identityRoles, string projectRole)
    {
        if (string.IsNullOrWhiteSpace(projectRole))
        {
            return false;
        }

        return identityRoles.Any(roleName =>
            string.Equals(roleName, projectRole, StringComparison.OrdinalIgnoreCase));
    }
}
