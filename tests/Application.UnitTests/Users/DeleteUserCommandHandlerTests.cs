using Application.Abstractions.Authentication;
using Application.UnitTests.Abstractions;
using Application.Users.Delete;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class DeleteUserCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnCannotDeleteSelf_WhenDeletingOwnAccount()
    {
        // Arrange
        var currentAdminId = Guid.NewGuid();

        using UserManager<User> userManager = CreateUserManager();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(currentAdminId);
        HybridCache cache = CreateCache();

        var handler = new DeleteUserCommandHandler(userManager, userContext, cache);
        var command = new DeleteUserCommand(currentAdminId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.CannotDeleteSelf);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var currentAdminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        using UserManager<User> userManager = CreateUserManager();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(currentAdminId);
        HybridCache cache = CreateCache();

        userManager.FindByIdAsync(targetUserId.ToString()).Returns((User?)null);

        var handler = new DeleteUserCommandHandler(userManager, userContext, cache);
        var command = new DeleteUserCommand(targetUserId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.NotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnCannotDeleteLastAdmin_WhenDeletingTheOnlyAdmin()
    {
        // Arrange
        var currentAdminId = Guid.NewGuid();
        var targetAdminId = Guid.NewGuid();

        using UserManager<User> userManager = CreateUserManager();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(currentAdminId);
        HybridCache cache = CreateCache();

        var targetAdmin = new User { Id = targetAdminId, Email = "target@example.com" };
        userManager.FindByIdAsync(targetAdminId.ToString()).Returns(targetAdmin);
        userManager.IsInRoleAsync(targetAdmin, RoleNames.Admin).Returns(true);
        userManager.GetUsersInRoleAsync(RoleNames.Admin).Returns([targetAdmin]);

        var handler = new DeleteUserCommandHandler(userManager, userContext, cache);
        var command = new DeleteUserCommand(targetAdminId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.CannotDeleteLastAdmin);
    }

    [Fact]
    public async Task Handle_Should_DeleteUserAndRaiseDomainEvent_WhenValid()
    {
        // Arrange
        var currentAdminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        using UserManager<User> userManager = CreateUserManager();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(currentAdminId);
        HybridCache cache = CreateCache();

        var targetUser = new User { Id = targetUserId, Email = "target@example.com" };
        userManager.FindByIdAsync(targetUserId.ToString()).Returns(targetUser);
        userManager.IsInRoleAsync(targetUser, RoleNames.Admin).Returns(false);
        userManager.DeleteAsync(targetUser).Returns(IdentityResult.Success);

        var handler = new DeleteUserCommandHandler(userManager, userContext, cache);
        var command = new DeleteUserCommand(targetUserId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        targetUser.DomainEvents.ShouldContain(e => e is UserDeletedDomainEvent);
        await userManager.Received(1).DeleteAsync(targetUser);
    }
}
