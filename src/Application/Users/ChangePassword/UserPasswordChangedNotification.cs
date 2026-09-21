using Application.Abstractions.Notifications;

namespace Application.Users.ChangePassword;

public sealed record UserPasswordChangedNotification(Guid UserId) : INotification;
