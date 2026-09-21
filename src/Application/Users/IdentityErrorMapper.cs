using Domain.Users;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace Application.Users;

internal static class IdentityErrorMapper
{
    public static Error Map(IdentityError identityError) => identityError.Code switch
    {
        "DuplicateEmail" => UserErrors.DuplicateEmail,
        "DuplicateUserName" => UserErrors.DuplicateUserName,
        "PasswordTooShort" => UserErrors.PasswordTooShort,
        "PasswordRequiresDigit" => UserErrors.PasswordRequiresDigit,
        "PasswordRequiresLower" => UserErrors.PasswordRequiresLower,
        "PasswordRequiresUpper" => UserErrors.PasswordRequiresUpper,
        "PasswordRequiresNonAlphanumeric" => UserErrors.PasswordRequiresNonAlphanumeric,
        "PasswordRequiresUniqueChars" => UserErrors.PasswordRequiresUniqueChars,
        "InvalidEmail" => UserErrors.InvalidEmail,
        "InvalidUserName" => UserErrors.InvalidUserName,
        "InvalidToken" => UserErrors.InvalidPasswordResetToken,
        _ => UserErrors.IdentityError(identityError.Code, identityError.Description)
    };

    public static Error MapFirst(IEnumerable<IdentityError> identityErrors) =>
        Map(identityErrors.First());
}
