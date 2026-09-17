using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.Unarchive;

internal sealed class UnarchiveOrganizationCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UnarchiveOrganizationCommand>
{
    public async Task<Result> Handle(UnarchiveOrganizationCommand command, CancellationToken cancellationToken)
    {
        Organization? organization = await context.Organizations.SingleOrDefaultAsync(
            currentOrganization => currentOrganization.Id == command.OrganizationId,
            cancellationToken);

        if (organization is null)
        {
            return Result.Failure(OrganizationErrors.NotFound(command.OrganizationId));
        }

        if (organization.Status == OrganizationStatus.Active)
        {
            return Result.Failure(OrganizationErrors.AlreadyActive(command.OrganizationId));
        }

        organization.Status = OrganizationStatus.Active;
        organization.Raise(new OrganizationUnarchivedDomainEvent(organization.Id));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
