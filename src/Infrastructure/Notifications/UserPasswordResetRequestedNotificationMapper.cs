using Application.Abstractions.Notifications;
using Application.Users.ForgotPassword;
using Domain.Users;
using SharedKernel;

namespace Infrastructure.Notifications;

internal sealed class UserPasswordResetRequestedNotificationMapper : IDomainEventToNotificationMapper
{
    public INotification? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        UserPasswordResetRequestedDomainEvent e => new UserPasswordResetRequestedNotification(
            e.UserId,
            e.Email,
            e.ResetToken),
        _ => null
    };
}
