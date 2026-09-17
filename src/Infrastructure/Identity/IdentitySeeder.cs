using System.Security.Claims;
using Application.Abstractions.Authorization;
using Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public static class IdentitySeeder
{
    private const string AdministratorRoleName = "Admin";
    private const string PermissionClaimType = "permission";

    public static async Task SeedAsync(RoleManager<Role> roleManager)
    {
        Role? administratorRole = await roleManager.FindByNameAsync(AdministratorRoleName);

        if (administratorRole is null)
        {
            administratorRole = new Role(AdministratorRoleName);
            IdentityResult createRoleResult = await roleManager.CreateAsync(administratorRole);

            if (!createRoleResult.Succeeded)
            {
                throw new InvalidOperationException("Failed to seed the Admin role.");
            }
        }

        await SeedPermissionsAsync(roleManager, administratorRole, Permissions.Organizations.All);
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
