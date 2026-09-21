using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Settings.Update;

internal sealed class UpdateUserSettingsCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : ICommandHandler<UpdateUserSettingsCommand>
{
    public async Task<Result> Handle(
        UpdateUserSettingsCommand command,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        bool userExists = await context.Users
            .AnyAsync(u => u.Id == userId, cancellationToken);

        if (!userExists)
        {
            return Result.Failure(UserErrors.NotFound(userId));
        }

        UserSettings? settings = await context.UserSettings
            .SingleOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (settings is null)
        {
            settings = new UserSettings
            {
                UserId = userId,
                NotifyOnTicketCreated = command.EmailNotifications.NotifyOnTicketCreated,
                NotifyOnTicketReply = command.EmailNotifications.NotifyOnTicketReply
            };

            context.UserSettings.Add(settings);
        }
        else
        {
            settings.NotifyOnTicketCreated = command.EmailNotifications.NotifyOnTicketCreated;
            settings.NotifyOnTicketReply = command.EmailNotifications.NotifyOnTicketReply;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
