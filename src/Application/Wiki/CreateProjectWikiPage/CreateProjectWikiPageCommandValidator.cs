using FluentValidation;

namespace Application.Wiki.CreateProjectWikiPage;

internal sealed class CreateProjectWikiPageCommandValidator : AbstractValidator<CreateProjectWikiPageCommand>
{
    public CreateProjectWikiPageCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();

        RuleFor(c => c.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(c => c.Content).NotNull();

        RuleFor(c => c.Slug)
            .MaximumLength(200)
            .When(c => !string.IsNullOrWhiteSpace(c.Slug));
    }
}
