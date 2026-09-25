using System.Diagnostics.CodeAnalysis;

namespace Application.Common;

public static class UserAvatarUrlHelper
{
    [SuppressMessage("Design", "CA1055:Uri return values should not be strings", Justification = "Returned as a relative API path in a JSON response, not consumed as a Uri.")]
    public static string? GetAvatarUrl(Guid userId, string? profilePictureStorageKey) =>
        string.IsNullOrEmpty(profilePictureStorageKey) ? null : $"/users/{userId}/avatar";
}
