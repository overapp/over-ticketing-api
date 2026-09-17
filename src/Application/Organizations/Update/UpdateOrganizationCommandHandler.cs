using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.Update;

internal sealed class UpdateOrganizationCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateOrganizationCommand>
{
    public async Task<Result> Handle(UpdateOrganizationCommand command, CancellationToken cancellationToken)
    {
        Organization? organization = await context.Organizations.SingleOrDefaultAsync(
            currentOrganization => currentOrganization.Id == command.OrganizationId,
            cancellationToken);

        if (organization is null)
        {
            return Result.Failure(OrganizationErrors.NotFound(command.OrganizationId));
        }

        organization.Name = command.Name;
        organization.Logo = command.Logo;
        organization.Raise(new OrganizationUpdatedDomainEvent(organization.Id));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
