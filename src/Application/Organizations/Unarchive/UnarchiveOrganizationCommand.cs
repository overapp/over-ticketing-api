using Application.Abstractions.Messaging;

namespace Application.Organizations.Unarchive;

public sealed record UnarchiveOrganizationCommand(Guid OrganizationId) : ICommand;
