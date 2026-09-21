using System.Net;
using System.Security.Claims;
using Application.Abstractions.Authentication;
using Domain.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Web.Api.Middleware;

namespace IntegrationTests.Users;

public sealed class MustChangePasswordMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_CallNext_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var context = new DefaultHttpContext();
        bool nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new MustChangePasswordMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_WhenMustChangePasswordIsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(CustomClaims.MustChangePassword, "false")
        ], "TestAuth");
        context.User = new ClaimsPrincipal(identity);
        context.Request.Path = "/tickets";

        bool nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new MustChangePasswordMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.ShouldBeTrue();
    }

    [Theory]
    [InlineData("/auth/change-password")]
    [InlineData("/auth/me")]
    [InlineData("/auth/refresh-token")]
    [InlineData("/health")]
    public async Task InvokeAsync_Should_CallNext_WhenMustChangePasswordIsTrue_AndPathIsAllowed(string path)
    {
        // Arrange
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(CustomClaims.MustChangePassword, "true")
        ], "TestAuth");
        context.User = new ClaimsPrincipal(identity);
        context.Request.Path = path;

        bool nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new MustChangePasswordMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.ShouldBeTrue();
    }

    [Theory]
    [InlineData("/tickets")]
    [InlineData("/users")]
    [InlineData("/projects")]
    [InlineData("/organizations")]
    [InlineData("/wiki")]
    public async Task InvokeAsync_Should_Return403Forbidden_WhenMustChangePasswordIsTrue_AndPathIsNotAllowed(string path)
    {
        // Arrange
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(CustomClaims.MustChangePassword, "true")
        ], "TestAuth");
        context.User = new ClaimsPrincipal(identity);
        context.Request.Path = path;

        bool nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new MustChangePasswordMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }
}
