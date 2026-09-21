using Application.Abstractions.Messaging;

namespace Application.Users.Update;

public sealed record UpdateUserCommand(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string>? Roles = null) : ICommand;
