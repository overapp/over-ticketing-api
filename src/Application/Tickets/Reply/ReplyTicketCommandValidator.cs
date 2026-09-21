using Application.Common.Validation;
using FluentValidation;

namespace Application.Tickets.Reply;

public sealed class ReplyTicketCommandValidator : AbstractValidator<ReplyTicketCommand>
{
    public ReplyTicketCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();

        RuleFor(command => command.Content)
            .NotEmpty()
            .MaximumLength(10000)
            .ValidMarkdown();

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
