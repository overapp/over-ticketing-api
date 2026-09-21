using Application.Abstractions.Messaging;
using Application.Common;
using Application.Users.GetById;

namespace Application.Users.Get;

public sealed record GetUsersQuery(
    int Page = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? Role = null) : IQuery<PagedResponse<UserResponse>>;
