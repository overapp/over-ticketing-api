using Application.Abstractions.Emails;
using Application.UnitTests.Abstractions;
using Application.Users.AdminResetPassword;
using Application.Users.ChangePassword;
using Application.Users.ForgotPassword;
using Application.Users.Register;
using Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class EmailNotificationHandlersTests : BaseHandlerTest
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IOptions<EmailNotificationOptions> _emailOptions = Options.Create(new EmailNotificationOptions
    {
        AppBaseUrl = "https://ticketing.overapp.com"
    });

    public EmailNotificationHandlersTests()
    {
        _templateRenderer.RenderAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("<html>Email</html>"));
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task SendPasswordReset_Should_NotSendEmail_WhenUserDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        ILogger<SendPasswordResetEmailNotificationHandler> logger =
            NullLogger<SendPasswordResetEmailNotificationHandler>.Instance;
        var handler = new SendPasswordResetEmailNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            logger);

        var notification = new UserPasswordResetRequestedNotification(
            Guid.NewGuid(),
            "user@test.com",
            "reset-token");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await _emailSender.DidNotReceiveWithAnyArgs().SendAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendPasswordReset_Should_SendEmail_WhenUserExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            FirstName = "John",
            LastName = "Doe"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        ILogger<SendPasswordResetEmailNotificationHandler> logger =
            NullLogger<SendPasswordResetEmailNotificationHandler>.Instance;
        var handler = new SendPasswordResetEmailNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            logger);

        var notification = new UserPasswordResetRequestedNotification(
            user.Id,
            user.Email,
            "reset-token");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await _emailSender.Received(1).SendAsync(
            user.Email,
            "Recupero password OverTicketing",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendTemporaryPassword_Should_NotSendEmail_WhenUserDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        ILogger<SendTemporaryPasswordEmailNotificationHandler> logger =
            NullLogger<SendTemporaryPasswordEmailNotificationHandler>.Instance;
        var handler = new SendTemporaryPasswordEmailNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            logger);

        var notification = new UserTemporaryPasswordAssignedNotification(
            Guid.NewGuid(),
            "TempPass123!");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await _emailSender.DidNotReceiveWithAnyArgs().SendAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendTemporaryPassword_Should_SendEmail_WhenUserExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "temp@test.com",
            FirstName = "Alice",
            LastName = "Smith"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        ILogger<SendTemporaryPasswordEmailNotificationHandler> logger =
            NullLogger<SendTemporaryPasswordEmailNotificationHandler>.Instance;
        var handler = new SendTemporaryPasswordEmailNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            logger);

        var notification = new UserTemporaryPasswordAssignedNotification(
            user.Id,
            "TempPass123!");

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await _emailSender.Received(1).SendAsync(
            user.Email,
            "Reset password OverTicketing",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendPasswordChanged_Should_NotSendEmail_WhenUserDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        ILogger<SendPasswordChangedEmailNotificationHandler> logger =
            NullLogger<SendPasswordChangedEmailNotificationHandler>.Instance;
        var handler = new SendPasswordChangedEmailNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _dateTimeProvider,
            logger);

        var notification = new UserPasswordChangedNotification(Guid.NewGuid());

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await _emailSender.DidNotReceiveWithAnyArgs().SendAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendPasswordChanged_Should_SendEmail_WhenUserExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "changed@test.com",
            FirstName = "Bob",
            LastName = "Marley"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        ILogger<SendPasswordChangedEmailNotificationHandler> logger =
            NullLogger<SendPasswordChangedEmailNotificationHandler>.Instance;
        var handler = new SendPasswordChangedEmailNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _dateTimeProvider,
            logger);

        var notification = new UserPasswordChangedNotification(user.Id);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await _emailSender.Received(1).SendAsync(
            user.Email,
            "La tua password di OverTicketing è stata modificata",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UserRegisteredDomainEventHandler_Should_CompleteSuccessfully()
    {
        // Arrange
        var handler = new UserRegisteredDomainEventHandler();
        var domainEvent = new UserRegisteredDomainEvent(Guid.NewGuid());

        // Act
        Task task = handler.Handle(domainEvent, CancellationToken.None);
        await task;

        // Assert
        task.IsCompletedSuccessfully.ShouldBeTrue();
    }
}
