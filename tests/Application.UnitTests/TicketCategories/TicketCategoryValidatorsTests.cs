using Application.TicketCategories.Archive;
using Application.TicketCategories.CreateProject;
using Application.TicketCategories.Unarchive;
using Application.TicketCategories.Update;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.UnitTests.TicketCategories;

public sealed class TicketCategoryValidatorsTests
{
    private readonly CreateProjectTicketCategoryCommandValidator _createProjectValidator = new();
    private readonly UpdateTicketCategoryCommandValidator _updateValidator = new();
    private readonly ArchiveTicketCategoryCommandValidator _archiveValidator = new();
    private readonly UnarchiveTicketCategoryCommandValidator _unarchiveValidator = new();

    [Fact]
    public void CreateProject_Should_HaveValidationError_WhenProjectIdIsEmpty()
    {
        var command = new CreateProjectTicketCategoryCommand(Guid.Empty, "Category");

        TestValidationResult<CreateProjectTicketCategoryCommand> result = _createProjectValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ProjectId);
    }

    [Fact]
    public void CreateProject_Should_HaveValidationError_WhenNameIsEmpty()
    {
        var command = new CreateProjectTicketCategoryCommand(Guid.NewGuid(), "");

        TestValidationResult<CreateProjectTicketCategoryCommand> result = _createProjectValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void CreateProject_Should_HaveValidationError_WhenNameExceeds100Chars()
    {
        var command = new CreateProjectTicketCategoryCommand(Guid.NewGuid(), new string('a', 101));

        TestValidationResult<CreateProjectTicketCategoryCommand> result = _createProjectValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void CreateProject_Should_HaveValidationError_WhenDescriptionExceeds500Chars()
    {
        var command = new CreateProjectTicketCategoryCommand(Guid.NewGuid(), "Valid", new string('d', 501));

        TestValidationResult<CreateProjectTicketCategoryCommand> result = _createProjectValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void CreateProject_Should_HaveValidationError_WhenBackgroundColorInvalid()
    {
        var command = new CreateProjectTicketCategoryCommand(Guid.NewGuid(), "Valid", BackgroundColor: "invalid");

        TestValidationResult<CreateProjectTicketCategoryCommand> result = _createProjectValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.BackgroundColor);
    }

    [Fact]
    public void CreateProject_Should_HaveValidationError_WhenForegroundColorInvalid()
    {
        var command = new CreateProjectTicketCategoryCommand(Guid.NewGuid(), "Valid", ForegroundColor: "not-a-color");

        TestValidationResult<CreateProjectTicketCategoryCommand> result = _createProjectValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ForegroundColor);
    }

    [Fact]
    public void CreateProject_Should_NotHaveValidationError_WhenCommandIsValid()
    {
        var command = new CreateProjectTicketCategoryCommand(
            Guid.NewGuid(),
            "Bug",
            "Software bug",
            "#FF0000",
            "#FFFFFF");

        TestValidationResult<CreateProjectTicketCategoryCommand> result = _createProjectValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Update_Should_HaveValidationError_WhenCategoryIdIsEmpty()
    {
        var command = new UpdateTicketCategoryCommand(Guid.Empty, "Category");

        TestValidationResult<UpdateTicketCategoryCommand> result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CategoryId);
    }

    [Fact]
    public void Update_Should_HaveValidationError_WhenNameIsEmpty()
    {
        var command = new UpdateTicketCategoryCommand(Guid.NewGuid(), "");

        TestValidationResult<UpdateTicketCategoryCommand> result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Update_Should_HaveValidationError_WhenNameExceeds100Chars()
    {
        var command = new UpdateTicketCategoryCommand(Guid.NewGuid(), new string('x', 101));

        TestValidationResult<UpdateTicketCategoryCommand> result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Update_Should_HaveValidationError_WhenDescriptionExceeds500Chars()
    {
        var command = new UpdateTicketCategoryCommand(Guid.NewGuid(), "Valid", new string('d', 501));

        TestValidationResult<UpdateTicketCategoryCommand> result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void Update_Should_HaveValidationError_WhenColorsAreInvalid()
    {
        var command = new UpdateTicketCategoryCommand(
            Guid.NewGuid(),
            "Valid",
            BackgroundColor: "bad",
            ForegroundColor: "bad");

        TestValidationResult<UpdateTicketCategoryCommand> result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.BackgroundColor);
        result.ShouldHaveValidationErrorFor(c => c.ForegroundColor);
    }

    [Fact]
    public void Update_Should_NotHaveValidationError_WhenCommandIsValid()
    {
        var command = new UpdateTicketCategoryCommand(
            Guid.NewGuid(),
            "Valid",
            "Description",
            "#000000",
            "#FFFFFF");

        TestValidationResult<UpdateTicketCategoryCommand> result = _updateValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Archive_Should_HaveValidationError_WhenCategoryIdIsEmpty()
    {
        var command = new ArchiveTicketCategoryCommand(Guid.Empty);

        TestValidationResult<ArchiveTicketCategoryCommand> result = _archiveValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CategoryId);
    }

    [Fact]
    public void Archive_Should_NotHaveValidationError_WhenCategoryIdIsNotEmpty()
    {
        var command = new ArchiveTicketCategoryCommand(Guid.NewGuid());

        TestValidationResult<ArchiveTicketCategoryCommand> result = _archiveValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Unarchive_Should_HaveValidationError_WhenCategoryIdIsEmpty()
    {
        var command = new UnarchiveTicketCategoryCommand(Guid.Empty);

        TestValidationResult<UnarchiveTicketCategoryCommand> result = _unarchiveValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CategoryId);
    }

    [Fact]
    public void Unarchive_Should_NotHaveValidationError_WhenCategoryIdIsNotEmpty()
    {
        var command = new UnarchiveTicketCategoryCommand(Guid.NewGuid());

        TestValidationResult<UnarchiveTicketCategoryCommand> result = _unarchiveValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
