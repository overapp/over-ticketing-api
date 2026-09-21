using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.GetById;

internal sealed class GetUserByIdQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetUserByIdQuery, UserResponse>
{
    public async Task<Result<UserResponse>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == query.UserId)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                Email = u.Email ?? string.Empty,
                u.EmailConfirmed
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserResponse>(UserErrors.NotFound(query.UserId));
        }

        List<string> roles = await (from ur in context.UserRoles
                                    join r in context.Roles on ur.RoleId equals r.Id
                                    where ur.UserId == query.UserId
                                    select r.Name!)
                                   .ToListAsync(cancellationToken);

        return new UserResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles
        };
    }
}
