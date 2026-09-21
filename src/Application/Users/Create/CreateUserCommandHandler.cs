using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace Application.Users.Create;

internal sealed class CreateUserCommandHandler(
    UserManager<User> userManager,
    IPasswordGenerator passwordGenerator)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        string temporaryPassword = passwordGenerator.Generate();

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = command.Email,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            EmailConfirmed = true,
            MustChangePassword = true
        };

        user.Raise(new UserCreatedDomainEvent(user.Id, temporaryPassword));

        IdentityResult createResult = await userManager.CreateAsync(user, temporaryPassword);

        if (!createResult.Succeeded)
        {
            return Result.Failure<Guid>(IdentityErrorMapper.MapFirst(createResult.Errors));
        }

        var rolesToAssign = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { RoleNames.User };

        if (command.Roles is not null)
        {
            foreach (string role in command.Roles)
            {
                string? matchedRole = RoleNames.All.FirstOrDefault(r =>
                    string.Equals(r, role, StringComparison.OrdinalIgnoreCase));

                if (matchedRole is not null)
                {
                    rolesToAssign.Add(matchedRole);
                }
            }
        }

        IdentityResult addToRolesResult = await userManager.AddToRolesAsync(user, rolesToAssign);

        if (!addToRolesResult.Succeeded)
        {
            return Result.Failure<Guid>(IdentityErrorMapper.MapFirst(addToRolesResult.Errors));
        }

        return user.Id;
    }
}
