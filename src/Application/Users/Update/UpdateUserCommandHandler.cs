using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using SharedKernel;

namespace Application.Users.Update;

internal sealed class UpdateUserCommandHandler(
    UserManager<User> userManager,
    IUserContext userContext,
    HybridCache cache)
    : ICommandHandler<UpdateUserCommand>
{
    public async Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(command.UserId.ToString());

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        if (!string.Equals(user.Email, command.Email, StringComparison.OrdinalIgnoreCase))
        {
            User? existingUser = await userManager.FindByEmailAsync(command.Email);

            if (existingUser is not null && existingUser.Id != user.Id)
            {
                return Result.Failure(UserErrors.DuplicateEmail);
            }
        }

        var targetRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { RoleNames.User };

        if (command.Roles is not null)
        {
            foreach (string role in command.Roles)
            {
                string? matchedRole = RoleNames.All.FirstOrDefault(r =>
                    string.Equals(r, role, StringComparison.OrdinalIgnoreCase));

                if (matchedRole is not null)
                {
                    targetRoles.Add(matchedRole);
                }
            }
        }

        IList<string> currentRoles = await userManager.GetRolesAsync(user);

        bool wasAdmin = currentRoles.Contains(RoleNames.Admin, StringComparer.OrdinalIgnoreCase);
        bool willBeAdmin = targetRoles.Contains(RoleNames.Admin);

        if (wasAdmin && !willBeAdmin)
        {
            if (user.Id == userContext.UserId)
            {
                return Result.Failure(UserErrors.CannotDemoteSelf);
            }

            IList<User> admins = await userManager.GetUsersInRoleAsync(RoleNames.Admin);

            if (admins.Count <= 1)
            {
                return Result.Failure(UserErrors.CannotDemoteLastAdmin);
            }
        }

        user.FirstName = command.FirstName;
        user.LastName = command.LastName;
        user.Email = command.Email;
        user.UserName = command.Email;

        IdentityResult updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return Result.Failure(IdentityErrorMapper.MapFirst(updateResult.Errors));
        }

        var rolesToRemove = currentRoles.Except(targetRoles, StringComparer.OrdinalIgnoreCase).ToList();
        var rolesToAdd = targetRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();

        if (rolesToRemove.Count > 0)
        {
            IdentityResult removeRolesResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);

            if (!removeRolesResult.Succeeded)
            {
                return Result.Failure(IdentityErrorMapper.MapFirst(removeRolesResult.Errors));
            }
        }

        if (rolesToAdd.Count > 0)
        {
            IdentityResult addRolesResult = await userManager.AddToRolesAsync(user, rolesToAdd);

            if (!addRolesResult.Succeeded)
            {
                return Result.Failure(IdentityErrorMapper.MapFirst(addRolesResult.Errors));
            }
        }

        await cache.RemoveAsync($"permissions-{user.Id}", cancellationToken);

        user.Raise(new UserUpdatedDomainEvent(user.Id));

        return Result.Success();
    }
}
