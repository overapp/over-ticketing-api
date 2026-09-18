using Application.Projects.Archive;
using Application.Projects.Create;
using Application.Projects.Delete;
using Application.Projects.Unarchive;
using Application.Projects.Update;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Projects;

public sealed class ProjectValidatorsTests
{
    private readonly ArchiveProjectCommandValidator _archiveValidator = new();
    private readonly CreateProjectCommandValidator _createValidator = new();
    private readonly DeleteProjectCommandValidator _deleteValidator = new();
    private readonly UnarchiveProjectCommandValidator _unarchiveValidator = new();
    private readonly UpdateProjectCommandValidator _updateValidator = new();

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CreateValidator_Should_HaveError_WhenNameIsInvalid(string? name)
    {
        var command = new CreateProjectCommand(Guid.NewGuid(), name!, "Description");

        TestValidationResult<CreateProjectCommand> result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.Name);
    }

    [Fact]
    public void CreateValidator_Should_HaveError_WhenOrganizationIdIsEmpty()
    {
        var command = new CreateProjectCommand(Guid.Empty, "API", "Description");

        TestValidationResult<CreateProjectCommand> result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.OrganizationId);
    }

    [Fact]
    public void CreateValidator_Should_HaveError_WhenNameExceedsMaximumLength()
    {
        var command = new CreateProjectCommand(Guid.NewGuid(), new string('a', 201), "Description");

        TestValidationResult<CreateProjectCommand> result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CreateValidator_Should_HaveError_WhenDescriptionIsInvalid(string? description)
    {
        var command = new CreateProjectCommand(Guid.NewGuid(), "API", description!);

        TestValidationResult<CreateProjectCommand> result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.Description);
    }

    [Fact]
    public void CreateValidator_Should_HaveError_WhenDescriptionExceedsMaximumLength()
    {
        var command = new CreateProjectCommand(Guid.NewGuid(), "API", new string('a', 2049));

        TestValidationResult<CreateProjectCommand> result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.Description);
    }

    [Fact]
    public void CreateValidator_Should_NotHaveValidationErrors_WhenCommandIsValid()
    {
        TestValidationResult<CreateProjectCommand> result =
            _createValidator.TestValidate(new CreateProjectCommand(Guid.NewGuid(), "API", "Description"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateValidator_Should_HaveError_WhenProjectIdIsEmpty()
    {
        TestValidationResult<UpdateProjectCommand> result =
            _updateValidator.TestValidate(new UpdateProjectCommand(Guid.Empty, "API", "Description"));

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.ProjectId);
    }

    [Fact]
    public void UpdateValidator_Should_HaveError_WhenNameExceedsMaximumLength()
    {
        TestValidationResult<UpdateProjectCommand> result =
            _updateValidator.TestValidate(new UpdateProjectCommand(Guid.NewGuid(), new string('a', 201), "Description"));

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.Name);
    }

    [Fact]
    public void UpdateValidator_Should_HaveError_WhenDescriptionExceedsMaximumLength()
    {
        TestValidationResult<UpdateProjectCommand> result =
            _updateValidator.TestValidate(new UpdateProjectCommand(Guid.NewGuid(), "API", new string('a', 2049)));

        result.ShouldHaveValidationErrorFor(currentCommand => currentCommand.Description);
    }

    [Fact]
    public void UpdateValidator_Should_NotHaveValidationErrors_WhenCommandIsValid()
    {
        TestValidationResult<UpdateProjectCommand> result =
            _updateValidator.TestValidate(new UpdateProjectCommand(Guid.NewGuid(), "API", "Description"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void UpdateValidator_Should_HaveError_WhenNameIsInvalid(string? name)
    {
        TestValidationResult<UpdateProjectCommand> result =
            _updateValidator.TestValidate(new UpdateProjectCommand(Guid.NewGuid(), name!, "Description"));

        result.ShouldHaveValidationErrorFor(command => command.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void UpdateValidator_Should_HaveError_WhenDescriptionIsInvalid(string? description)
    {
        TestValidationResult<UpdateProjectCommand> result =
            _updateValidator.TestValidate(new UpdateProjectCommand(Guid.NewGuid(), "API", description!));

        result.ShouldHaveValidationErrorFor(command => command.Description);
    }

    [Fact]
    public void ArchiveValidator_Should_HaveError_WhenProjectIdIsEmpty()
    {
        TestValidationResult<ArchiveProjectCommand> result =
            _archiveValidator.TestValidate(new ArchiveProjectCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(command => command.ProjectId);
    }

    [Fact]
    public void ArchiveValidator_Should_NotHaveValidationErrors_WhenProjectIdIsValid()
    {
        TestValidationResult<ArchiveProjectCommand> result =
            _archiveValidator.TestValidate(new ArchiveProjectCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DeleteValidator_Should_HaveError_WhenProjectIdIsEmpty()
    {
        TestValidationResult<DeleteProjectCommand> result =
            _deleteValidator.TestValidate(new DeleteProjectCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(command => command.ProjectId);
    }

    [Fact]
    public void DeleteValidator_Should_NotHaveValidationErrors_WhenProjectIdIsValid()
    {
        TestValidationResult<DeleteProjectCommand> result =
            _deleteValidator.TestValidate(new DeleteProjectCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UnarchiveValidator_Should_HaveError_WhenProjectIdIsEmpty()
    {
        TestValidationResult<UnarchiveProjectCommand> result =
            _unarchiveValidator.TestValidate(new UnarchiveProjectCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(command => command.ProjectId);
    }

    [Fact]
    public void UnarchiveValidator_Should_NotHaveValidationErrors_WhenProjectIdIsValid()
    {
        TestValidationResult<UnarchiveProjectCommand> result =
            _unarchiveValidator.TestValidate(new UnarchiveProjectCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
