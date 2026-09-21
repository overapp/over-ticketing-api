using System.Security.Claims;
using Application.Abstractions.Authorization;
using Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public static class IdentitySeeder
{
    private const string PermissionClaimType = "permission";

    public static async Task SeedAsync(RoleManager<Role> roleManager)
    {
        await SeedRoleAsync(roleManager, RoleNames.User, []);
        await SeedRoleAsync(roleManager, RoleNames.Support, []);
        await SeedRoleAsync(roleManager, RoleNames.Admin, [
            .. Permissions.Organizations.All,
            .. Permissions.Projects.All,
            .. Permissions.Users.All,
            .. Permissions.Tickets.All,
            .. Permissions.Wiki.All,
            .. Permissions.Categories.All]);
    }

    private static async Task SeedRoleAsync(RoleManager<Role> roleManager, string roleName, IReadOnlyCollection<string> permissions)
    {
        Role? role = await roleManager.FindByNameAsync(roleName);

        if (role is not null)
        {
            if (permissions is not null && permissions.Any())
            {
                await SeedPermissionsAsync(roleManager, role, permissions);
            }

            return;
        }

        IdentityResult createRoleResult = await roleManager.CreateAsync(new Role(roleName));

        if (!createRoleResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to seed the {roleName} role.");
        }

        Role newRole = await roleManager.FindByNameAsync(roleName);

        if (permissions is not null && permissions.Any())
        {
            await SeedPermissionsAsync(roleManager, newRole!, permissions);
        }
    }

    private static async Task SeedPermissionsAsync(
        RoleManager<Role> roleManager,
        Role administratorRole,
        IReadOnlyCollection<string> permissions)
    {
        IList<Claim> administratorClaims = await roleManager.GetClaimsAsync(administratorRole);

        foreach (string permission in permissions.Where(permission =>
                     !administratorClaims.Any(claim =>
                         claim.Type == PermissionClaimType && claim.Value == permission)))
        {
            IdentityResult addClaimResult = await roleManager.AddClaimAsync(
                administratorRole,
                new Claim(PermissionClaimType, permission));

            if (!addClaimResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to seed the {permission} permission for the Admin role.");
            }
        }
    }
}
