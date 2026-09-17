using Application.Abstractions.Messaging;

namespace Application.Organizations.Archive;

public sealed record ArchiveOrganizationCommand(Guid OrganizationId) : ICommand;
