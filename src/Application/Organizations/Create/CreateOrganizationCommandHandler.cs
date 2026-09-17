using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Organizations;
using SharedKernel;

namespace Application.Organizations.Create;

internal sealed class CreateOrganizationCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateOrganizationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Logo = command.Logo
        };

        organization.Raise(new OrganizationCreatedDomainEvent(organization.Id));

        context.Organizations.Add(organization);
        await context.SaveChangesAsync(cancellationToken);

        return organization.Id;
    }
}
