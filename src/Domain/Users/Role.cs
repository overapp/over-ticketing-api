using Microsoft.AspNetCore.Identity;

namespace Domain.Users;

public sealed class Role : IdentityRole<Guid>
{
    public Role()
    {
    }

    public Role(string name)
        : base(name)
    {
    }
}
