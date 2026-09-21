using Application.Abstractions.Messaging;
using Application.Users.GetById;

namespace Application.Users.GetMe;

public sealed record GetMyUserQuery : IQuery<UserResponse>;
