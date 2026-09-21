namespace Application.Users.Create;

public sealed record UserWelcomeEmailModel(
    string RecipientName,
    string Email,
    Uri LoginUrl,
    string? TemporaryPassword = null);
