# Clean Architecture Template

A pragmatic Clean Architecture starter for **.NET 10**. Batteries included, opinionated where it matters, and easy to extend.

## What's included in the template?

- **SharedKernel** project with common Domain-Driven Design abstractions.
- **Domain** layer with sample entities and domain events.
- **Application** layer with abstractions for:
  - CQRS (lightweight, MediatR-free command/query handlers)
  - User management use cases
  - Cross-cutting concerns (logging, validation) implemented as decorators
- **Infrastructure** layer with:
  - ASP.NET Core **Identity** (users, roles, claims) with EF Core stores, mapped to a dedicated `identity` schema without the default `AspNet*` table prefixes
  - JWT authentication (**RS256**, asymmetric RSA key pair) with **refresh tokens** (with token rotation)
  - Permission-based authorization (backed by role claims, cached with **HybridCache**)
  - EF Core + SQL Server (snake_case naming, migrations)
  - **HybridCache** for fast, unified caching with cache invalidation
  - Structured logging through OpenTelemetry
- **Web.Api** layer with:
  - Minimal API endpoints
  - **Rate limiting** (configurable global + authentication policies)
  - **OpenTelemetry** logs, tracing, and metrics (ASP.NET Core, HTTP, SQL Client, runtime)
  - Global exception handling and `ProblemDetails`
  - OpenAPI + Scalar UI with JWT support
- **Aspire** AppHost and service defaults
  - SQL Server provisioning
  - Aspire dashboard for OpenTelemetry signals
- **Testing** projects
  - Architecture testing (`ArchitectureTests`)
  - Unit testing (`Application.UnitTests`)
  - Integration testing with **Aspire Testing** (`IntegrationTests`)

## Getting started

```bash
dotnet run --project aspire/AppHost
```

### JWT signing keys (RSA)

Access tokens are signed with **RS256**, using an RSA key pair instead of a shared secret. You need to
generate a development key pair once and provide it to the app via configuration.

1. Generate a 2048-bit RSA private key, then derive the public key from it:

   ```bash
   openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out jwt-private.pem
   openssl rsa -pubout -in jwt-private.pem -out jwt-public.pem
   ```

2. Convert the private key to base64-encoded PKCS8 DER, and the public key to base64-encoded
   X.509 SubjectPublicKeyInfo DER (the formats `RSA.ImportPkcs8PrivateKey` /
   `RSA.ImportSubjectPublicKeyInfo` expect):

   ```bash
   openssl pkcs8 -topk8 -inform PEM -outform DER -nocrypt -in jwt-private.pem | base64 | tr -d '\n' > jwt-private.b64
   openssl rsa -pubout -in jwt-private.pem -outform DER | base64 | tr -d '\n' > jwt-public.b64
   ```

3. Store the keys as user secrets for local development. The Aspire AppHost reads them as the
   `jwt-private-key` / `jwt-public-key` secret parameters and passes them to the API; the
   `src/Web.Api` secrets are only needed when running the API without Aspire:

   ```bash
   dotnet user-secrets set "Parameters:jwt-private-key" "$(cat jwt-private.b64)" --project aspire/AppHost
   dotnet user-secrets set "Parameters:jwt-public-key" "$(cat jwt-public.b64)" --project aspire/AppHost

   dotnet user-secrets set "Jwt:PrivateKey" "$(cat jwt-private.b64)" --project src/Web.Api
   dotnet user-secrets set "Jwt:PublicKey" "$(cat jwt-public.b64)" --project src/Web.Api
   ```

   The integration tests start the AppHost, so they need the AppHost secrets too.

4. Delete the intermediate PEM/base64 files (`jwt-private.pem`, `jwt-public.pem`, `jwt-private.b64`,
   `jwt-public.b64`) once the secrets are stored — they should never be committed to source control.

For non-local environments, provide `Jwt:PrivateKey` and `Jwt:PublicKey` (or the equivalent
`Jwt__PrivateKey` / `Jwt__PublicKey` environment variables) through your secret manager of choice
(e.g. Azure Key Vault, AWS Secrets Manager) instead of `appsettings.json`.

Run the full test suite (the integration tests start the Aspire AppHost with SQL Server, so
a container runtime must be running):

```bash
dotnet test OverTicketing.slnx
```

To target .NET 8 or .NET 9 instead of .NET 10, see the notes in `Directory.Build.props`.

I'm open to hearing your feedback about the template and what you'd like to see in future iterations.

If you're ready to learn more, check out [**Pragmatic Clean Architecture**](https://www.milanjovanovic.tech/pragmatic-clean-architecture?utm_source=ca-template):

- Domain-Driven Design
- Role-based authorization
- Permission-based authorization
- Distributed caching with Redis
- OpenTelemetry
- Outbox pattern
- API Versioning
- Unit testing
- Functional testing
- Integration testing

Stay awesome!
