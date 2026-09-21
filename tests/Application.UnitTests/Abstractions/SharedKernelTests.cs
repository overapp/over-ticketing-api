using SharedKernel;

namespace Application.UnitTests.Abstractions;

public sealed class SharedKernelTests
{
    [Fact]
    public void ValidationError_FromResults_Should_ExtractOnlyFailures()
    {
        // Arrange
        var err1 = Error.Problem("Err1", "First error");
        var err2 = Error.Conflict("Err2", "Second error");
        Result[] results =
        [
            Result.Success(),
            Result.Failure(err1),
            Result.Success(),
            Result.Failure(err2)
        ];

        // Act
        var validationError = ValidationError.FromResults(results);

        // Assert
        validationError.Code.ShouldBe("Validation.General");
        validationError.Type.ShouldBe(ErrorType.Validation);
        validationError.Errors.Length.ShouldBe(2);
        validationError.Errors.ShouldContain(err1);
        validationError.Errors.ShouldContain(err2);
    }

    [Fact]
    public void Result_Should_ThrowArgumentException_WhenSuccessWithError()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new Result(true, Error.Problem("Err", "Error")));
    }

    [Fact]
    public void Result_Should_ThrowArgumentException_WhenFailureWithNoneError()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new Result(false, Error.None));
    }

    [Fact]
    public void ResultTValue_Value_Should_Throw_WhenAccessedOnFailure()
    {
        // Arrange
        var failure = Result.Failure<string>(Error.Problem("Code", "Desc"));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _ = failure.Value);
    }

    [Fact]
    public void ResultTValue_ImplicitOperator_Should_ReturnSuccess_WhenValueNotNull()
    {
        // Act
        Result<string> result = "hello";

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("hello");
    }

    [Fact]
    public void ResultTValue_ImplicitOperator_Should_ReturnFailure_WhenValueNull()
    {
        // Act
        string? val = null;
        Result<string> result = val;

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NullValue);
    }

    [Fact]
    public void ResultTValue_ValidationFailure_Should_CreateFailure()
    {
        // Act
        var err = Error.Validation("Val.Code", "Val description");
        var result = Result<string>.ValidationFailure(err);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(err);
    }

    [Fact]
    public void Error_FactoryMethods_Should_SetCorrectTypes()
    {
        Error.Conflict("Code", "Desc").Type.ShouldBe(ErrorType.Conflict);
        Error.Validation("Code", "Desc").Type.ShouldBe(ErrorType.Validation);
        Error.NotFound("Code", "Desc").Type.ShouldBe(ErrorType.NotFound);
        Error.Problem("Code", "Desc").Type.ShouldBe(ErrorType.Problem);
        Error.Failure("Code", "Desc").Type.ShouldBe(ErrorType.Failure);
    }
}
