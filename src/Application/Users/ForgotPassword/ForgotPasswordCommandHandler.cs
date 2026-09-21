using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace Application.Users.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(
    UserManager<User> userManager,
    IApplicationDbContext context) : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByEmailAsync(command.Email);

        if (user is null)
        {
            // Do not reveal whether the user exists or not (User Enumeration protection)
            return Result.Success();
        }

        string token = await userManager.GeneratePasswordResetTokenAsync(user);

        user.Raise(new UserPasswordResetRequestedDomainEvent(user.Id, user.Email!, token));

        // Save domain event into outbox and dispatch
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
