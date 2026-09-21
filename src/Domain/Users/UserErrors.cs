using SharedKernel;

namespace Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) => Error.NotFound(
        "Users.NotFound",
        $"The user with the Id = '{userId}' was not found");

    public static Error Unauthorized() => Error.Failure(
        "Users.Unauthorized",
        "You are not authorized to perform this action.");

    public static readonly Error NotFoundByEmail = Error.NotFound(
        "Users.NotFoundByEmail",
        "The user with the specified email was not found");

    public static readonly Error EmailNotUnique = Error.Conflict(
        "Users.EmailNotUnique",
        "The provided email is not unique");

    public static readonly Error InvalidRefreshToken = Error.Problem(
        "Users.InvalidRefreshToken",
        "The provided refresh token is invalid or has expired");

    public static readonly Error DuplicateEmail = Error.Conflict(
        "Users.DuplicateEmail",
        "The provided email is already in use");

    public static readonly Error DuplicateUserName = Error.Conflict(
        "Users.DuplicateUserName",
        "The provided username is already in use");

    public static readonly Error PasswordTooShort = Error.Validation(
        "Users.PasswordTooShort",
        "The password is too short");

    public static readonly Error PasswordRequiresDigit = Error.Validation(
        "Users.PasswordRequiresDigit",
        "The password must contain at least one digit");

    public static readonly Error PasswordRequiresLower = Error.Validation(
        "Users.PasswordRequiresLower",
        "The password must contain at least one lowercase letter");

    public static readonly Error PasswordRequiresUpper = Error.Validation(
        "Users.PasswordRequiresUpper",
        "The password must contain at least one uppercase letter");

    public static readonly Error PasswordRequiresNonAlphanumeric = Error.Validation(
        "Users.PasswordRequiresNonAlphanumeric",
        "The password must contain at least one non-alphanumeric character");

    public static readonly Error PasswordRequiresUniqueChars = Error.Validation(
        "Users.PasswordRequiresUniqueChars",
        "The password must contain enough unique characters");

    public static readonly Error InvalidEmail = Error.Validation(
        "Users.InvalidEmail",
        "The provided email is invalid");

    public static readonly Error InvalidUserName = Error.Validation(
        "Users.InvalidUserName",
        "The provided username is invalid");

    public static readonly Error CannotDeleteSelf = Error.Problem(
        "Users.CannotDeleteSelf",
        "You cannot delete your own user account");

    public static readonly Error CannotDeleteLastAdmin = Error.Problem(
        "Users.CannotDeleteLastAdmin",
        "The last administrator account cannot be deleted");

    public static readonly Error CannotDemoteSelf = Error.Problem(
        "Users.CannotDemoteSelf",
        "You cannot revoke your own administrator role");

    public static readonly Error CannotDemoteLastAdmin = Error.Problem(
        "Users.CannotDemoteLastAdmin",
        "The last administrator account cannot be demoted");

    public static Error InvalidRole(string role) => Error.Validation(
        "Users.InvalidRole",
        $"The role '{role}' is invalid");

    public static Error IdentityError(string code, string description) => Error.Failure(
        $"Users.{code}",
        description);
}
