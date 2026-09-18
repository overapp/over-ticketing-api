using Application.Abstractions.Messaging;

namespace Application.Projects.Create;

public sealed record CreateProjectCommand(Guid OrganizationId, string Name, string Description) : ICommand<Guid>;
