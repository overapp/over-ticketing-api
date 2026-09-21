# Notification Slice Templates

Files go in `src/Application/{Feature}/{UseCase}/`, alongside the command/use-case that originates or relates to the notification. Replace `{Feature}` (plural, e.g. `Tickets`), `{Entity}` (e.g. `Ticket`), `{Action}` (e.g. `Created`), and `{Purpose}` (e.g. `SendEmail`) throughout.

## Notification

Positional record implementing `INotification`:

```csharp
using Application.Abstractions.Notifications;

namespace Application.Tickets.Create;

public sealed record TicketCreatedNotification(Guid TicketId) : INotification;
```

If the notification carries additional metadata or context required for processing without re-querying everything:

```csharp
using Application.Abstractions.Notifications;

namespace Application.Tickets.Create;

public sealed record TicketCreatedNotification(
    Guid TicketId,
    Guid ProjectId,
    Guid CreatedByUserId) : INotification;
```

- Notifications are immutable messages representing an event or task that requires background side-effects.
- They are dispatched asynchronously (e.g., via Outbox and queue consumer workers).

## Notification Event Handler

`internal sealed`, primary constructor, implementing `INotificationEventHandler<TNotification>`. Handlers live in the same use-case folder as the notification.

```csharp
using Application.Abstractions.Data;
using Application.Abstractions.Emails;
using Application.Abstractions.Notifications;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Tickets.Create;

internal sealed class SendEmailTicketCreatedNotificationHandler(
    IApplicationDbContext context,
    IEmailSender emailSender,
    ILogger<SendEmailTicketCreatedNotificationHandler> logger)
    : INotificationEventHandler<TicketCreatedNotification>
{
    public async Task Handle(TicketCreatedNotification notification, CancellationToken cancellationToken)
    {
        Ticket? ticket = await context.Tickets
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == notification.TicketId, cancellationToken);

        if (ticket is null)
        {
            logger.LogWarning("Ticket with Id {TicketId} was not found for notification handling.", notification.TicketId);
            return;
        }

        // Perform side-effect (e.g., render email template and send via IEmailSender)
        await emailSender.SendAsync(
            recipient: "user@example.com",
            subject: $"Ticket #{ticket.Id} Created",
            bodyHtml: "<p>Your ticket has been created.</p>",
            cancellationToken: cancellationToken);
    }
}
```

Notes:
- **Assembly scanning**: Like command and query handlers, `INotificationEventHandler<T>` implementations are discovered and registered automatically by Scrutor in `Application/DependencyInjection.cs`. Never register them manually.
- **Independence**: Multiple notification handlers can listen to the same notification (e.g., one sends an email, another pushes a webhook or records an audit log). Each handler executes independently.
- **Failures & Retries**: Asynchronous handlers are driven by background workers; exceptions trigger retry policies and eventual poison queue routing if retries are exhausted.

## Tests

Notification handlers get unit tests in `tests/Application.UnitTests/{Feature}/{Handler}Tests.cs`:

```csharp
using Application.Abstractions.Emails;
using Application.Tickets.Create;
using Application.UnitTests.Abstractions;
using Domain.Tickets;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Tickets;

public sealed class SendEmailTicketCreatedNotificationHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_SendEmail_WhenTicketExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = "Test Ticket",
            CreatedAt = DateTime.UtcNow
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        IEmailSender emailSender = Substitute.For<IEmailSender>();
        ILogger<SendEmailTicketCreatedNotificationHandler> logger = Substitute.For<ILogger<SendEmailTicketCreatedNotificationHandler>>();

        var handler = new SendEmailTicketCreatedNotificationHandler(context, emailSender, logger);
        var notification = new TicketCreatedNotification(ticket.Id);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await emailSender.Received(1).SendAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
```
