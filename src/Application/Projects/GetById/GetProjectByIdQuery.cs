using Application.Abstractions.Messaging;
using Application.Projects;

namespace Application.Projects.GetById;

public sealed record GetProjectByIdQuery(Guid ProjectId) : IQuery<ProjectResponse>;
