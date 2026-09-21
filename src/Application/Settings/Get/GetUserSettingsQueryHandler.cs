using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Settings.Get;

internal sealed class GetUserSettingsQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetUserSettingsQuery, UserSettingsResponse>
{
    public async Task<Result<UserSettingsResponse>> Handle(
        GetUserSettingsQuery query,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;

        bool userExists = await context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, cancellationToken);

        if (!userExists)
        {
            return Result.Failure<UserSettingsResponse>(UserErrors.NotFound(userId));
        }

        UserSettings? settings = await context.UserSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        bool notifyOnTicketCreated = settings?.NotifyOnTicketCreated ?? true;
        bool notifyOnTicketReply = settings?.NotifyOnTicketReply ?? true;

        var response = new UserSettingsResponse(
            new EmailNotificationSettingsResponse(notifyOnTicketCreated, notifyOnTicketReply));

        return response;
    }
}
