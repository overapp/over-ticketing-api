using Application.Abstractions.Messaging;

namespace Application.Users.DeleteAvatar;

public sealed record DeleteUserAvatarCommand(Guid UserId) : ICommand;
