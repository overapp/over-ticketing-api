using Application.Abstractions.Messaging;
using Application.Organizations;

namespace Application.Organizations.GetById;

public sealed record GetOrganizationByIdQuery(Guid OrganizationId) : IQuery<OrganizationResponse>;
