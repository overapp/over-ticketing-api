using FluentValidation;

namespace Application.Users.UpdateAvatar;

public sealed class UpdateUserAvatarCommandValidator : AbstractValidator<UpdateUserAvatarCommand>
{
    private static readonly string[] AllowedMimeTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    ];

    public UpdateUserAvatarCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required.");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage("Content type is required.")
            .Must(ct => AllowedMimeTypes.Contains(ct))
            .WithMessage("Only image files (JPEG, PNG, GIF, WebP) are allowed.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .WithMessage("File size must be greater than 0.")
            .LessThanOrEqualTo(5 * 1024 * 1024)
            .WithMessage("File size cannot exceed 5 MB.");

        RuleFor(x => x.ContentStream)
            .NotNull()
            .WithMessage("File content is required.");
    }
}
