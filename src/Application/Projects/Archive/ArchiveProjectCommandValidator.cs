using FluentValidation;

namespace Application.Projects.Archive;

public sealed class ArchiveProjectCommandValidator : AbstractValidator<ArchiveProjectCommand>
{
    public ArchiveProjectCommandValidator()
    {
        RuleFor(command => command.ProjectId).NotEmpty();
    }
}
