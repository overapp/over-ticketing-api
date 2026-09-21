using Application.TicketCategories.CreateGlobal;
using Application.UnitTests.Abstractions;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SharedKernel;
using Shouldly;
using Xunit;

namespace Application.UnitTests.TicketCategories;

public sealed class CreateGlobalTicketCategoryCommandHandlerTests : BaseHandlerTest
{
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly DateTime _utcNow = new(2026, 9, 21, 14, 0, 0, DateTimeKind.Utc);

    public CreateGlobalTicketCategoryCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(_utcNow);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenGlobalCategoryWithSameNameAlreadyExists()
    {
        await using TestDbContext context = CreateDbContext();
        context.TicketCategories.Add(new TicketCategory
        {
            Id = Guid.NewGuid(),
            ProjectId = null,
            Name = "Bug",
            Status = TicketCategoryStatus.Active,
            CreatedAt = _utcNow
        });
        await context.SaveChangesAsync();

        var handler = new CreateGlobalTicketCategoryCommandHandler(context, _dateTimeProvider);
        var command = new CreateGlobalTicketCategoryCommand("bug", "Description");

        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketCategoryErrors.NameNotUnique("bug"));
    }

    [Fact]
    public async Task Handle_Should_CreateGlobalCategoryWithDefaultColors_WhenColorsNotProvided()
    {
        await using TestDbContext context = CreateDbContext();
        var handler = new CreateGlobalTicketCategoryCommandHandler(context, _dateTimeProvider);
        var command = new CreateGlobalTicketCategoryCommand("Feature Request", "New ideas");

        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        TicketCategory? created = await context.TicketCategories.SingleOrDefaultAsync(tc => tc.Id == result.Value);
        created.ShouldNotBeNull();
        created.ProjectId.ShouldBeNull();
        created.Name.ShouldBe("Feature Request");
        created.Description.ShouldBe("New ideas");
        created.BackgroundColor.ShouldBe("#64748B");
        created.ForegroundColor.ShouldBe("#FFFFFF");
        created.Status.ShouldBe(TicketCategoryStatus.Active);
        created.CreatedAt.ShouldBe(_utcNow);
        created.DomainEvents.ShouldContain(e => e is TicketCategoryCreatedDomainEvent);
    }
}
