using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Users;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.GetMe;

internal sealed class GetMyUserQueryHandler(IApplicationDbContext context, IUserContext userContext)
    : IQueryHandler<GetMyUserQuery, UserResponse>
{
    public async Task<Result<UserResponse>> Handle(GetMyUserQuery query, CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                Email = u.Email ?? string.Empty,
                u.EmailConfirmed,
                u.MustChangePassword,
                u.ProfilePictureUrl
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserResponse>(UserErrors.NotFound(userId));
        }

        List<string> roles = await (from ur in context.UserRoles
                                    join r in context.Roles on ur.RoleId equals r.Id
                                    where ur.UserId == userId
                                    select r.Name!)
                                   .ToListAsync(cancellationToken);

        return new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            MustChangePassword = user.MustChangePassword,
            AvatarUrl = UserAvatarUrlHelper.GetAvatarUrl(user.Id, user.ProfilePictureUrl),
            Roles = roles
        };
    }
}
