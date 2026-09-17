using System.Security.Claims;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;

namespace Infrastructure.Authorization;

internal sealed class PermissionProvider(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    HybridCache cache)
{
    private const string PermissionClaimType = "permission";

    public async Task<HashSet<string>> GetForUserIdAsync(Guid userId)
    {
        return await cache.GetOrCreateAsync(
            $"permissions-{userId}",
            async _ =>
            {
                User? user = await userManager.FindByIdAsync(userId.ToString());

                if (user is null)
                {
                    return [];
                }

                IList<string> roleNames = await userManager.GetRolesAsync(user);

                var permissions = new HashSet<string>();

                foreach (string roleName in roleNames)
                {
                    Role? role = await roleManager.FindByNameAsync(roleName);

                    if (role is null)
                    {
                        continue;
                    }

                    IList<Claim> claims = await roleManager.GetClaimsAsync(role);

                    permissions.UnionWith(
                        claims.Where(claim => claim.Type == PermissionClaimType).Select(claim => claim.Value));
                }

                return permissions;
            });
    }
}
