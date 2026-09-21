namespace Application.Users.ChangePassword;

public sealed record UserPasswordChangedEmailModel(
    string RecipientName,
    string Email,
    string ChangedAtUtc);
