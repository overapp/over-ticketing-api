namespace Application.Users.GetAvatar;

public sealed record AvatarDownloadResponse(Stream Stream, string ContentType);
