using Application.Abstractions.Authentication;
using Application.UnitTests.Abstractions;
using Application.Users.Update;
using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class UpdateUserCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        using UserManager<User> userManager = CreateUserManager();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(Guid.NewGuid());
        HybridCache cache = CreateCache();

        userManager.FindByIdAsync(Arg.Any<string>()).Returns((User?)null);

        var handler = new UpdateUserCommandHandler(userManager, userContext, cache);
        var command = new UpdateUserCommand(Guid.NewGuid(), "test@example.com", "John", "Doe");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.NotFound");
    }

    [Fact]
    public async Task Handle_Should_ReturnDuplicateEmail_WhenEmailBelongsToAnotherUser()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        using UserManager<User> userManager = CreateUserManager();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(Guid.NewGuid());
        HybridCache cache = CreateCache();

        var existingUser = new User { Id = targetUserId, Email = "old@example.com" };
        var duplicateUser = new User { Id = otherUserId, Email = "new@example.com" };

        userManager.FindByIdAsync(targetUserId.ToString()).Returns(existingUser);
        userManager.FindByEmailAsync("new@example.com").Returns(duplicateUser);

        var handler = new UpdateUserCommandHandler(userManager, userContext, cache);
        var command = new UpdateUserCommand(targetUserId, "new@example.com", "John", "Doe");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.DuplicateEmail);
    }

    [Fact]
    public async Task Handle_Should_ReturnCannotDemoteSelf_WhenAdminAttemptsToDemoteThemselves()
    {
        // Arrange
        var adminId = Guid.NewGuid();

        using UserManager<User> userManager = CreateUserManager();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(adminId);
        HybridCache cache = CreateCache();

        var adminUser = new User { Id = adminId, Email = "admin@example.com" };
        userManager.FindByIdAsync(adminId.ToString()).Returns(adminUser);
        userManager.GetRolesAsync(adminUser).Returns([RoleNames.Admin, RoleNames.User]);

        var handler = new UpdateUserCommandHandler(userManager, userContext, cache);
        var command = new UpdateUserCommand(adminId, "admin@example.com", "Admin", "User", [RoleNames.User]);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.CannotDemoteSelf);
    }

    [Fact]
    public async Task Handle_Should_ReturnCannotDemoteLastAdmin_WhenDemotingTheOnlyAdmin()
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
        userManager.GetRolesAsync(targetAdmin).Returns([RoleNames.Admin, RoleNames.User]);
        userManager.GetUsersInRoleAsync(RoleNames.Admin).Returns([targetAdmin]);

        var handler = new UpdateUserCommandHandler(userManager, userContext, cache);
        var command = new UpdateUserCommand(targetAdminId, "target@example.com", "Target", "User", [RoleNames.User]);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.CannotDemoteLastAdmin);
    }

    [Fact]
    public async Task Handle_Should_UpdateUserAndRolesAndRaiseDomainEvent_WhenValid()
    {
        // Arrange
        var currentAdminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        using UserManager<User> userManager = CreateUserManager();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(currentAdminId);
        HybridCache cache = CreateCache();

        var targetUser = new User
        {
            Id = targetUserId,
            Email = "user@example.com",
            FirstName = "Old",
            LastName = "Name"
        };

        userManager.FindByIdAsync(targetUserId.ToString()).Returns(targetUser);
        userManager.GetRolesAsync(targetUser).Returns([RoleNames.User]);
        userManager.UpdateAsync(targetUser).Returns(IdentityResult.Success);
        userManager.AddToRolesAsync(targetUser, Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);

        var handler = new UpdateUserCommandHandler(userManager, userContext, cache);
        var command = new UpdateUserCommand(
            targetUserId,
            "updated@example.com",
            "NewFirstName",
            "NewLastName",
            [RoleNames.Support]);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        targetUser.FirstName.ShouldBe("NewFirstName");
        targetUser.LastName.ShouldBe("NewLastName");
        targetUser.Email.ShouldBe("updated@example.com");
        targetUser.UserName.ShouldBe("updated@example.com");
        targetUser.DomainEvents.ShouldContain(e => e is UserUpdatedDomainEvent);

        await userManager.Received(1).AddToRolesAsync(
            targetUser,
            Arg.Is<IEnumerable<string>>(roles => roles.Contains(RoleNames.Support)));
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUpdateAsyncFails()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        using UserManager<User> userManager = CreateUserManager();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(Guid.NewGuid());
        HybridCache cache = CreateCache();

        var targetUser = new User { Id = targetUserId, Email = "user@example.com" };
        userManager.FindByIdAsync(targetUserId.ToString()).Returns(targetUser);
        userManager.GetRolesAsync(targetUser).Returns([RoleNames.User]);
        userManager.UpdateAsync(targetUser).Returns(IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure", Description = "Concurrency error" }));

        var handler = new UpdateUserCommandHandler(userManager, userContext, cache);
        var command = new UpdateUserCommand(targetUserId, "user@example.com", "First", "Last");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.ConcurrencyFailure");
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenRemoveFromRolesFails()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        using UserManager<User> userManager = CreateUserManager();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(Guid.NewGuid());
        HybridCache cache = CreateCache();

        var targetUser = new User { Id = targetUserId, Email = "user@example.com" };
        userManager.FindByIdAsync(targetUserId.ToString()).Returns(targetUser);
        userManager.GetRolesAsync(targetUser).Returns([RoleNames.User, RoleNames.Support]);
        userManager.UpdateAsync(targetUser).Returns(IdentityResult.Success);
        userManager.RemoveFromRolesAsync(targetUser, Arg.Any<IEnumerable<string>>())
            .Returns(IdentityResult.Failed(new IdentityError { Code = "RemoveRoleFailed", Description = "Cannot remove" }));

        var handler = new UpdateUserCommandHandler(userManager, userContext, cache);
        var command = new UpdateUserCommand(targetUserId, "user@example.com", "First", "Last", [RoleNames.User]); // Removes Support

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.RemoveRoleFailed");
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenAddToRolesFails()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        using UserManager<User> userManager = CreateUserManager();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(Guid.NewGuid());
        HybridCache cache = CreateCache();

        var targetUser = new User { Id = targetUserId, Email = "user@example.com" };
        userManager.FindByIdAsync(targetUserId.ToString()).Returns(targetUser);
        userManager.GetRolesAsync(targetUser).Returns([RoleNames.User]);
        userManager.UpdateAsync(targetUser).Returns(IdentityResult.Success);
        userManager.AddToRolesAsync(targetUser, Arg.Any<IEnumerable<string>>())
            .Returns(IdentityResult.Failed(new IdentityError { Code = "AddRoleFailed", Description = "Cannot add" }));

        var handler = new UpdateUserCommandHandler(userManager, userContext, cache);
        var command = new UpdateUserCommand(targetUserId, "user@example.com", "First", "Last", [RoleNames.User, RoleNames.Support]);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.AddRoleFailed");
    }
}
