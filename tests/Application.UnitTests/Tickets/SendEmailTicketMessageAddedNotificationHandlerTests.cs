using Application.Abstractions.Emails;
using Application.Tickets.Reply;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Tickets;

public sealed class SendEmailTicketMessageAddedNotificationHandlerTests : BaseHandlerTest
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();
    private readonly IOptions<EmailNotificationOptions> _emailOptions = Options.Create(new EmailNotificationOptions
    {
        AppBaseUrl = "https://ticketing.overapp.com"
    });
    private readonly ILogger<SendEmailTicketMessageAddedNotificationHandler> _logger =
        NullLogger<SendEmailTicketMessageAddedNotificationHandler>.Instance;

    public SendEmailTicketMessageAddedNotificationHandlerTests()
    {
        _templateRenderer.RenderAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("<html>Rendered</html>"));
    }

    [Fact]
    public async Task Handle_Should_NotSendEmail_WhenMessageIsInternal()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new SendEmailTicketMessageAddedNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            _logger);

        var notification = new TicketMessageAddedNotification(Guid.NewGuid(), Guid.NewGuid(), IsInternal: true);

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
    public async Task Handle_Should_SendEmailToCustomer_WhenSupportReplies()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = "customer@example.com",
            FirstName = "Cliente",
            LastName = "Uno"
        };
        var supportUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "support@example.com",
            FirstName = "Operatore",
            LastName = "Supporto"
        };

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "OverTicketing",
            Status = ProjectStatus.Active
        };

        var supportAssignment = new ProjectAssignment
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
            CreatedByUserId = customer.Id,
            Title = "Problema di accesso",
            Status = TicketStatus.New
        };

        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = supportUser.Id,
            Content = "Abbiamo reimpostato la password",
            IsInternal = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(customer, supportUser);
        context.Projects.Add(project);
        context.ProjectAssignments.Add(supportAssignment);
        context.Tickets.Add(ticket);
        context.TicketMessages.Add(message);
        await context.SaveChangesAsync();

        var handler = new SendEmailTicketMessageAddedNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            _logger);

        var notification = new TicketMessageAddedNotification(ticket.Id, message.Id, IsInternal: false);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - Customer receives reply notification
        await _emailSender.Received(1).SendAsync(
            "customer@example.com",
            Arg.Is<string>(s => s.Contains("[Risposta Ticket") && s.Contains("Problema di accesso")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SendEmailToSupportAndAdmins_WhenNormalUserReplies()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = "customer@example.com",
            FirstName = "Cliente",
            LastName = "Uno"
        };
        var supportUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "support@example.com",
            FirstName = "Operatore",
            LastName = "Supporto"
        };
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "Centrale"
        };

        var adminRole = new Role(RoleNames.Admin) { Id = Guid.NewGuid() };
        var adminUserRole = new IdentityUserRole<Guid> { UserId = adminUser.Id, RoleId = adminRole.Id };

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "OverTicketing",
            Status = ProjectStatus.Active
        };

        var customerAssignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = customer.Id,
            Role = RoleNames.User
        };
        var supportAssignment = new ProjectAssignment
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
            CreatedByUserId = customer.Id,
            Title = "Problema di accesso",
            Status = TicketStatus.WaitingForCustomer
        };

        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = customer.Id,
            Content = "Grazie, ora funziona tutto!",
            IsInternal = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(customer, supportUser, adminUser);
        context.Roles.Add(adminRole);
        context.UserRoles.Add(adminUserRole);
        context.Projects.Add(project);
        context.ProjectAssignments.AddRange(customerAssignment, supportAssignment);
        context.Tickets.Add(ticket);
        context.TicketMessages.Add(message);
        await context.SaveChangesAsync();

        var handler = new SendEmailTicketMessageAddedNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            _logger);

        var notification = new TicketMessageAddedNotification(ticket.Id, message.Id, IsInternal: false);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - Support receives email
        await _emailSender.Received(1).SendAsync(
            "support@example.com",
            Arg.Is<string>(s => s.Contains("[Supporto - Nuova Risposta") && s.Contains("Problema di accesso")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Assert - Admin receives email
        await _emailSender.Received(1).SendAsync(
            "admin@example.com",
            Arg.Is<string>(s => s.Contains("[Amministrazione - Nuova Risposta") && s.Contains("Problema di accesso")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotSendEmailToCustomer_WhenCustomerDisabledNotifyOnTicketReply()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = "customer@example.com",
            FirstName = "Cliente",
            LastName = "Uno"
        };
        var supportUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "support@example.com",
            FirstName = "Operatore",
            LastName = "Supporto"
        };

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "OverTicketing",
            Status = ProjectStatus.Active
        };

        var supportAssignment = new ProjectAssignment
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
            CreatedByUserId = customer.Id,
            Title = "Problema di accesso",
            Status = TicketStatus.New
        };

        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = supportUser.Id,
            Content = "Abbiamo reimpostato la password",
            IsInternal = false,
            CreatedAt = DateTime.UtcNow
        };

        var customerSettings = new UserSettings
        {
            UserId = customer.Id,
            NotifyOnTicketCreated = true,
            NotifyOnTicketReply = false
        };

        context.Users.AddRange(customer, supportUser);
        context.UserSettings.Add(customerSettings);
        context.Projects.Add(project);
        context.ProjectAssignments.Add(supportAssignment);
        context.Tickets.Add(ticket);
        context.TicketMessages.Add(message);
        await context.SaveChangesAsync();

        var handler = new SendEmailTicketMessageAddedNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            _logger);

        var notification = new TicketMessageAddedNotification(ticket.Id, message.Id, IsInternal: false);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - Customer does not receive email
        await _emailSender.DidNotReceive().SendAsync(
            "customer@example.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SkipStaff_WhenStaffDisabledNotifyOnTicketReply()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = "customer@example.com",
            FirstName = "Cliente",
            LastName = "Uno"
        };
        var supportUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "support@example.com",
            FirstName = "Operatore",
            LastName = "Supporto"
        };
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "Centrale"
        };

        var adminRole = new Role(RoleNames.Admin) { Id = Guid.NewGuid() };
        var adminUserRole = new IdentityUserRole<Guid> { UserId = adminUser.Id, RoleId = adminRole.Id };

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "OverTicketing",
            Status = ProjectStatus.Active
        };

        var customerAssignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = customer.Id,
            Role = RoleNames.User
        };
        var supportAssignment = new ProjectAssignment
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
            CreatedByUserId = customer.Id,
            Title = "Problema di accesso",
            Status = TicketStatus.WaitingForCustomer
        };

        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = customer.Id,
            Content = "Grazie!",
            IsInternal = false,
            CreatedAt = DateTime.UtcNow
        };

        var supportSettings = new UserSettings
        {
            UserId = supportUser.Id,
            NotifyOnTicketCreated = true,
            NotifyOnTicketReply = false
        };

        context.Users.AddRange(customer, supportUser, adminUser);
        context.UserSettings.Add(supportSettings);
        context.Roles.Add(adminRole);
        context.UserRoles.Add(adminUserRole);
        context.Projects.Add(project);
        context.ProjectAssignments.AddRange(customerAssignment, supportAssignment);
        context.Tickets.Add(ticket);
        context.TicketMessages.Add(message);
        await context.SaveChangesAsync();

        var handler = new SendEmailTicketMessageAddedNotificationHandler(
            context,
            _emailSender,
            _templateRenderer,
            _emailOptions,
            _logger);

        var notification = new TicketMessageAddedNotification(ticket.Id, message.Id, IsInternal: false);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - Support was skipped
        await _emailSender.DidNotReceive().SendAsync(
            "support@example.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        // Assert - Admin received notification
        await _emailSender.Received(1).SendAsync(
            "admin@example.com",
            Arg.Is<string>(s => s.Contains("[Amministrazione - Nuova Risposta")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
