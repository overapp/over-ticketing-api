using Application.Abstractions.Messaging;

namespace Application.Users.AdminResetPassword;

public sealed record AdminResetPasswordCommand(Guid UserId) : ICommand;
