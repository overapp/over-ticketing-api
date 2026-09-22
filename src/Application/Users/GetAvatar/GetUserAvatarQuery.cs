using Application.Abstractions.Messaging;

namespace Application.Users.GetAvatar;

public sealed record GetUserAvatarQuery(Guid UserId) : IQuery<AvatarDownloadResponse>;
