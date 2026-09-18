using Application.Abstractions.Messaging;

namespace Application.Projects.Update;

public sealed record UpdateProjectCommand(Guid ProjectId, string Name, string Description) : ICommand;
