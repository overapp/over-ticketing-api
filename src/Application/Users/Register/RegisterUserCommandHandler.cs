using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandHandler(UserManager<User> userManager)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = command.Email,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName
        };

        user.Raise(new UserRegisteredDomainEvent(user.Id));

        IdentityResult result = await userManager.CreateAsync(user, command.Password);

        if (!result.Succeeded)
        {
            return Result.Failure<Guid>(IdentityErrorMapper.MapFirst(result.Errors));
        }

        return user.Id;
    }
}
