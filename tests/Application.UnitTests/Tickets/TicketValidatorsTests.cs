using Application.Tickets;
using Application.Tickets.Assign;
using Application.Tickets.Create;
using Application.Tickets.Reply;
using Application.Tickets.UpdatePriority;
using Application.Tickets.UpdateStatus;
using Domain.Tickets;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Tickets;

public sealed class TicketValidatorsTests
{
    private readonly CreateTicketCommandValidator _createValidator = new();
    private readonly ReplyTicketCommandValidator _replyValidator = new();
    private readonly AssignTicketCommandValidator _assignValidator = new();
    private readonly UpdateTicketStatusCommandValidator _statusValidator = new();
    private readonly UpdateTicketPriorityCommandValidator _priorityValidator = new();

    [Fact]
    public void CreateValidator_Should_HaveError_WhenProjectIdIsEmpty()
    {
        var command = new CreateTicketCommand(Guid.Empty, "Bug", "Message");
        TestValidationResult<CreateTicketCommand> result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.ProjectId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CreateValidator_Should_HaveError_WhenTitleIsInvalid(string? title)
    {
        var command = new CreateTicketCommand(Guid.NewGuid(), title!, "Message");
        TestValidationResult<CreateTicketCommand> result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CreateValidator_Should_HaveError_WhenMessageIsInvalid(string? message)
    {
        var command = new CreateTicketCommand(Guid.NewGuid(), "Bug", message!);
        TestValidationResult<CreateTicketCommand> result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Message);
    }

    [Fact]
    public void CreateValidator_Should_HaveError_WhenAttachmentsExceedFive()
    {
        var attachments = Enumerable.Range(0, 6)
            .Select(i => new FileUploadModel($"file{i}.txt", "text/plain", 100, Stream.Null))
            .ToList();

        var command = new CreateTicketCommand(Guid.NewGuid(), "Bug", "Message", Attachments: attachments);
        TestValidationResult<CreateTicketCommand> result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Attachments);
    }

    [Fact]
    public void CreateValidator_Should_NotHaveError_WhenCommandIsValid()
    {
        var command = new CreateTicketCommand(Guid.NewGuid(), "Bug", "Message with **markdown** and [link](https://example.com)");
        TestValidationResult<CreateTicketCommand> result = _createValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateValidator_Should_HaveError_WhenMessageContainsRawHtml()
    {
        var command = new CreateTicketCommand(Guid.NewGuid(), "Bug", "Message with <script>alert(1)</script>");
        TestValidationResult<CreateTicketCommand> result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Message);
    }

    [Fact]
    public void CreateValidator_Should_HaveError_WhenMessageContainsMaliciousLink()
    {
        var command = new CreateTicketCommand(Guid.NewGuid(), "Bug", "Message with [click](javascript:alert(1))");
        TestValidationResult<CreateTicketCommand> result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ReplyValidator_Should_HaveError_WhenContentIsInvalid(string? content)
    {
        var command = new ReplyTicketCommand(Guid.NewGuid(), content!);
        TestValidationResult<ReplyTicketCommand> result = _replyValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Content);
    }

    [Fact]
    public void ReplyValidator_Should_HaveError_WhenContentContainsRawHtml()
    {
        var command = new ReplyTicketCommand(Guid.NewGuid(), "Reply with <img src=x onerror=alert(1)>");
        TestValidationResult<ReplyTicketCommand> result = _replyValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Content);
    }

    [Fact]
    public void ReplyValidator_Should_HaveError_WhenContentContainsMaliciousLink()
    {
        var command = new ReplyTicketCommand(Guid.NewGuid(), "Reply with [click](data:text/html;base64,123)");
        TestValidationResult<ReplyTicketCommand> result = _replyValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Content);
    }

    [Fact]
    public void ReplyValidator_Should_NotHaveError_WhenCommandIsValid()
    {
        var command = new ReplyTicketCommand(Guid.NewGuid(), "Reply with `code` and [link](https://example.com)");
        TestValidationResult<ReplyTicketCommand> result = _replyValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AssignValidator_Should_HaveError_WhenTicketIdIsEmpty()
    {
        var command = new AssignTicketCommand(Guid.Empty, Guid.NewGuid());
        TestValidationResult<AssignTicketCommand> result = _assignValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.TicketId);
    }

    [Fact]
    public void StatusValidator_Should_HaveError_WhenTicketIdIsEmpty()
    {
        var command = new UpdateTicketStatusCommand(Guid.Empty, TicketStatus.InProgress);
        TestValidationResult<UpdateTicketStatusCommand> result = _statusValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.TicketId);
    }

    [Fact]
    public void PriorityValidator_Should_HaveError_WhenTicketIdIsEmpty()
    {
        var command = new UpdateTicketPriorityCommand(Guid.Empty, TicketPriority.Urgent);
        TestValidationResult<UpdateTicketPriorityCommand> result = _priorityValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.TicketId);
    }
}
