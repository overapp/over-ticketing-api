using Application.Abstractions.Messaging;

namespace Application.Users.Create;

public sealed record CreateUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password,
    IReadOnlyCollection<string>? Roles = null) : ICommand<Guid>;
