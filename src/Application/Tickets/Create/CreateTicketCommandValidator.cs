using FluentValidation;

namespace Application.Tickets.Create;

public sealed class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(command => command.ProjectId).NotEmpty();

        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Message)
            .NotEmpty()
            .MaximumLength(10000);

        RuleFor(command => command.Priority)
            .IsInEnum();

        RuleFor(command => command.Attachments)
            .Must(attachments => attachments is null || attachments.Count <= 5)
            .WithMessage("A maximum of 5 attachments are allowed per message.");

        RuleForEach(command => command.Attachments)
            .ChildRules(attachment =>
            {
                attachment.RuleFor(a => a.Size)
                    .LessThanOrEqualTo(10 * 1024 * 1024)
                    .WithMessage("File size must not exceed 10 MB.");
            });
    }
}
