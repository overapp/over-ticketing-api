namespace Application.Users.AdminResetPassword;

public sealed record UserTemporaryPasswordEmailModel(
    string RecipientName,
    string Email,
    Uri LoginUrl,
    string TemporaryPassword);
