using Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// Placeholder for seeding default Identity data (roles, permission claims, an initial admin
/// user, etc.) on application startup. Fill this in with whatever your application needs;
/// it's invoked once at startup in Web.Api's Program.cs.
/// </summary>
public static class IdentitySeeder
{
    public static Task SeedAsync(RoleManager<Role> roleManager, UserManager<User> userManager)
    {
        // TODO: Seed default roles/permissions/users here, e.g.:
        //
        // if (await roleManager.FindByNameAsync("Admin") is null)
        // {
        //     var adminRole = new Role("Admin");
        //     await roleManager.CreateAsync(adminRole);
        //     await roleManager.AddClaimAsync(adminRole, new Claim("permission", "users:access"));
        // }

        return Task.CompletedTask;
    }
}
