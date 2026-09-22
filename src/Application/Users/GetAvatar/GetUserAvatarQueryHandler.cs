using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.GetAvatar;

internal sealed class GetUserAvatarQueryHandler(
    IApplicationDbContext context,
    IFileStorageService fileStorageService)
    : IQueryHandler<GetUserAvatarQuery, AvatarDownloadResponse>
{
    public async Task<Result<AvatarDownloadResponse>> Handle(
        GetUserAvatarQuery query,
        CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<AvatarDownloadResponse>(UserErrors.NotFound(query.UserId));
        }

        if (string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            return Result.Failure<AvatarDownloadResponse>(
                Error.NotFound("Users.AvatarNotFound", "User has no avatar uploaded."));
        }

        Stream? stream = await fileStorageService.DownloadAsync(user.ProfilePictureUrl, cancellationToken);
        if (stream is null)
        {
            return Result.Failure<AvatarDownloadResponse>(
                Error.NotFound("Users.AvatarFileNotFound", "The avatar file was not found on storage."));
        }

        return new AvatarDownloadResponse(stream, "image/*");
    }
}
