using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using SharedKernel;

namespace Application.Users.Delete;

internal sealed class DeleteUserCommandHandler(
    UserManager<User> userManager,
    IUserContext userContext,
    HybridCache cache)
    : ICommandHandler<DeleteUserCommand>
{
    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == userContext.UserId)
        {
            return Result.Failure(UserErrors.CannotDeleteSelf);
        }

        User? user = await userManager.FindByIdAsync(command.UserId.ToString());

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        if (await userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            IList<User> admins = await userManager.GetUsersInRoleAsync(RoleNames.Admin);

            if (admins.Count <= 1)
            {
                return Result.Failure(UserErrors.CannotDeleteLastAdmin);
            }
        }

        user.Raise(new UserDeletedDomainEvent(user.Id));

        IdentityResult deleteResult = await userManager.DeleteAsync(user);

        if (!deleteResult.Succeeded)
        {
            return Result.Failure(IdentityErrorMapper.MapFirst(deleteResult.Errors));
        }

        await cache.RemoveAsync($"permissions-{user.Id}", cancellationToken);

        return Result.Success();
    }
}
