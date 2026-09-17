using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.Archive;

internal sealed class ArchiveOrganizationCommandHandler(IApplicationDbContext context)
    : ICommandHandler<ArchiveOrganizationCommand>
{
    public async Task<Result> Handle(ArchiveOrganizationCommand command, CancellationToken cancellationToken)
    {
        Organization? organization = await context.Organizations.SingleOrDefaultAsync(
            currentOrganization => currentOrganization.Id == command.OrganizationId,
            cancellationToken);

        if (organization is null)
        {
            return Result.Failure(OrganizationErrors.NotFound(command.OrganizationId));
        }

        if (organization.Status == OrganizationStatus.Archived)
        {
            return Result.Failure(OrganizationErrors.AlreadyArchived(command.OrganizationId));
        }

        organization.Status = OrganizationStatus.Archived;
        organization.Raise(new OrganizationArchivedDomainEvent(organization.Id));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
