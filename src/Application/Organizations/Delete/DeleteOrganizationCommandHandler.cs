using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.Delete;

internal sealed class DeleteOrganizationCommandHandler(IApplicationDbContext context)
    : ICommandHandler<DeleteOrganizationCommand>
{
    public async Task<Result> Handle(DeleteOrganizationCommand command, CancellationToken cancellationToken)
    {
        Organization? organization = await context.Organizations.SingleOrDefaultAsync(
            currentOrganization => currentOrganization.Id == command.OrganizationId,
            cancellationToken);

        if (organization is null)
        {
            return Result.Failure(OrganizationErrors.NotFound(command.OrganizationId));
        }

        organization.Raise(new OrganizationDeletedDomainEvent(organization.Id));

        context.Organizations.Remove(organization);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
