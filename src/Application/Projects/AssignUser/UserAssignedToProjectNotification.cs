using Application.Abstractions.Notifications;

namespace Application.Projects.AssignUser;

public sealed record UserAssignedToProjectNotification(
    Guid ProjectId,
    Guid UserId,
    string Role) : INotification;
