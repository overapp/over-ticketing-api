using FluentValidation;

namespace Application.Wiki.UpdateWikiPageStatus;

internal sealed class UpdateWikiPageStatusCommandValidator : AbstractValidator<UpdateWikiPageStatusCommand>
{
    public UpdateWikiPageStatusCommandValidator()
    {
        RuleFor(c => c.WikiPageId).NotEmpty();
        RuleFor(c => c.Status).IsInEnum();
    }
}
