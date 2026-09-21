using Application.Abstractions.Messaging;

namespace Application.Settings.Update;

public sealed record UpdateEmailNotificationSettings(
    bool NotifyOnTicketCreated,
    bool NotifyOnTicketReply);

public sealed record UpdateUserSettingsCommand(
    UpdateEmailNotificationSettings EmailNotifications) : ICommand;
