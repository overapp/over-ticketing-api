using Application.Abstractions.Messaging;

namespace Application.Projects.Archive;

public sealed record ArchiveProjectCommand(Guid ProjectId) : ICommand;
