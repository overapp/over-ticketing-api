---
name: add-feature
description: Scaffold a complete Clean Architecture feature slice — command, query, or notification, custom handler, FluentValidation validator, minimal API endpoint, and tests (unit, validator, integration). Use when the user asks to add a feature, use case, command, query, notification, or endpoint to this Clean Architecture template.
argument-hint: <feature description, e.g. "archive a todo item", "get todos due this week", or "send ticket created email notification">
---

# Add a Feature (Vertical Slice)

Scaffold a full use case following this template's conventions: an Application-layer use case with a custom command/query/notification handler, an optional Web.Api minimal-API endpoint (for commands and queries), and tests. No MediatR — this codebase uses its own `ICommand`/`IQuery`/`INotification` abstractions with Scrutor-registered handlers and decorators.

## Workflow

1. **Classify the use case.** A state change is a **command**; a read is a **query**; an asynchronous/background side-effect or integration is a **notification**. Derive names from the existing pattern:
   - Command: use case verb + entity, e.g. `ArchiveTodoCommand`.
   - Query: `Get` + entity/filter, e.g. `GetOverdueTodosQuery`.
   - Notification: entity + action + `Notification`, e.g. `TicketCreatedNotification`.
2. **Check the Domain layer.** If the entity, its `{Entity}Errors` class, or a needed domain event doesn't exist, add it first (see the `add-entity` skill). Commands that change state should raise a domain event via `entity.Raise(...)`.
3. **Create the Application slice** in `src/Application/{Feature}/{UseCase}/` — command/query/notification, handler, validator (commands only), response DTO (queries only). Templates: [references/command-slice.md](references/command-slice.md), [references/query-slice.md](references/query-slice.md), and [references/notification-slice.md](references/notification-slice.md).
4. **Create the endpoint** (commands and queries only) in `src/Web.Api/Endpoints/{Feature}/{UseCase}.cs`. Template: [references/endpoint.md](references/endpoint.md). (Notifications are consumed asynchronously in background workers, not via HTTP endpoints).
5. **Write tests** — handler unit tests, validator tests (for commands), and an integration test (for endpoints). Templates: [references/tests.md](references/tests.md) and [references/notification-slice.md](references/notification-slice.md).
6. **Verify:** `dotnet build` then `dotnet test`. All test projects must pass, including `ArchitectureTests` (layer dependency rules).

## Non-negotiable conventions

- **Folder = use case.** One folder per use case under `src/Application/{Feature}/` (e.g. `Todos/Archive/` or `Tickets/Create/`), containing all files for that slice including any notification and its notification handlers.
- **Handlers are `internal sealed`** with primary constructors, implementing `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResponse>`, `IQueryHandler<TQuery, TResponse>`, or `INotificationEventHandler<TNotification>`.
- **No manual DI registration.** Handlers, validators, and endpoints are discovered by assembly scanning (`Scrutor`, `AddValidatorsFromAssembly`, `AddEndpoints`). Never touch `DependencyInjection.cs` for a new slice.
- **Return `Result` / `Result<T>`, never throw** for expected failures in commands/queries. Errors come from static factory methods on `{Entity}Errors` in the Domain layer with codes like `"Todos.NotFound"`.
- **Validation lives in a `{Command}Validator`** (FluentValidation). It runs automatically via `ValidationDecorator` — the handler never validates input shape itself. Queries and notifications have no validators.
- **Data access via `IApplicationDbContext`** (from `Application.Abstractions.Data`) — never reference Infrastructure from Application.
- **Authorization check** in handlers that act on user-owned data: compare `IUserContext.UserId` and return `UserErrors.Unauthorized()` on mismatch, or filter queries by `userContext.UserId`.
- **Queries project directly to a `{X}Response` DTO** with `.Select(...)` — never return domain entities. Cache hot reads with `HybridCache` using a `{Feature}CacheKeys` static class; invalidate in the commands that mutate the cached data.
- **Endpoints** implement `IEndpoint`, resolve the handler interface directly from DI, use `result.Match(Results.Ok, CustomResults.Problem)` (or `Results.NoContent` for void commands), tag with the `Tags` class, and call `.RequireAuthorization()`.

## Naming reference

| Artifact | Pattern | Example |
|---|---|---|
| Command | `{Verb}{Entity}Command` | `ArchiveTodoCommand` |
| Query | `Get{X}Query` | `GetOverdueTodosQuery` |
| Notification | `{Entity}{Action}Notification` | `TicketCreatedNotification` |
| Handler (Command/Query) | `{Command/Query}Handler` | `ArchiveTodoCommandHandler` |
| Handler (Notification) | `{Purpose}{Notification}Handler` | `SendEmailTicketCreatedNotificationHandler` |
| Validator | `{Command}Validator` | `ArchiveTodoCommandValidator` |
| Response | `{X}Response` | `TodoResponse` |
| Endpoint | `{UseCase}.cs` in `Endpoints/{Feature}/` | `Endpoints/Todos/Archive.cs` |
| Unit test | `{Handler}Tests` | `ArchiveTodoCommandHandlerTests` |
| Test method | `Handle_Should_{Outcome}_When{Condition}` | `Handle_Should_ReturnNotFound_WhenTodoDoesNotExist` |
