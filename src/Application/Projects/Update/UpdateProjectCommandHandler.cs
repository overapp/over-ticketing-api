using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Projects;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Projects.Update;

internal sealed class UpdateProjectCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateProjectCommand>
{
    public async Task<Result> Handle(UpdateProjectCommand command, CancellationToken cancellationToken)
    {
        Project? project = await context.Projects.SingleOrDefaultAsync(
            currentProject => currentProject.Id == command.ProjectId,
            cancellationToken);

        if (project is null)
        {
            return Result.Failure(ProjectErrors.NotFound(command.ProjectId));
        }

        project.Name = command.Name;
        project.Description = command.Description;
        project.Raise(new ProjectUpdatedDomainEvent(project.Id));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
