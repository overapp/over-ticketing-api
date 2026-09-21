using Application.Abstractions.Authentication;
using Application.Abstractions.Storage;
using Application.Tickets.GetAttachment;
using Application.UnitTests.Abstractions;
using Domain.Projects;
using Domain.Tickets;
using Domain.Users;
using SharedKernel;

namespace Application.UnitTests.Tickets;

public sealed class GetAttachmentQueryHandlerTests : BaseHandlerTest
{
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IFileStorageService _fileStorageService = Substitute.For<IFileStorageService>();
    private readonly Guid _userId = Guid.NewGuid();

    public GetAttachmentQueryHandlerTests()
    {
        _userContext.UserId.Returns(_userId);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenAttachmentDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new GetAttachmentQueryHandler(context, _userContext, _fileStorageService);
        var query = new GetAttachmentQuery(Guid.NewGuid());

        // Act
        Result<AttachmentDownloadResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Attachments.NotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenMessageDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var attachment = new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketMessageId = Guid.NewGuid(),
            FileName = "file.txt",
            ContentType = "text/plain",
            StoragePath = "path/file.txt"
        };
        context.TicketAttachments.Add(attachment);
        await context.SaveChangesAsync();

        var handler = new GetAttachmentQueryHandler(context, _userContext, _fileStorageService);
        var query = new GetAttachmentQuery(attachment.Id);

        // Act
        Result<AttachmentDownloadResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.MessageNotFound(attachment.TicketMessageId));
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTicketDoesNotExist()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var ticketId = Guid.NewGuid();
        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorUserId = _userId,
            Content = "Hello",
            IsInternal = false
        };
        var attachment = new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketMessageId = message.Id,
            FileName = "file.txt",
            ContentType = "text/plain",
            StoragePath = "path/file.txt"
        };
        context.TicketMessages.Add(message);
        context.TicketAttachments.Add(attachment);
        await context.SaveChangesAsync();

        var handler = new GetAttachmentQueryHandler(context, _userContext, _fileStorageService);
        var query = new GetAttachmentQuery(attachment.Id);

        // Act
        Result<AttachmentDownloadResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.NotFound(ticketId));
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserNotInProject()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Ticket",
            CreatedByUserId = _userId
        };
        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = _userId,
            Content = "Hello",
            IsInternal = false
        };
        var attachment = new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketMessageId = message.Id,
            FileName = "file.txt",
            ContentType = "text/plain",
            StoragePath = "path/file.txt"
        };
        context.Projects.Add(project);
        context.Tickets.Add(ticket);
        context.TicketMessages.Add(message);
        context.TicketAttachments.Add(attachment);
        await context.SaveChangesAsync();

        var handler = new GetAttachmentQueryHandler(context, _userContext, _fileStorageService);
        var query = new GetAttachmentQuery(attachment.Id);

        // Act
        Result<AttachmentDownloadResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.UserNotInProject(_userId, project.Id));
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserIsNotSupportOrAdminAndNotCreator()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.User
        };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Ticket",
            CreatedByUserId = Guid.NewGuid() // Different creator
        };
        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = ticket.CreatedByUserId,
            Content = "Hello",
            IsInternal = false
        };
        var attachment = new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketMessageId = message.Id,
            FileName = "file.txt",
            ContentType = "text/plain",
            StoragePath = "path/file.txt"
        };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(assignment);
        context.Tickets.Add(ticket);
        context.TicketMessages.Add(message);
        context.TicketAttachments.Add(attachment);
        await context.SaveChangesAsync();

        var handler = new GetAttachmentQueryHandler(context, _userContext, _fileStorageService);
        var query = new GetAttachmentQuery(attachment.Id);

        // Act
        Result<AttachmentDownloadResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.UnauthorizedAccess(ticket.Id));
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserIsNotSupportOrAdminAndMessageIsInternal()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.User
        };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Ticket",
            CreatedByUserId = _userId // Is creator
        };
        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = Guid.NewGuid(),
            Content = "Internal note",
            IsInternal = true // Internal message!
        };
        var attachment = new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketMessageId = message.Id,
            FileName = "internal.txt",
            ContentType = "text/plain",
            StoragePath = "path/internal.txt"
        };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(assignment);
        context.Tickets.Add(ticket);
        context.TicketMessages.Add(message);
        context.TicketAttachments.Add(attachment);
        await context.SaveChangesAsync();

        var handler = new GetAttachmentQueryHandler(context, _userContext, _fileStorageService);
        var query = new GetAttachmentQuery(attachment.Id);

        // Act
        Result<AttachmentDownloadResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TicketErrors.UnauthorizedAccess(ticket.Id));
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenFileStorageReturnsNull()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.User
        };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Ticket",
            CreatedByUserId = _userId
        };
        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = _userId,
            Content = "Hello",
            IsInternal = false
        };
        var attachment = new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketMessageId = message.Id,
            FileName = "file.txt",
            ContentType = "text/plain",
            StoragePath = "path/file.txt"
        };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(assignment);
        context.Tickets.Add(ticket);
        context.TicketMessages.Add(message);
        context.TicketAttachments.Add(attachment);
        await context.SaveChangesAsync();

        _fileStorageService.DownloadAsync(attachment.StoragePath, Arg.Any<CancellationToken>())
            .Returns((Stream?)null);

        var handler = new GetAttachmentQueryHandler(context, _userContext, _fileStorageService);
        var query = new GetAttachmentQuery(attachment.Id);

        // Act
        Result<AttachmentDownloadResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Attachments.FileNotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnAttachment_WhenUserIsCreatorAndMessageIsNotInternal()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.User
        };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Ticket",
            CreatedByUserId = _userId
        };
        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = _userId,
            Content = "Hello",
            IsInternal = false
        };
        var attachment = new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketMessageId = message.Id,
            FileName = "file.txt",
            ContentType = "text/plain",
            StoragePath = "path/file.txt"
        };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(assignment);
        context.Tickets.Add(ticket);
        context.TicketMessages.Add(message);
        context.TicketAttachments.Add(attachment);
        await context.SaveChangesAsync();

        using var memoryStream = new MemoryStream([1, 2, 3]);
        _fileStorageService.DownloadAsync(attachment.StoragePath, Arg.Any<CancellationToken>())
            .Returns(memoryStream);

        var handler = new GetAttachmentQueryHandler(context, _userContext, _fileStorageService);
        var query = new GetAttachmentQuery(attachment.Id);

        // Act
        Result<AttachmentDownloadResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.FileName.ShouldBe("file.txt");
        result.Value.ContentType.ShouldBe("text/plain");
        result.Value.Stream.ShouldBe(memoryStream);
    }

    [Fact]
    public async Task Handle_Should_ReturnAttachment_WhenUserIsSupportOrAdminEvenForInternalMessage()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var project = new Project { Id = Guid.NewGuid(), Name = "Project", Status = ProjectStatus.Active };
        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = _userId,
            Role = RoleNames.Support
        };
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Ticket",
            CreatedByUserId = Guid.NewGuid() // Different user created ticket
        };
        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = Guid.NewGuid(),
            Content = "Internal note",
            IsInternal = true
        };
        var attachment = new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketMessageId = message.Id,
            FileName = "internal.txt",
            ContentType = "text/plain",
            StoragePath = "path/internal.txt"
        };
        context.Projects.Add(project);
        context.ProjectAssignments.Add(assignment);
        context.Tickets.Add(ticket);
        context.TicketMessages.Add(message);
        context.TicketAttachments.Add(attachment);
        await context.SaveChangesAsync();

        using var memoryStream = new MemoryStream([1, 2, 3]);
        _fileStorageService.DownloadAsync(attachment.StoragePath, Arg.Any<CancellationToken>())
            .Returns(memoryStream);

        var handler = new GetAttachmentQueryHandler(context, _userContext, _fileStorageService);
        var query = new GetAttachmentQuery(attachment.Id);

        // Act
        Result<AttachmentDownloadResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.FileName.ShouldBe("internal.txt");
    }
}
