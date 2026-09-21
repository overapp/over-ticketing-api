namespace Application.Users.ForgotPassword;

public sealed record UserPasswordResetEmailModel(
    string RecipientName,
    string Email,
    Uri ResetUrl);
