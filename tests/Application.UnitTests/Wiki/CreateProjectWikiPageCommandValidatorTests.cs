using Application.Wiki.CreateProjectWikiPage;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.UnitTests.Wiki;

public sealed class CreateProjectWikiPageCommandValidatorTests
{
    private readonly CreateProjectWikiPageCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_HaveValidationError_WhenProjectIdIsEmpty()
    {
        var command = new CreateProjectWikiPageCommand(
            Guid.Empty,
            "Title",
            null,
            "Content",
            null,
            false);

        TestValidationResult<CreateProjectWikiPageCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ProjectId);
    }

    [Fact]
    public void Validate_Should_HaveValidationError_WhenTitleIsEmpty()
    {
        var command = new CreateProjectWikiPageCommand(
            Guid.NewGuid(),
            "",
            null,
            "Content",
            null,
            false);

        TestValidationResult<CreateProjectWikiPageCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void Validate_Should_HaveValidationError_WhenTitleExceedsMaxLength()
    {
        var command = new CreateProjectWikiPageCommand(
            Guid.NewGuid(),
            new string('a', 201),
            null,
            "Content",
            null,
            false);

        TestValidationResult<CreateProjectWikiPageCommand> result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void Validate_Should_NotHaveValidationError_WhenCommandIsValid()
    {
        var command = new CreateProjectWikiPageCommand(
            Guid.NewGuid(),
            "Valid Title",
            "valid-slug",
            "Content",
            null,
            false);

        TestValidationResult<CreateProjectWikiPageCommand> result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
