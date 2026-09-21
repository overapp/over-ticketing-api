using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.AdminResetPassword;

internal sealed class AdminResetPasswordCommandHandler(
    UserManager<User> userManager,
    IPasswordGenerator passwordGenerator,
    IApplicationDbContext context) : ICommandHandler<AdminResetPasswordCommand>
{
    public async Task<Result> Handle(AdminResetPasswordCommand command, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(command.UserId.ToString());

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        string temporaryPassword = passwordGenerator.Generate();

        string resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

        IdentityResult resetResult = await userManager.ResetPasswordAsync(user, resetToken, temporaryPassword);

        if (!resetResult.Succeeded)
        {
            return Result.Failure(IdentityErrorMapper.MapFirst(resetResult.Errors));
        }

        user.MustChangePassword = true;

        user.Raise(new UserTemporaryPasswordAssignedDomainEvent(user.Id, temporaryPassword));

        IdentityResult updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return Result.Failure(IdentityErrorMapper.MapFirst(updateResult.Errors));
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
