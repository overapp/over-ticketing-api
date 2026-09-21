using Application.TicketCategories.Update;
using Application.UnitTests.Abstractions;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.TicketCategories;

public sealed class UpdateTicketCategoryCommandHandlerTests : BaseHandlerTest
{
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly DateTime _utcNow = new(2026, 9, 21, 14, 0, 0, DateTimeKind.Utc);

    public UpdateTicketCategoryCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(_utcNow);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenCategoryDoesNotExist()
    {
        await using TestDbContext context = CreateDbContext();
        var handler = new UpdateTicketCategoryCommandHandler(context, _dateTimeProvider);
        var command = new UpdateTicketCategoryCommand(Guid.NewGuid(), "New Name");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketCategoryErrors.NotFound(command.CategoryId));
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenNameCollidesInSameScope()
    {
        await using TestDbContext context = CreateDbContext();
        var category1 = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Name = "Bug",
            Status = TicketCategoryStatus.Active
        };
        var category2 = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Name = "Feature",
            Status = TicketCategoryStatus.Active
        };
        context.TicketCategories.AddRange(category1, category2);
        await context.SaveChangesAsync();

        var handler = new UpdateTicketCategoryCommandHandler(context, _dateTimeProvider);
        var command = new UpdateTicketCategoryCommand(category2.Id, "bug");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketCategoryErrors.NameNotUnique("bug"));
    }

    [Fact]
    public async Task Handle_Should_UpdateCategorySuccessfully_WhenValid()
    {
        await using TestDbContext context = CreateDbContext();
        var category = new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Name = "Original Name",
            Description = "Original Desc",
            BackgroundColor = "#111111",
            ForegroundColor = "#FFFFFF",
            Status = TicketCategoryStatus.Active
        };
        context.TicketCategories.Add(category);
        await context.SaveChangesAsync();

        var handler = new UpdateTicketCategoryCommandHandler(context, _dateTimeProvider);
        var command = new UpdateTicketCategoryCommand(
            category.Id,
            "Updated Name",
            "Updated Desc",
            "#222222",
            "#EEEEEE");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        TicketCategory? updated = await context.TicketCategories.SingleOrDefaultAsync(tc => tc.Id == category.Id);
        updated.ShouldNotBeNull();
        updated.Name.ShouldBe("Updated Name");
        updated.Description.ShouldBe("Updated Desc");
        updated.BackgroundColor.ShouldBe("#222222");
        updated.ForegroundColor.ShouldBe("#EEEEEE");
        updated.UpdatedAt.ShouldBe(_utcNow);
        updated.DomainEvents.ShouldContain(e => e is TicketCategoryUpdatedDomainEvent);
    }
}
