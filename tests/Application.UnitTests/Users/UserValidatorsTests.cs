using Application.Users.Create;
using Application.Users.Delete;
using Application.Users.Refresh;
using Application.Users.Register;
using Application.Users.Update;
using Domain.Users;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Users;

public sealed class UserValidatorsTests
{
    private readonly CreateUserCommandValidator _createValidator = new();
    private readonly UpdateUserCommandValidator _updateValidator = new();
    private readonly DeleteUserCommandValidator _deleteValidator = new();
    private readonly RegisterUserCommandValidator _registerValidator = new();
    private readonly RefreshTokenCommandValidator _refreshTokenValidator = new();

    [Fact]
    public void CreateValidator_Should_HaveError_WhenEmailIsInvalid()
    {
        var command = new CreateUserCommand("not-an-email", "Mario", "Rossi");
        TestValidationResult<CreateUserCommand> result = _createValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void CreateValidator_Should_HaveError_WhenRoleIsInvalid()
    {
        var command = new CreateUserCommand("test@example.com", "Mario", "Rossi", ["NonExistentRole"]);
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

    [Fact]
    public void RegisterValidator_Should_HaveError_WhenFirstNameIsEmpty()
    {
        var command = new RegisterUserCommand("test@example.com", "", "Rossi", "Password123!");
        TestValidationResult<RegisterUserCommand> result = _registerValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.FirstName);
    }

    [Fact]
    public void RegisterValidator_Should_HaveError_WhenLastNameIsEmpty()
    {
        var command = new RegisterUserCommand("test@example.com", "Mario", "", "Password123!");
        TestValidationResult<RegisterUserCommand> result = _registerValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.LastName);
    }

    [Fact]
    public void RegisterValidator_Should_HaveError_WhenEmailIsInvalid()
    {
        var command = new RegisterUserCommand("not-an-email", "Mario", "Rossi", "Password123!");
        TestValidationResult<RegisterUserCommand> result = _registerValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ab1!")] // < 6 chars
    [InlineData("PassWord!")] // missing digit
    [InlineData("PASSWORD123!")] // missing lowercase
    [InlineData("password123!")] // missing uppercase
    [InlineData("Password123")] // missing special character
    public void RegisterValidator_Should_HaveError_WhenPasswordDoesNotMeetRequirements(string password)
    {
        var command = new RegisterUserCommand("test@example.com", "Mario", "Rossi", password);
        TestValidationResult<RegisterUserCommand> result = _registerValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void RegisterValidator_Should_NotHaveError_WhenCommandIsValid()
    {
        var command = new RegisterUserCommand("test@example.com", "Mario", "Rossi", "Password123!");
        TestValidationResult<RegisterUserCommand> result = _registerValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RefreshTokenValidator_Should_HaveError_WhenRefreshTokenIsEmpty()
    {
        var command = new RefreshTokenCommand("");
        TestValidationResult<RefreshTokenCommand> result = _refreshTokenValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.RefreshToken);
    }

    [Fact]
    public void RefreshTokenValidator_Should_NotHaveError_WhenRefreshTokenIsNotEmpty()
    {
        var command = new RefreshTokenCommand("valid-token");
        TestValidationResult<RefreshTokenCommand> result = _refreshTokenValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
