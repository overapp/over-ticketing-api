using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.ChangePassword;

internal sealed class ChangePasswordCommandHandler(
    UserManager<User> userManager,
    IUserContext userContext,
    IApplicationDbContext context) : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        User? user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(userId));
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return Result.Failure(UserErrors.UserLockedOut);
        }

        IdentityResult changeResult = await userManager.ChangePasswordAsync(
            user,
            command.CurrentPassword,
            command.NewPassword);

        if (!changeResult.Succeeded)
        {
            // Record failed attempt for lockout protection
            await userManager.AccessFailedAsync(user);

            IdentityError? firstError = changeResult.Errors.FirstOrDefault();

            if (firstError?.Code == "PasswordMismatch")
            {
                return Result.Failure(UserErrors.InvalidCurrentPassword);
            }

            return Result.Failure(IdentityErrorMapper.MapFirst(changeResult.Errors));
        }

        // Reset failed access count on success
        await userManager.ResetAccessFailedCountAsync(user);

        user.MustChangePassword = false;

        user.Raise(new UserPasswordChangedDomainEvent(user.Id));

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
