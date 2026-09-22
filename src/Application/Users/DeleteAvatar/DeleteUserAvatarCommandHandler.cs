using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace Application.Users.DeleteAvatar;

internal sealed class DeleteUserAvatarCommandHandler(
    UserManager<User> userManager,
    IFileStorageService fileStorageService)
    : ICommandHandler<DeleteUserAvatarCommand>
{
    public async Task<Result> Handle(DeleteUserAvatarCommand command, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(command.UserId.ToString());

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        string? oldProfilePictureUrl = user.ProfilePictureUrl;

        user.ProfilePictureUrl = null;

        IdentityResult updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return Result.Failure(UserErrors.UpdateFailed);
        }

        if (!string.IsNullOrEmpty(oldProfilePictureUrl))
        {
            await fileStorageService.DeleteAsync(oldProfilePictureUrl, cancellationToken);
        }

        user.Raise(new UserAvatarUpdatedDomainEvent(user.Id, oldProfilePictureUrl, null));

        return Result.Success();
    }
}
