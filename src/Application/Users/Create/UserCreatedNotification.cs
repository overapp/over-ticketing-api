using Application.Abstractions.Notifications;

namespace Application.Users.Create;

public sealed record UserCreatedNotification(Guid UserId, string? TemporaryPassword = null) : INotification;
