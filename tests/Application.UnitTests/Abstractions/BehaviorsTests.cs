using Application.Abstractions.Behaviors;
using Application.Abstractions.Messaging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.UnitTests.Abstractions;

public sealed class BehaviorsTests
{
    public sealed record TestCommandWithResponse(string Data) : ICommand<string>;
    public sealed record TestCommandBase(string Data) : ICommand;
    public sealed record TestQuery(string Data) : IQuery<string>;

    [Fact]
    public async Task LoggingDecorator_CommandHandler_Should_LogAndReturnSuccess_WhenInnerSucceeds()
    {
        // Arrange
        ICommandHandler<TestCommandWithResponse, string> innerHandler = Substitute.For<ICommandHandler<TestCommandWithResponse, string>>();
        ILogger<LoggingDecorator.CommandHandler<TestCommandWithResponse, string>> logger =
            Microsoft.Extensions.Logging.Abstractions.NullLogger<LoggingDecorator.CommandHandler<TestCommandWithResponse, string>>.Instance;
        innerHandler.Handle(Arg.Any<TestCommandWithResponse>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("ok"));

        var decorator = new LoggingDecorator.CommandHandler<TestCommandWithResponse, string>(innerHandler, logger);
        var command = new TestCommandWithResponse("test");

        // Act
        Result<string> result = await decorator.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("ok");
    }

    [Fact]
    public async Task LoggingDecorator_CommandHandler_Should_LogError_WhenInnerFails()
    {
        // Arrange
        ICommandHandler<TestCommandWithResponse, string> innerHandler = Substitute.For<ICommandHandler<TestCommandWithResponse, string>>();
        ILogger<LoggingDecorator.CommandHandler<TestCommandWithResponse, string>> logger =
            Microsoft.Extensions.Logging.Abstractions.NullLogger<LoggingDecorator.CommandHandler<TestCommandWithResponse, string>>.Instance;
        innerHandler.Handle(Arg.Any<TestCommandWithResponse>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(Error.Failure("Test.Error", "Error description")));

        var decorator = new LoggingDecorator.CommandHandler<TestCommandWithResponse, string>(innerHandler, logger);
        var command = new TestCommandWithResponse("test");

        // Act
        Result<string> result = await decorator.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Test.Error");
    }

    [Fact]
    public async Task LoggingDecorator_CommandBaseHandler_Should_LogAndReturnSuccess_WhenInnerSucceeds()
    {
        // Arrange
        ICommandHandler<TestCommandBase> innerHandler = Substitute.For<ICommandHandler<TestCommandBase>>();
        ILogger<LoggingDecorator.CommandBaseHandler<TestCommandBase>> logger =
            Microsoft.Extensions.Logging.Abstractions.NullLogger<LoggingDecorator.CommandBaseHandler<TestCommandBase>>.Instance;
        innerHandler.Handle(Arg.Any<TestCommandBase>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var decorator = new LoggingDecorator.CommandBaseHandler<TestCommandBase>(innerHandler, logger);
        var command = new TestCommandBase("test");

        // Act
        Result result = await decorator.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task LoggingDecorator_CommandBaseHandler_Should_LogError_WhenInnerFails()
    {
        // Arrange
        ICommandHandler<TestCommandBase> innerHandler = Substitute.For<ICommandHandler<TestCommandBase>>();
        ILogger<LoggingDecorator.CommandBaseHandler<TestCommandBase>> logger =
            Microsoft.Extensions.Logging.Abstractions.NullLogger<LoggingDecorator.CommandBaseHandler<TestCommandBase>>.Instance;
        innerHandler.Handle(Arg.Any<TestCommandBase>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.NotFound("Test.NotFound", "Not found")));

        var decorator = new LoggingDecorator.CommandBaseHandler<TestCommandBase>(innerHandler, logger);
        var command = new TestCommandBase("test");

        // Act
        Result result = await decorator.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Test.NotFound");
    }

    [Fact]
    public async Task LoggingDecorator_QueryHandler_Should_LogAndReturnSuccess_WhenInnerSucceeds()
    {
        // Arrange
        IQueryHandler<TestQuery, string> innerHandler = Substitute.For<IQueryHandler<TestQuery, string>>();
        ILogger<LoggingDecorator.QueryHandler<TestQuery, string>> logger =
            Microsoft.Extensions.Logging.Abstractions.NullLogger<LoggingDecorator.QueryHandler<TestQuery, string>>.Instance;
        innerHandler.Handle(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("data"));

        var decorator = new LoggingDecorator.QueryHandler<TestQuery, string>(innerHandler, logger);
        var query = new TestQuery("param");

        // Act
        Result<string> result = await decorator.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("data");
    }

    [Fact]
    public async Task LoggingDecorator_QueryHandler_Should_LogError_WhenInnerFails()
    {
        // Arrange
        IQueryHandler<TestQuery, string> innerHandler = Substitute.For<IQueryHandler<TestQuery, string>>();
        ILogger<LoggingDecorator.QueryHandler<TestQuery, string>> logger =
            Microsoft.Extensions.Logging.Abstractions.NullLogger<LoggingDecorator.QueryHandler<TestQuery, string>>.Instance;
        innerHandler.Handle(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(Error.Problem("Test.QueryError", "Problem")));

        var decorator = new LoggingDecorator.QueryHandler<TestQuery, string>(innerHandler, logger);
        var query = new TestQuery("param");

        // Act
        Result<string> result = await decorator.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Test.QueryError");
    }

    [Fact]
    public async Task ValidationDecorator_CommandHandler_Should_DelegateToInner_WhenNoValidators()
    {
        // Arrange
        ICommandHandler<TestCommandWithResponse, string> innerHandler = Substitute.For<ICommandHandler<TestCommandWithResponse, string>>();
        innerHandler.Handle(Arg.Any<TestCommandWithResponse>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("success"));

        var decorator = new ValidationDecorator.CommandHandler<TestCommandWithResponse, string>(
            innerHandler,
            []);
        var command = new TestCommandWithResponse("data");

        // Act
        Result<string> result = await decorator.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("success");
    }

    [Fact]
    public async Task ValidationDecorator_CommandHandler_Should_ReturnValidationError_WhenValidationFails()
    {
        // Arrange
        ICommandHandler<TestCommandWithResponse, string> innerHandler = Substitute.For<ICommandHandler<TestCommandWithResponse, string>>();
        IValidator<TestCommandWithResponse> validator = Substitute.For<IValidator<TestCommandWithResponse>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestCommandWithResponse>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Data", "Data is required") { ErrorCode = "Data.Required" }]));

        var decorator = new ValidationDecorator.CommandHandler<TestCommandWithResponse, string>(
            innerHandler,
            [validator]);
        var command = new TestCommandWithResponse("");

        // Act
        Result<string> result = await decorator.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>();
        await innerHandler.DidNotReceive().Handle(Arg.Any<TestCommandWithResponse>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidationDecorator_CommandHandler_Should_CallInner_WhenValidationPasses()
    {
        // Arrange
        ICommandHandler<TestCommandWithResponse, string> innerHandler = Substitute.For<ICommandHandler<TestCommandWithResponse, string>>();
        innerHandler.Handle(Arg.Any<TestCommandWithResponse>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("passed"));

        IValidator<TestCommandWithResponse> validator = Substitute.For<IValidator<TestCommandWithResponse>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestCommandWithResponse>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var decorator = new ValidationDecorator.CommandHandler<TestCommandWithResponse, string>(
            innerHandler,
            [validator]);
        var command = new TestCommandWithResponse("valid");

        // Act
        Result<string> result = await decorator.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("passed");
    }

    [Fact]
    public async Task ValidationDecorator_CommandBaseHandler_Should_DelegateToInner_WhenNoValidators()
    {
        // Arrange
        ICommandHandler<TestCommandBase> innerHandler = Substitute.For<ICommandHandler<TestCommandBase>>();
        innerHandler.Handle(Arg.Any<TestCommandBase>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var decorator = new ValidationDecorator.CommandBaseHandler<TestCommandBase>(
            innerHandler,
            []);
        var command = new TestCommandBase("data");

        // Act
        Result result = await decorator.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidationDecorator_CommandBaseHandler_Should_ReturnValidationError_WhenValidationFails()
    {
        // Arrange
        ICommandHandler<TestCommandBase> innerHandler = Substitute.For<ICommandHandler<TestCommandBase>>();
        IValidator<TestCommandBase> validator = Substitute.For<IValidator<TestCommandBase>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestCommandBase>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Data", "Invalid") { ErrorCode = "Data.Invalid" }]));

        var decorator = new ValidationDecorator.CommandBaseHandler<TestCommandBase>(
            innerHandler,
            [validator]);
        var command = new TestCommandBase("");

        // Act
        Result result = await decorator.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>();
        await innerHandler.DidNotReceive().Handle(Arg.Any<TestCommandBase>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidationDecorator_CommandBaseHandler_Should_CallInner_WhenValidationPasses()
    {
        // Arrange
        ICommandHandler<TestCommandBase> innerHandler = Substitute.For<ICommandHandler<TestCommandBase>>();
        innerHandler.Handle(Arg.Any<TestCommandBase>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        IValidator<TestCommandBase> validator = Substitute.For<IValidator<TestCommandBase>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestCommandBase>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var decorator = new ValidationDecorator.CommandBaseHandler<TestCommandBase>(
            innerHandler,
            [validator]);
        var command = new TestCommandBase("valid");

        // Act
        Result result = await decorator.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
}
