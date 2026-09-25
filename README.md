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

   The integration tests don't need these secrets: they generate a throwaway key pair for each run.

4. Delete the intermediate PEM/base64 files (`jwt-private.pem`, `jwt-public.pem`, `jwt-private.b64`,
   `jwt-public.b64`) once the secrets are stored — they should never be committed to source control.

### Secrets outside Development (Azure Key Vault)

Outside the `Development` environment the API loads its secrets from Azure Key Vault and refuses
to start without it. Configure (as app settings / environment variables, not secrets):

| Setting | Value |
| --- | --- |
| `KeyVault__Uri` | The vault URI, e.g. `https://<vault-name>.vault.azure.net/` |
| `KeyVault__ManagedIdentityClientId` | Client ID of a user-assigned managed identity. Leave unset for a system-assigned identity. |

The API authenticates with `DefaultAzureCredential` (managed identity in Azure, Azure CLI / Visual
Studio sign-in when run locally), so that identity needs the **Key Vault Secrets User** role on the vault.

Key Vault secret names use `--` in place of `:`:

| Secret name | Required |
| --- | --- |
| `Jwt--PrivateKey` | Always |
| `Jwt--PublicKey` | Always |
| `ConnectionStrings--Database` | Always |
| `ConnectionStrings--BlobStorage` | Always |
| `ConnectionStrings--QueueStorage` | Always |
| `Email--AzureCommunicationServices--ConnectionString` | When `Email:Provider` is `AzureCommunicationServices` |
| `Email--Smtp--Password` | When the SMTP server requires authentication |

Non-secret JWT settings (`Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationInMinutes`) can live in the vault
or in regular configuration.

On startup the API validates all required settings, in every environment, before touching the
database: missing values, malformed RSA keys, or a public key that doesn't belong to the private key
stop the app with a message naming each offending setting. Secrets are read once at startup, so restart
the app after rotating a secret in the vault.

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
