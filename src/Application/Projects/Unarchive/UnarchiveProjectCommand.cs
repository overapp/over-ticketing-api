using Application.Abstractions.Messaging;

namespace Application.Projects.Unarchive;

public sealed record UnarchiveProjectCommand(Guid ProjectId) : ICommand;
