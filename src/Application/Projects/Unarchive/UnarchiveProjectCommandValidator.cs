using FluentValidation;

namespace Application.Projects.Unarchive;

public sealed class UnarchiveProjectCommandValidator : AbstractValidator<UnarchiveProjectCommand>
{
    public UnarchiveProjectCommandValidator()
    {
        RuleFor(command => command.ProjectId).NotEmpty();
    }
}
