using Application.Wiki.CreateGlobalWikiPage;
using Application.Wiki.UpdateProjectWikiPage;
using Application.Wiki.UpdateWikiPageStatus;
using Domain.Wiki;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.UnitTests.Wiki;

public sealed class WikiValidatorsTests
{
    private readonly CreateGlobalWikiPageCommandValidator _createGlobalValidator = new();
    private readonly UpdateProjectWikiPageCommandValidator _updateProjectValidator = new();
    private readonly UpdateWikiPageStatusCommandValidator _updateStatusValidator = new();

    [Fact]
    public void CreateGlobal_Should_HaveValidationError_WhenTitleIsEmpty()
    {
        var command = new CreateGlobalWikiPageCommand("", null, "Content", null, false);

        TestValidationResult<CreateGlobalWikiPageCommand> result = _createGlobalValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void CreateGlobal_Should_HaveValidationError_WhenTitleExceeds200Chars()
    {
        var command = new CreateGlobalWikiPageCommand(new string('a', 201), null, "Content", null, false);

        TestValidationResult<CreateGlobalWikiPageCommand> result = _createGlobalValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void CreateGlobal_Should_HaveValidationError_WhenContentIsNull()
    {
        var command = new CreateGlobalWikiPageCommand("Title", null, null!, null, false);

        TestValidationResult<CreateGlobalWikiPageCommand> result = _createGlobalValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Content);
    }

    [Fact]
    public void CreateGlobal_Should_HaveValidationError_WhenSlugExceeds200Chars()
    {
        var command = new CreateGlobalWikiPageCommand("Title", new string('s', 201), "Content", null, false);

        TestValidationResult<CreateGlobalWikiPageCommand> result = _createGlobalValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Slug);
    }

    [Fact]
    public void CreateGlobal_Should_NotHaveValidationError_WhenCommandIsValid()
    {
        var command = new CreateGlobalWikiPageCommand("Title", "slug", "Content", null, false);

        TestValidationResult<CreateGlobalWikiPageCommand> result = _createGlobalValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateProject_Should_HaveValidationError_WhenProjectIdIsEmpty()
    {
        var command = new UpdateProjectWikiPageCommand(Guid.Empty, Guid.NewGuid(), "Title", null, "Content", null, false);

        TestValidationResult<UpdateProjectWikiPageCommand> result = _updateProjectValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ProjectId);
    }

    [Fact]
    public void UpdateProject_Should_HaveValidationError_WhenWikiPageIdIsEmpty()
    {
        var command = new UpdateProjectWikiPageCommand(Guid.NewGuid(), Guid.Empty, "Title", null, "Content", null, false);

        TestValidationResult<UpdateProjectWikiPageCommand> result = _updateProjectValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.WikiPageId);
    }

    [Fact]
    public void UpdateProject_Should_HaveValidationError_WhenTitleIsEmpty()
    {
        var command = new UpdateProjectWikiPageCommand(Guid.NewGuid(), Guid.NewGuid(), "", null, "Content", null, false);

        TestValidationResult<UpdateProjectWikiPageCommand> result = _updateProjectValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void UpdateProject_Should_HaveValidationError_WhenTitleExceeds200Chars()
    {
        var command = new UpdateProjectWikiPageCommand(Guid.NewGuid(), Guid.NewGuid(), new string('a', 201), null, "Content", null, false);

        TestValidationResult<UpdateProjectWikiPageCommand> result = _updateProjectValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void UpdateProject_Should_HaveValidationError_WhenContentIsNull()
    {
        var command = new UpdateProjectWikiPageCommand(Guid.NewGuid(), Guid.NewGuid(), "Title", null, null!, null, false);

        TestValidationResult<UpdateProjectWikiPageCommand> result = _updateProjectValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Content);
    }

    [Fact]
    public void UpdateProject_Should_HaveValidationError_WhenSlugExceeds200Chars()
    {
        var command = new UpdateProjectWikiPageCommand(Guid.NewGuid(), Guid.NewGuid(), "Title", new string('s', 201), "Content", null, false);

        TestValidationResult<UpdateProjectWikiPageCommand> result = _updateProjectValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Slug);
    }

    [Fact]
    public void UpdateProject_Should_NotHaveValidationError_WhenCommandIsValid()
    {
        var command = new UpdateProjectWikiPageCommand(Guid.NewGuid(), Guid.NewGuid(), "Title", "slug", "Content", null, false);

        TestValidationResult<UpdateProjectWikiPageCommand> result = _updateProjectValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateStatus_Should_HaveValidationError_WhenWikiPageIdIsEmpty()
    {
        var command = new UpdateWikiPageStatusCommand(Guid.Empty, WikiPageStatus.Published);

        TestValidationResult<UpdateWikiPageStatusCommand> result = _updateStatusValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.WikiPageId);
    }

    [Fact]
    public void UpdateStatus_Should_HaveValidationError_WhenStatusIsOutOfRange()
    {
        var command = new UpdateWikiPageStatusCommand(Guid.NewGuid(), (WikiPageStatus)999);

        TestValidationResult<UpdateWikiPageStatusCommand> result = _updateStatusValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Status);
    }

    [Fact]
    public void UpdateStatus_Should_NotHaveValidationError_WhenCommandIsValid()
    {
        var command = new UpdateWikiPageStatusCommand(Guid.NewGuid(), WikiPageStatus.Published);

        TestValidationResult<UpdateWikiPageStatusCommand> result = _updateStatusValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
