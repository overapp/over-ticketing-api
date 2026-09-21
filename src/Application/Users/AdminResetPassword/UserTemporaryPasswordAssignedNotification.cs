using Application.Abstractions.Notifications;

namespace Application.Users.AdminResetPassword;

public sealed record UserTemporaryPasswordAssignedNotification(
    Guid UserId,
    string TemporaryPassword) : INotification;
