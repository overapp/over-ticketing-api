using Application.Common;

namespace Application.Users;

public sealed record UserResponse
{
    public Guid Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Initials => UserInitialsHelper.GetInitials(FirstName, LastName);

    public string? AvatarUrl { get; init; }

    public IReadOnlyCollection<string> Roles { get; init; } = [];

    public bool EmailConfirmed { get; init; }

    public bool MustChangePassword { get; init; }
}
