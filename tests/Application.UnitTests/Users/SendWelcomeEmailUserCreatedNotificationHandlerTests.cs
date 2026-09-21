using Application.Abstractions.Emails;
using Application.UnitTests.Abstractions;
using Application.Users.Create;
using Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Users;

public sealed class SendWelcomeEmailUserCreatedNotificationHandlerTests : BaseHandlerTest
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();
    private readonly IOptions<EmailNotificationOptions> _emailOptions = Options.Create(new EmailNotificationOptions
    {
        AppBaseUrl = "https://ticketing.overapp.com"
    });
    private readonly ILogger<SendWelcomeEmailUserCreatedNotificationHandler> _logger =
        NullLogger<SendWelcomeEmailUserCreatedNotificationHandler>.Instance;

    public SendWelcomeEmailUserCreatedNotificationHandlerTests()
    {
        _templateRenderer.RenderAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("<html>Welcome</html>"));
    }

    [Fact]
    public async Task Handle_Should_NotSendEmail_WhenUserDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new SendWelcomeEmailUserCreatedNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            _logger);

        var notification = new UserCreatedNotification(Guid.NewGuid());

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
    public async Task Handle_Should_SendWelcomeEmail_WhenUserExists()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "newuser@example.com",
            FirstName = "Giulia",
            LastName = "Bianchi"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new SendWelcomeEmailUserCreatedNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            _logger);

        var notification = new UserCreatedNotification(user.Id);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await _emailSender.Received(1).SendAsync(
            "newuser@example.com",
            "Benvenuto su OverTicketing",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
