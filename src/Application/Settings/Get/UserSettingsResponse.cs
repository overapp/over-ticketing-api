namespace Application.Settings.Get;

public sealed record EmailNotificationSettingsResponse(
    bool NotifyOnTicketCreated,
    bool NotifyOnTicketReply);

public sealed record UserSettingsResponse(
    EmailNotificationSettingsResponse EmailNotifications);
