using Application.Abstractions.Emails;
using Application.Projects.AssignUser;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Projects;

public sealed class SendEmailUserAssignedToProjectNotificationHandlerTests : BaseHandlerTest
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();
    private readonly IOptions<EmailNotificationOptions> _emailOptions = Options.Create(new EmailNotificationOptions
    {
        AppBaseUrl = "https://ticketing.overapp.com"
    });
    private readonly ILogger<SendEmailUserAssignedToProjectNotificationHandler> _logger =
        NullLogger<SendEmailUserAssignedToProjectNotificationHandler>.Instance;

    public SendEmailUserAssignedToProjectNotificationHandlerTests()
    {
        _templateRenderer.RenderAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("<html>Assigned</html>"));
    }

    [Fact]
    public async Task Handle_Should_NotSendEmail_WhenUserDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new SendEmailUserAssignedToProjectNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            _logger);

        var notification = new UserAssignedToProjectNotification(Guid.NewGuid(), Guid.NewGuid(), RoleNames.Support);

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
    public async Task Handle_Should_SendEmailToAssignedUser_WhenValid()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "assigned@example.com",
            FirstName = "Marco",
            LastName = "Neri"
        };
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Progetto Cloud",
            Status = ProjectStatus.Active
        };

        context.Users.Add(user);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var handler = new SendEmailUserAssignedToProjectNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            _logger);

        var notification = new UserAssignedToProjectNotification(project.Id, user.Id, RoleNames.Support);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await _emailSender.Received(1).SendAsync(
            "assigned@example.com",
            Arg.Is<string>(s => s.Contains("Progetto Cloud") && s.Contains("Assegnazione al progetto")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
