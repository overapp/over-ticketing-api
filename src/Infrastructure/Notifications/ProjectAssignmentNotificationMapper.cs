using Application.Abstractions.Notifications;
using Application.Projects.AssignUser;
using Domain.Projects;
using SharedKernel;

namespace Infrastructure.Notifications;

internal sealed class ProjectAssignmentNotificationMapper : IDomainEventToNotificationMapper
{
    public INotification? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        UserAssignedToProjectDomainEvent e => new UserAssignedToProjectNotification(e.ProjectId, e.UserId, e.Role),
        _ => null
    };
}
