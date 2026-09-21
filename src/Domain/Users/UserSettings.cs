using SharedKernel;

namespace Domain.Users;

public sealed class UserSettings : Entity
{
    public Guid UserId { get; set; }

    public bool NotifyOnTicketCreated { get; set; } = true;

    public bool NotifyOnTicketReply { get; set; } = true;

    public User? User { get; set; }
}
