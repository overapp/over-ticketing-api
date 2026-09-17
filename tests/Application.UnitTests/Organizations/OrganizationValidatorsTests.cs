using Application.Organizations.Archive;
using Application.Organizations.Create;
using Application.Organizations.Delete;
using Application.Organizations.Unarchive;
using Application.Organizations.Update;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Organizations;

public sealed class OrganizationValidatorsTests
{
    private readonly ArchiveOrganizationCommandValidator _archiveValidator = new();
    private readonly CreateOrganizationCommandValidator _createValidator = new();
    private readonly DeleteOrganizationCommandValidator _deleteValidator = new();
    private readonly UnarchiveOrganizationCommandValidator _unarchiveValidator = new();
    private readonly UpdateOrganizationCommandValidator _updateValidator = new();

    [Fact]
    public void CreateValidator_Should_HaveError_WhenNameIsEmpty()
    {
        var command = new CreateOrganizationCommand(string.Empty, "https://example.com/logo.png");

        TestValidationResult<CreateOrganizationCommand> result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.Name);
    }

    [Fact]
    public void CreateValidator_Should_HaveError_WhenNameExceedsMaximumLength()
    {
        var command = new CreateOrganizationCommand(new string('a', 201), "https://example.com/logo.png");

        TestValidationResult<CreateOrganizationCommand> result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.Name);
    }

    [Fact]
    public void CreateValidator_Should_HaveError_WhenLogoIsEmpty()
    {
        var command = new CreateOrganizationCommand("Overapp", string.Empty);

        TestValidationResult<CreateOrganizationCommand> result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.Logo);
    }

    [Fact]
    public void CreateValidator_Should_HaveError_WhenLogoExceedsMaximumLength()
    {
        var command = new CreateOrganizationCommand("Overapp", new string('a', 2049));

        TestValidationResult<CreateOrganizationCommand> result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.Logo);
    }

    [Fact]
    public void CreateValidator_Should_NotHaveValidationErrors_WhenCommandIsValid()
    {
        var command = new CreateOrganizationCommand("Overapp", "https://example.com/logo.png");

        TestValidationResult<CreateOrganizationCommand> result = _createValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateValidator_Should_HaveError_WhenOrganizationIdIsEmpty()
    {
        var command = new UpdateOrganizationCommand(
            Guid.Empty,
            "Overapp",
            "https://example.com/logo.png");

        TestValidationResult<UpdateOrganizationCommand> result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.OrganizationId);
    }

    [Fact]
    public void UpdateValidator_Should_HaveError_WhenNameIsEmpty()
    {
        var command = new UpdateOrganizationCommand(
            Guid.NewGuid(),
            string.Empty,
            "https://example.com/logo.png");

        TestValidationResult<UpdateOrganizationCommand> result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.Name);
    }

    [Fact]
    public void UpdateValidator_Should_HaveError_WhenNameExceedsMaximumLength()
    {
        var command = new UpdateOrganizationCommand(
            Guid.NewGuid(),
            new string('a', 201),
            "https://example.com/logo.png");

        TestValidationResult<UpdateOrganizationCommand> result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.Name);
    }

    [Fact]
    public void UpdateValidator_Should_HaveError_WhenLogoIsEmpty()
    {
        var command = new UpdateOrganizationCommand(Guid.NewGuid(), "Overapp", string.Empty);

        TestValidationResult<UpdateOrganizationCommand> result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.Logo);
    }

    [Fact]
    public void UpdateValidator_Should_HaveError_WhenLogoExceedsMaximumLength()
    {
        var command = new UpdateOrganizationCommand(
            Guid.NewGuid(),
            "Overapp",
            new string('a', 2049));

        TestValidationResult<UpdateOrganizationCommand> result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.Logo);
    }

    [Fact]
    public void UpdateValidator_Should_NotHaveValidationErrors_WhenCommandIsValid()
    {
        var command = new UpdateOrganizationCommand(
            Guid.NewGuid(),
            "Overapp",
            "https://example.com/logo.png");

        TestValidationResult<UpdateOrganizationCommand> result = _updateValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DeleteValidator_Should_HaveError_WhenOrganizationIdIsEmpty()
    {
        var command = new DeleteOrganizationCommand(Guid.Empty);

        TestValidationResult<DeleteOrganizationCommand> result = _deleteValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.OrganizationId);
    }

    [Fact]
    public void DeleteValidator_Should_NotHaveValidationErrors_WhenOrganizationIdIsValid()
    {
        var command = new DeleteOrganizationCommand(Guid.NewGuid());

        TestValidationResult<DeleteOrganizationCommand> result = _deleteValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ArchiveValidator_Should_HaveError_WhenOrganizationIdIsEmpty()
    {
        TestValidationResult<ArchiveOrganizationCommand> result =
            _archiveValidator.TestValidate(new ArchiveOrganizationCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(command => command.OrganizationId);
    }

    [Fact]
    public void ArchiveValidator_Should_NotHaveValidationErrors_WhenOrganizationIdIsValid()
    {
        TestValidationResult<ArchiveOrganizationCommand> result =
            _archiveValidator.TestValidate(new ArchiveOrganizationCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UnarchiveValidator_Should_HaveError_WhenOrganizationIdIsEmpty()
    {
        TestValidationResult<UnarchiveOrganizationCommand> result =
            _unarchiveValidator.TestValidate(new UnarchiveOrganizationCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(command => command.OrganizationId);
    }

    [Fact]
    public void UnarchiveValidator_Should_NotHaveValidationErrors_WhenOrganizationIdIsValid()
    {
        TestValidationResult<UnarchiveOrganizationCommand> result =
            _unarchiveValidator.TestValidate(new UnarchiveOrganizationCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
