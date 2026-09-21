using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    UserManager<User> userManager,
    IApplicationDbContext context) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByEmailAsync(command.Email);

        if (user is null)
        {
            return Result.Failure(UserErrors.InvalidPasswordResetToken);
        }

        IdentityResult resetResult = await userManager.ResetPasswordAsync(user, command.Token, command.NewPassword);

        if (!resetResult.Succeeded)
        {
            return Result.Failure(IdentityErrorMapper.MapFirst(resetResult.Errors));
        }

        // Revoke active sessions by deleting user's refresh tokens
        List<RefreshToken> activeRefreshTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == user.Id)
            .ToListAsync(cancellationToken);

        if (activeRefreshTokens.Count > 0)
        {
            context.RefreshTokens.RemoveRange(activeRefreshTokens);
            await context.SaveChangesAsync(cancellationToken);
        }

        // Invalidate current security stamp to invalidate any outstanding auth tickets
        await userManager.UpdateSecurityStampAsync(user);

        return Result.Success();
    }
}
