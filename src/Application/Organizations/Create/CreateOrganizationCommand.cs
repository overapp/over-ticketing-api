using Application.Abstractions.Messaging;

namespace Application.Organizations.Create;

public sealed record CreateOrganizationCommand(string Name, string Logo) : ICommand<Guid>;
