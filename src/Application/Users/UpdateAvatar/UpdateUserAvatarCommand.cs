using Application.Abstractions.Messaging;
using Application.Tickets;

namespace Application.Users.UpdateAvatar;

public sealed record UpdateUserAvatarCommand(
    Guid UserId,
    string FileName,
    string ContentType,
    long FileSize,
    Stream ContentStream) : ICommand;
