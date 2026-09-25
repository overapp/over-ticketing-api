using Application.Abstractions.Messaging;
using Application.Users;

namespace Application.Users.GetMe;

public sealed record GetMyUserQuery : IQuery<UserResponse>;
