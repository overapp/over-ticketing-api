using FluentValidation;

namespace Application.Projects.Delete;

public sealed class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
{
    public DeleteProjectCommandValidator()
    {
        RuleFor(command => command.ProjectId).NotEmpty();
    }
}
