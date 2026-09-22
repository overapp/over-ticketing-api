using Application.Abstractions.Data;
using Application.Abstractions.Storage;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Infrastructure.Users;

internal sealed class UserDeletedEventHandler(
    IApplicationDbContext context,
    IFileStorageService fileStorageService)
    : IDomainEventHandler<UserDeletedDomainEvent>
{
    public async Task Handle(UserDeletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == domainEvent.UserId, cancellationToken);

        if (user is null || string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            return;
        }

        await fileStorageService.DeleteAsync(user.ProfilePictureUrl, cancellationToken);
    }
}
