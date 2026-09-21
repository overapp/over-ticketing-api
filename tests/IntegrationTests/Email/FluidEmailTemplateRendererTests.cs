using Application.Tickets.Create;
using Infrastructure.Email;
using Shouldly;
using Xunit;

namespace IntegrationTests.Email;

public sealed class FluidEmailTemplateRendererTests
{
    private readonly FluidEmailTemplateRenderer _renderer = new();

    [Fact]
    public async Task RenderAsync_Should_RenderAuthorEmailWithLayout_WhenValidModelProvided()
    {
        // Arrange
        var model = new TicketCreatedAuthorEmailModel(
            RecipientName: "Mario Rossi",
            TicketId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Title: "Impossibile accedere alla dashboard",
            Priority: "High",
            Status: "New",
            ProjectName: "OverTicketing API",
            TicketUrl: new Uri("https://ticketing.overapp.com/tickets/11111111-1111-1111-1111-111111111111"),
            CreatedAt: "21/09/2026 16:00",
            FirstResponseDueAt: "21/09/2026 18:00");

        // Act
        string html = await _renderer.RenderAsync("ticket-created-author", model);

        // Assert
        html.ShouldNotBeNullOrWhiteSpace();
        html.ShouldContain("OverTicketing"); // from _layout.liquid
        html.ShouldContain("Mario Rossi");
        html.ShouldContain("11111111-1111-1111-1111-111111111111");
        html.ShouldContain("Impossibile accedere alla dashboard");
        html.ShouldContain("OverTicketing API");
        html.ShouldContain("https://ticketing.overapp.com/tickets/11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public async Task RenderAsync_Should_RenderSupportEmailWithLayoutAndFallbackBanner_WhenAdminFallbackIsTrue()
    {
        // Arrange
        var model = new TicketCreatedSupportEmailModel(
            RecipientName: "Admin Boss",
            TicketId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Title: "Crash all'avvio",
            Priority: "Critical",
            ProjectName: "Core Banking",
            AuthorName: "Giuseppe Garibaldi",
            AuthorEmail: "giuseppe@example.com",
            InitialMessage: "Si è verificato un errore critico",
            TicketUrl: new Uri("https://ticketing.overapp.com/tickets/22222222-2222-2222-2222-222222222222"),
            CreatedAt: "21/09/2026 16:30",
            FirstResponseDueAt: "21/09/2026 17:00",
            IsAdminFallback: true);

        // Act
        string html = await _renderer.RenderAsync("ticket-created-support", model);

        // Assert
        html.ShouldNotBeNullOrWhiteSpace();
        html.ShouldContain("OverTicketing");
        html.ShouldContain("Admin Boss");
        html.ShouldContain("Questo messaggio ti è stato inoltrato come Amministratore");
        html.ShouldContain("Giuseppe Garibaldi");
        html.ShouldContain("giuseppe@example.com");
        html.ShouldContain("Si è verificato un errore critico");
    }

    [Fact]
    public async Task RenderAsync_Should_RenderCustomerReplyEmailWithLayout_WhenValidModelProvided()
    {
        // Arrange
        var model = new Application.Tickets.Reply.TicketReplyCustomerEmailModel(
            RecipientName: "Mario Rossi",
            TicketId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Title: "Problema stampante",
            ProjectName: "Helpdesk IT",
            RepliedByName: "Supporto Tecnico",
            MessageContent: "Abbiamo riavviato lo spooler.",
            TicketUrl: new Uri("https://ticketing.overapp.com/tickets/33333333-3333-3333-3333-333333333333"),
            RepliedAt: "21/09/2026 17:00");

        // Act
        string html = await _renderer.RenderAsync("ticket-reply-customer", model);

        // Assert
        html.ShouldNotBeNullOrWhiteSpace();
        html.ShouldContain("OverTicketing");
        html.ShouldContain("Mario Rossi");
        html.ShouldContain("Supporto Tecnico");
        html.ShouldContain("Abbiamo riavviato lo spooler.");
    }

    [Fact]
    public async Task RenderAsync_Should_RenderStaffReplyEmailWithLayout_WhenValidModelProvided()
    {
        // Arrange
        var model = new Application.Tickets.Reply.TicketReplyStaffEmailModel(
            RecipientName: "Support Team",
            TicketId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Title: "Problema VPN",
            ProjectName: "Helpdesk IT",
            AuthorName: "Franco Franchi",
            AuthorEmail: "franco@example.com",
            MessageContent: "Ancora non riesco a connettermi.",
            TicketUrl: new Uri("https://ticketing.overapp.com/tickets/44444444-4444-4444-4444-444444444444"),
            RepliedAt: "21/09/2026 17:15",
            IsAdmin: false);

        // Act
        string html = await _renderer.RenderAsync("ticket-reply-staff", model);

        // Assert
        html.ShouldNotBeNullOrWhiteSpace();
        html.ShouldContain("OverTicketing");
        html.ShouldContain("Franco Franchi");
        html.ShouldContain("franco@example.com");
        html.ShouldContain("Ancora non riesco a connettermi.");
    }

    [Fact]
    public async Task RenderAsync_Should_RenderWelcomeEmailWithLayout_WhenValidModelProvided()
    {
        // Arrange
        var model = new Application.Users.Create.UserWelcomeEmailModel(
            RecipientName: "Anna Frank",
            Email: "anna@example.com",
            LoginUrl: new Uri("https://ticketing.overapp.com/login"));

        // Act
        string html = await _renderer.RenderAsync("user-welcome", model);

        // Assert
        html.ShouldNotBeNullOrWhiteSpace();
        html.ShouldContain("OverTicketing");
        html.ShouldContain("Anna Frank");
        html.ShouldContain("anna@example.com");
        html.ShouldContain("https://ticketing.overapp.com/login");
    }

    [Fact]
    public async Task RenderAsync_Should_RenderProjectAssignedEmailWithLayout_WhenValidModelProvided()
    {
        // Arrange
        var model = new Application.Projects.AssignUser.ProjectAssignedEmailModel(
            RecipientName: "Paolo Rossi",
            ProjectName: "Mobile App",
            Role: "Support",
            ProjectUrl: new Uri("https://ticketing.overapp.com/projects/55555555-5555-5555-5555-555555555555"));

        // Act
        string html = await _renderer.RenderAsync("project-assigned", model);

        // Assert
        html.ShouldNotBeNullOrWhiteSpace();
        html.ShouldContain("OverTicketing");
        html.ShouldContain("Paolo Rossi");
        html.ShouldContain("Mobile App");
        html.ShouldContain("Support");
    }

    [Fact]
    public async Task RenderAsync_Should_RenderPasswordResetEmailWithLayout_WhenValidModelProvided()
    {
        // Arrange
        var model = new Application.Users.ForgotPassword.UserPasswordResetEmailModel(
            RecipientName: "Paolo Rossi",
            Email: "paolo@example.com",
            ResetUrl: new Uri("https://ticketing.overapp.com/reset-password?email=paolo%40example.com&token=sample-token"));

        // Act
        string html = await _renderer.RenderAsync("user-password-reset", model);

        // Assert
        html.ShouldNotBeNullOrWhiteSpace();
        html.ShouldContain("OverTicketing");
        html.ShouldContain("Paolo Rossi");
        html.ShouldContain("paolo@example.com");
        html.ShouldContain("https://ticketing.overapp.com/reset-password?email=paolo%40example.com&token=sample-token");
    }

    [Fact]
    public async Task RenderAsync_Should_ThrowFileNotFoundException_WhenTemplateDoesNotExist()
    {
        // Arrange
        var model = new { Name = "Test" };

        // Act & Assert
        await Should.ThrowAsync<FileNotFoundException>(() =>
            _renderer.RenderAsync("non-existent-template", model));
    }
}
