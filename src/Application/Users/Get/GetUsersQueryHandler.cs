using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Users.GetById;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Get;

internal sealed class GetUsersQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetUsersQuery, PagedResponse<UserResponse>>
{
    public async Task<Result<PagedResponse<UserResponse>>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        int page = query.Page < 1 ? 1 : query.Page;
        int pageSize = query.PageSize < 1 ? 10 : query.PageSize;
        if (pageSize > 100)
        {
            pageSize = 100;
        }

        IQueryable<Domain.Users.User> usersQuery = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = $"%{query.SearchTerm.Trim()}%";
            usersQuery = usersQuery.Where(u =>
                EF.Functions.Like(u.FirstName, term) ||
                EF.Functions.Like(u.LastName, term) ||
                u.Email != null && EF.Functions.Like(u.Email, term));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            string roleName = query.Role.Trim();
            string normalizedRole = roleName.ToUpperInvariant();

            Guid? roleId = await context.Roles
                .AsNoTracking()
                .Where(r => r.NormalizedName == normalizedRole || r.Name == roleName)
                .Select(r => (Guid?)r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (roleId is null)
            {
                return new PagedResponse<UserResponse>
                {
                    Items = [],
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }

            usersQuery = usersQuery.Where(u => context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId.Value));
        }

        int totalCount = await usersQuery.CountAsync(cancellationToken);

        var users = await usersQuery
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                Email = u.Email ?? string.Empty,
                u.EmailConfirmed,
                u.MustChangePassword
            })
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();

        var userRoles = await (from ur in context.UserRoles
                               join r in context.Roles on ur.RoleId equals r.Id
                               where userIds.Contains(ur.UserId)
                               select new { ur.UserId, RoleName = r.Name })
                              .ToListAsync(cancellationToken);

        var lookup = userRoles
            .GroupBy(ur => ur.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyCollection<string>)g.Select(x => x.RoleName!).ToList());

        var items = users.Select(u => new UserResponse
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            EmailConfirmed = u.EmailConfirmed,
            MustChangePassword = u.MustChangePassword,
            Roles = lookup.GetValueOrDefault(u.Id, [])
        }).ToList();

        return new PagedResponse<UserResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
