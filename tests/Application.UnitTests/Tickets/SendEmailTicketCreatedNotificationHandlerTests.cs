using Application.Abstractions.Emails;
using Application.Tickets.Create;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Tickets;

public sealed class SendEmailTicketCreatedNotificationHandlerTests : BaseHandlerTest
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();
    private readonly IOptions<EmailNotificationOptions> _emailOptions = Options.Create(new EmailNotificationOptions
    {
        AppBaseUrl = "https://ticketing.overapp.com"
    });
    private readonly ILogger<SendEmailTicketCreatedNotificationHandler> _logger =
        Microsoft.Extensions.Logging.Abstractions.NullLogger<SendEmailTicketCreatedNotificationHandler>.Instance;

    public SendEmailTicketCreatedNotificationHandlerTests()
    {
        _templateRenderer.RenderAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("<html>Rendered</html>"));
    }

    [Fact]
    public async Task Handle_Should_NotSendEmail_WhenTicketDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new SendEmailTicketCreatedNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            _logger);

        var notification = new TicketCreatedNotification(Guid.NewGuid());

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
    public async Task Handle_Should_SendEmailToAuthorSupportAndAdmin_WhenSupportAndAdminUsersExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var author = new User
        {
            Id = Guid.NewGuid(),
            Email = "author@example.com",
            FirstName = "Mario",
            LastName = "Rossi"
        };
        var supportUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "support@example.com",
            FirstName = "Luigi",
            LastName = "Verdi"
        };
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "Boss"
        };

        var adminRole = new Role(RoleNames.Admin)
        {
            Id = Guid.NewGuid()
        };

        var userRole = new IdentityUserRole<Guid>
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        };

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "OverTicketing Project",
            Status = ProjectStatus.Active
        };

        var projectAssignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = supportUser.Id,
            Role = RoleNames.Support
        };

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CreatedByUserId = author.Id,
            Title = "Bug nel login",
            Priority = TicketPriority.High,
            Status = TicketStatus.New,
            CreatedAt = DateTime.UtcNow
        };

        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = author.Id,
            Content = "Non riesco ad accedere",
            CreatedAt = DateTime.UtcNow
        };
        ticket.Messages.Add(message);

        context.Users.AddRange(author, supportUser, adminUser);
        context.Roles.Add(adminRole);
        context.UserRoles.Add(userRole);
        context.Projects.Add(project);
        context.ProjectAssignments.Add(projectAssignment);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new SendEmailTicketCreatedNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            _logger);

        var notification = new TicketCreatedNotification(ticket.Id);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - Author received confirmation email
        await _emailSender.Received(1).SendAsync(
            "author@example.com",
            Arg.Is<string>(s => s.Contains("Bug nel login")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Assert - Support received ticket notification email
        await _emailSender.Received(1).SendAsync(
            "support@example.com",
            Arg.Is<string>(s => s.Contains("[Supporto - Nuovo Ticket") && s.Contains("Bug nel login")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Assert - Admin ALWAYS received ticket notification email
        await _emailSender.Received(1).SendAsync(
            "admin@example.com",
            Arg.Is<string>(s => s.Contains("[Amministrazione - Nuovo Ticket") && s.Contains("Bug nel login")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SendEmailToAdminUsers_WhenNoSupportUsersAreAssigned()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var author = new User
        {
            Id = Guid.NewGuid(),
            Email = "author@example.com",
            FirstName = "Mario",
            LastName = "Rossi"
        };
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "Boss"
        };

        var adminRole = new Role(RoleNames.Admin)
        {
            Id = Guid.NewGuid()
        };

        var userRole = new IdentityUserRole<Guid>
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        };

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "OverTicketing Project",
            Status = ProjectStatus.Active
        };

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CreatedByUserId = author.Id,
            Title = "Feature Request",
            Priority = TicketPriority.Low,
            Status = TicketStatus.New,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(author, adminUser);
        context.Roles.Add(adminRole);
        context.UserRoles.Add(userRole);
        context.Projects.Add(project);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new SendEmailTicketCreatedNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            _logger);

        var notification = new TicketCreatedNotification(ticket.Id);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - Author received email
        await _emailSender.Received(1).SendAsync(
            "author@example.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Assert - Admin received fallback notification email
        await _emailSender.Received(1).SendAsync(
            "admin@example.com",
            Arg.Is<string>(s => s.Contains("[Amministrazione - Nuovo Ticket") && s.Contains("Feature Request")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SkipSupportAndAdmin_WhenTheyDisabledNotifyOnTicketCreated()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var author = new User
        {
            Id = Guid.NewGuid(),
            Email = "author@example.com",
            FirstName = "Mario",
            LastName = "Rossi"
        };
        var supportUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "support@example.com",
            FirstName = "Luigi",
            LastName = "Verdi"
        };
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "Boss"
        };

        var adminRole = new Role(RoleNames.Admin) { Id = Guid.NewGuid() };
        var userRole = new IdentityUserRole<Guid> { UserId = adminUser.Id, RoleId = adminRole.Id };

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "OverTicketing Project",
            Status = ProjectStatus.Active
        };

        var projectAssignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = supportUser.Id,
            Role = RoleNames.Support
        };

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CreatedByUserId = author.Id,
            Title = "Bug nel login",
            Priority = TicketPriority.High,
            Status = TicketStatus.New,
            CreatedAt = DateTime.UtcNow
        };

        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = author.Id,
            Content = "Non riesco ad accedere",
            CreatedAt = DateTime.UtcNow
        };
        ticket.Messages.Add(message);

        // Support disabled notifications, Admin has no settings (defaults to true)
        var supportSettings = new UserSettings
        {
            UserId = supportUser.Id,
            NotifyOnTicketCreated = false,
            NotifyOnTicketReply = true
        };

        context.Users.AddRange(author, supportUser, adminUser);
        context.UserSettings.Add(supportSettings);
        context.Roles.Add(adminRole);
        context.UserRoles.Add(userRole);
        context.Projects.Add(project);
        context.ProjectAssignments.Add(projectAssignment);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var handler = new SendEmailTicketCreatedNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            _logger);

        var notification = new TicketCreatedNotification(ticket.Id);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - Author ALWAYS received confirmation email (transactional)
        await _emailSender.Received(1).SendAsync(
            "author@example.com",
            Arg.Is<string>(s => s.Contains("Bug nel login")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Assert - Support did NOT receive email because disabled
        await _emailSender.DidNotReceive().SendAsync(
            "support@example.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Assert - Admin received email because enabled by default
        await _emailSender.Received(1).SendAsync(
            "admin@example.com",
            Arg.Is<string>(s => s.Contains("[Amministrazione - Nuovo Ticket")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
