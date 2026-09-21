using FluentValidation;

namespace Application.Wiki.UpdateProjectWikiPage;

internal sealed class UpdateProjectWikiPageCommandValidator : AbstractValidator<UpdateProjectWikiPageCommand>
{
    public UpdateProjectWikiPageCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();

        RuleFor(c => c.WikiPageId).NotEmpty();

        RuleFor(c => c.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(c => c.Content).NotNull();

        RuleFor(c => c.Slug)
            .MaximumLength(200)
            .When(c => !string.IsNullOrWhiteSpace(c.Slug));
    }
}
