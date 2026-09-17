using Application.Abstractions.Messaging;

namespace Application.Organizations.Delete;

public sealed record DeleteOrganizationCommand(Guid OrganizationId) : ICommand;
