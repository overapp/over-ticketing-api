using Infrastructure.Database;
using Infrastructure.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using SharedKernel;
using Shouldly;
using Xunit;

namespace ArchitectureTests.Database;

public class DatabaseModelTests
{
    [Fact]
    public void ApplicationDbContext_ShouldNotHave_PendingModelChanges()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder
            .UseSqlServer("Server=localhost;Database=Test;Integrated Security=True;TrustServerCertificate=True", sqlServerOptions =>
                sqlServerOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default))
            .UseSnakeCaseNamingConvention();

        using var dbContext = new ApplicationDbContext(optionsBuilder.Options, new NoOpDomainEventsDispatcher());

        bool hasPendingChanges = dbContext.Database.HasPendingModelChanges();

        hasPendingChanges.ShouldBeFalse();
    }

    private sealed class NoOpDomainEventsDispatcher : IDomainEventsDispatcher
    {
        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
