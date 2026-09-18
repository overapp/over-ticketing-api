using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Organizations;
using Domain.Projects;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Projects.Create;

internal sealed class CreateProjectCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateProjectCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProjectCommand command, CancellationToken cancellationToken)
    {
        bool organizationExists = await context.Organizations.AnyAsync(
            organization => organization.Id == command.OrganizationId,
            cancellationToken);

        if (!organizationExists)
        {
            return Result.Failure<Guid>(OrganizationErrors.NotFound(command.OrganizationId));
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = command.OrganizationId,
            Name = command.Name,
            Description = command.Description
        };

        project.Raise(new ProjectCreatedDomainEvent(project.Id));

        context.Projects.Add(project);
        await context.SaveChangesAsync(cancellationToken);

        return project.Id;
    }
}
