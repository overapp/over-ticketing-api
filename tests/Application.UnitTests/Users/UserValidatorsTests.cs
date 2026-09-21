using Application.Users.Create;
using Application.Users.Delete;
using Application.Users.Update;
using Domain.Users;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Users;

public sealed class UserValidatorsTests
{
    private readonly CreateUserCommandValidator _createValidator = new();
    private readonly UpdateUserCommandValidator _updateValidator = new();
    private readonly DeleteUserCommandValidator _deleteValidator = new();

    [Fact]
    public void CreateValidator_Should_HaveError_WhenEmailIsInvalid()
    {
        var command = new CreateUserCommand("not-an-email", "Mario", "Rossi", "Password123!");
        TestValidationResult<CreateUserCommand> result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void CreateValidator_Should_HaveError_WhenPasswordIsTooWeak()
    {
        var command = new CreateUserCommand("test@example.com", "Mario", "Rossi", "weak");
        TestValidationResult<CreateUserCommand> result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void CreateValidator_Should_HaveError_WhenRoleIsInvalid()
    {
        var command = new CreateUserCommand("test@example.com", "Mario", "Rossi", "Password123!", ["NonExistentRole"]);
        TestValidationResult<CreateUserCommand> result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("Roles[0]");
    }

    [Fact]
    public void UpdateValidator_Should_HaveError_WhenUserIdIsEmpty()
    {
        var command = new UpdateUserCommand(Guid.Empty, "test@example.com", "Mario", "Rossi");
        TestValidationResult<UpdateUserCommand> result = _updateValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public void DeleteValidator_Should_HaveError_WhenUserIdIsEmpty()
    {
        var command = new DeleteUserCommand(Guid.Empty);
        TestValidationResult<DeleteUserCommand> result = _deleteValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }
}
