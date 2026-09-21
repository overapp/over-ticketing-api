using Application.TicketCategories.Archive;
using Application.TicketCategories.Unarchive;
using Application.UnitTests.Abstractions;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.TicketCategories;

public sealed class ArchiveAndUnarchiveTicketCategoryCommandHandlerTests : BaseHandlerTest
{
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly DateTime _utcNow = new(2026, 9, 21, 14, 0, 0, DateTimeKind.Utc);

    public ArchiveAndUnarchiveTicketCategoryCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(_utcNow);
    }

    [Fact]
    public async Task Archive_Should_ReturnNotFound_WhenCategoryDoesNotExist()
    {
        await using TestDbContext context = CreateDbContext();
        var handler = new ArchiveTicketCategoryCommandHandler(context, _dateTimeProvider);
        var command = new ArchiveTicketCategoryCommand(Guid.NewGuid());

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketCategoryErrors.NotFound(command.CategoryId));
    }

    [Fact]
    public async Task Archive_Should_ReturnAlreadyArchived_WhenAlreadyArchived()
    {
        await using TestDbContext context = CreateDbContext();
        var category = new TicketCategory
        {
            Id = Guid.NewGuid(),
            Name = "Archived Category",
            Status = TicketCategoryStatus.Archived
        };
        context.TicketCategories.Add(category);
        await context.SaveChangesAsync();

        var handler = new ArchiveTicketCategoryCommandHandler(context, _dateTimeProvider);
        var command = new ArchiveTicketCategoryCommand(category.Id);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketCategoryErrors.AlreadyArchived(category.Id));
    }

    [Fact]
    public async Task Archive_Should_Succeed_WhenCategoryIsActive()
    {
        await using TestDbContext context = CreateDbContext();
        var category = new TicketCategory
        {
            Id = Guid.NewGuid(),
            Name = "Active Category",
            Status = TicketCategoryStatus.Active
        };
        context.TicketCategories.Add(category);
        await context.SaveChangesAsync();

        var handler = new ArchiveTicketCategoryCommandHandler(context, _dateTimeProvider);
        var command = new ArchiveTicketCategoryCommand(category.Id);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        TicketCategory? archived = await context.TicketCategories.SingleOrDefaultAsync(tc => tc.Id == category.Id);
        archived.ShouldNotBeNull();
        archived.Status.ShouldBe(TicketCategoryStatus.Archived);
        archived.UpdatedAt.ShouldBe(_utcNow);
        archived.DomainEvents.ShouldContain(e => e is TicketCategoryArchivedDomainEvent);
    }

    [Fact]
    public async Task Unarchive_Should_ReturnNotFound_WhenCategoryDoesNotExist()
    {
        await using TestDbContext context = CreateDbContext();
        var handler = new UnarchiveTicketCategoryCommandHandler(context, _dateTimeProvider);
        var command = new UnarchiveTicketCategoryCommand(Guid.NewGuid());

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketCategoryErrors.NotFound(command.CategoryId));
    }

    [Fact]
    public async Task Unarchive_Should_ReturnAlreadyActive_WhenAlreadyActive()
    {
        await using TestDbContext context = CreateDbContext();
        var category = new TicketCategory
        {
            Id = Guid.NewGuid(),
            Name = "Active Category",
            Status = TicketCategoryStatus.Active
        };
        context.TicketCategories.Add(category);
        await context.SaveChangesAsync();

        var handler = new UnarchiveTicketCategoryCommandHandler(context, _dateTimeProvider);
        var command = new UnarchiveTicketCategoryCommand(category.Id);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketCategoryErrors.AlreadyActive(category.Id));
    }

    [Fact]
    public async Task Unarchive_Should_Succeed_WhenCategoryIsArchived()
    {
        await using TestDbContext context = CreateDbContext();
        var category = new TicketCategory
        {
            Id = Guid.NewGuid(),
            Name = "Archived Category",
            Status = TicketCategoryStatus.Archived
        };
        context.TicketCategories.Add(category);
        await context.SaveChangesAsync();

        var handler = new UnarchiveTicketCategoryCommandHandler(context, _dateTimeProvider);
        var command = new UnarchiveTicketCategoryCommand(category.Id);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        TicketCategory? active = await context.TicketCategories.SingleOrDefaultAsync(tc => tc.Id == category.Id);
        active.ShouldNotBeNull();
        active.Status.ShouldBe(TicketCategoryStatus.Active);
        active.UpdatedAt.ShouldBe(_utcNow);
        active.DomainEvents.ShouldContain(e => e is TicketCategoryUnarchivedDomainEvent);
    }
}
