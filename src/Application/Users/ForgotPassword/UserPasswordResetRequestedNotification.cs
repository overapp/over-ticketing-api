using Application.Abstractions.Notifications;

namespace Application.Users.ForgotPassword;

public sealed record UserPasswordResetRequestedNotification(
    Guid UserId,
    string Email,
    string ResetToken) : INotification;
