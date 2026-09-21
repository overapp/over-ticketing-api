using FluentValidation;

namespace Application.Wiki.CreateGlobalWikiPage;

internal sealed class CreateGlobalWikiPageCommandValidator : AbstractValidator<CreateGlobalWikiPageCommand>
{
    public CreateGlobalWikiPageCommandValidator()
    {
        RuleFor(c => c.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(c => c.Content).NotNull();

        RuleFor(c => c.Slug)
            .MaximumLength(200)
            .When(c => !string.IsNullOrWhiteSpace(c.Slug));
    }
}
