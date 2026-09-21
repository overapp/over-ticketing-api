using Application.Abstractions.Messaging;

namespace Application.Projects.GetById;

public sealed record GetProjectByIdQuery(Guid ProjectId) : IQuery<ProjectResponse>;
