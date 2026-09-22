using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace Application.Users.UpdateAvatar;

internal sealed class UpdateUserAvatarCommandHandler(
    UserManager<User> userManager,
    IFileStorageService fileStorageService)
    : ICommandHandler<UpdateUserAvatarCommand>
{
    public async Task<Result> Handle(UpdateUserAvatarCommand command, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(command.UserId.ToString());

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        string? oldProfilePictureUrl = user.ProfilePictureUrl;

        string newProfilePictureUrl = await fileStorageService.UploadAsync(
            command.ContentStream,
            command.FileName,
            command.ContentType,
            cancellationToken);

        user.ProfilePictureUrl = newProfilePictureUrl;

        IdentityResult updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            await fileStorageService.DeleteAsync(newProfilePictureUrl, cancellationToken);
            return Result.Failure(UserErrors.UpdateFailed);
        }

        if (!string.IsNullOrEmpty(oldProfilePictureUrl))
        {
            await fileStorageService.DeleteAsync(oldProfilePictureUrl, cancellationToken);
        }

        user.Raise(new UserAvatarUpdatedDomainEvent(user.Id, oldProfilePictureUrl, newProfilePictureUrl));

        return Result.Success();
    }
}
