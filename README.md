# MemeTokenHub Claim Service

The Claim Service is the private evidence and moderation bounded context for MemeTokenHub. It accepts project ownership, social identity, and official representative claims; validates user and token references; stores sensitive proof; supports moderator decisions and one appeal; and publishes a minimal `ClaimApproved` integration event without leaking evidence.

## Capabilities

- Authenticated claim submission with immutable proof snapshots and approved attachment references.
- Service-authenticated validation against User Service and Token Service.
- Moderator-only pending queue, review operation, and reviewed audit history.
- Owner-only, single-appeal workflow with optimistic concurrency.
- Public status responses designed for badges and intentionally free of evidence and moderator notes.
- Transactional MongoDB outbox and idempotent `ClaimApproved` event identifiers.
- Short-lived HMAC-signed attachment upload URLs with ownership records, content-type and size restrictions, and mandatory scanner approval before use.
- RFC 7807 error responses, JWT authentication, capability-based moderation authorization, dependency-aware health checks, Swagger/OpenAPI, and Swagger UI.

## Technology

- .NET 10 / ASP.NET Core
- MongoDB (a replica set is required for transactional approval/outbox writes)
- Azure Service Bus
- `MemeTokenHub.Shared` 1.1.14 from GitHub Packages
- NUnit, Moq, ASP.NET Core in-memory integration testing, and Swagger contract testing

## Solution layout

```text
src/MemeTokenHub.ClaimService.Api/
  Application/       Use cases and ports
  Configuration/     Strongly typed options
  Controllers/       HTTP API boundary
  Domain/            Claim state and business rules
  Dtos/              Separate public, owner, and moderator contracts
  Infrastructure/    MongoDB, REST validation, uploads, and Service Bus
  Middleware/        RFC 7807 exception mapping
tests/
  MemeTokenHub.ClaimService.UnitTests/
  MemeTokenHub.ClaimService.IntegrationTests/
  MemeTokenHub.ClaimService.ContractTests/
```

The implementation applies SOLID principles by depending on focused interfaces, keeping HTTP, domain, persistence, and messaging responsibilities separate, and registering implementations through dependency injection. Interfaces and production classes are kept in separate files.

## Prerequisites

1. Install the .NET 10 SDK selected by `global.json`.
2. Run MongoDB as a replica set.
3. Create an Azure Service Bus topic for claim events.
4. Obtain GitHub Packages read access for `https://nuget.pkg.github.com/isboat/index.json`.

Authenticate the configured package source without committing a token:

```bash
dotnet nuget update source MemeTokenHub \
  --username YOUR_GITHUB_USER \
  --password YOUR_GITHUB_TOKEN \
  --store-password-in-clear-text
```

The token needs `read:packages`. For local-only credentials, prefer a user-level NuGet configuration or environment-specific credential provider and do not commit generated credentials.

## Configuration

Configuration is read from standard ASP.NET Core providers. Production secrets must be supplied by Azure App Service settings or a secret manager rather than committed files.

| Section | Purpose |
| --- | --- |
| `MongoDb` | MongoDB connection string and `MemeTokenHubClaims` database name |
| `Jwt` | Platform JWT issuer, audience, and signing key |
| `ServiceAuthentication` | Service credential used for internal validation calls |
| `ServiceEndpoints` | User Service and Token Service base URLs |
| `Attachments` | Upload endpoint, HMAC signing key, lifetime, size, and MIME allowlist |
| `ServiceBus` | Enablement, Azure Service Bus connection string, and topic |

`ServiceBus:Enabled` is false in the safe local template. Enable it in a deployed environment to initialize indexes and publish outbox messages. Never use the placeholder secrets from `appsettings.json` in a real environment.

## Build and test

```bash
dotnet restore MemeTokenHub.ClaimService.sln
dotnet format MemeTokenHub.ClaimService.sln --no-restore --verify-no-changes
dotnet build MemeTokenHub.ClaimService.sln --configuration Release --no-restore
dotnet test MemeTokenHub.ClaimService.sln --configuration Release --no-build
```

Run locally with:

```bash
dotnet run --project src/MemeTokenHub.ClaimService.Api
```

Swagger UI is available at `/swagger`; the OpenAPI document is at `/swagger/v1/swagger.json`.

## Health checks

The service exposes three unauthenticated health endpoints for platform probes and the operations dashboard:

| Route | Purpose |
| --- | --- |
| `/health/live` | Lightweight application liveness check |
| `/health/ready` | Readiness check covering the application and every enabled dependency |
| `/health` | Detailed dashboard response with overall status, timestamp, duration, and per-component results |

The dashboard endpoint checks Claim Service itself, MongoDB, User Service, Token Service, and Azure Service Bus when messaging is enabled. Downstream services are queried through their `/health/ready` endpoints using the same service-authenticated clients as application traffic. A required dependency failure produces HTTP `503 Service Unavailable`; a fully healthy report produces HTTP `200 OK`. Exception details and connection secrets are never included in the response.

## API overview

| Method | Route | Access | Purpose |
| --- | --- | --- | --- |
| `POST` | `/api/claims` | Authenticated | Submit a claim |
| `GET` | `/api/claims/me` | Owner | Get the current user's redacted history |
| `GET` | `/api/claims/user/{userId}` | Owner/moderator | Get redacted user history |
| `GET` | `/api/claims/pending` | Claim moderator | Get private review queue |
| `GET` | `/api/claims/reviewed` | Claim moderator | Get private audit history |
| `GET` | `/api/claims/{claimId}/public-status` | Public | Get evidence-free verification status |
| `PUT` | `/api/claims/{claimId}/review` | Claim moderator | Approve or reject with an expected version |
| `POST` | `/api/claims/{claimId}/appeal` | Owner | Submit the single permitted appeal |
| `POST` | `/api/claims/attachments/upload-url` | Authenticated | Create a restricted upload URL |
| `POST` | `/api/internal/claim-attachments/scan-result` | Attachment scanner | Record the trusted final upload scan result |

Moderation authorization accepts either the `Moderator` role or the `moderation:claims` capability. User identifiers are read from the JWT subject rather than trusted from a submission body.

Every upload URL creates a pending attachment record bound to the authenticated user. Its HMAC covers the object reference, expiry, content type, and permitted content length; the receiving upload gateway must reject a body whose content type or length differs from that signed metadata. A trusted scanning worker with the `attachments:scan` capability records the final malware, content-type, and size decision. Submissions and appeals accept only unique attachment references that belong to the claimant and have reached the approved scan state; missing, foreign, pending, or rejected references are refused.

## Event delivery

Approval updates the claim and inserts a versioned `ClaimApproved` envelope into the outbox in the same MongoDB transaction. The background publisher sends pending messages to Azure Service Bus using the event ID as the broker message ID. User, Token, and Notification services can then update projections or notify the claimant independently. The event contains identifiers and status only—never proof, attachments, appeals, or review notes.

## CI/CD

- `.github/workflows/main.yml` runs restore, formatting verification, build, tests, and artifact publication on pushes to `main`.
- `.github/workflows/pull-request.yml` is triggered only by pull requests and performs restore, formatting verification, build, and tests. It never publishes or deploys.
- `.github/workflows/deploy.yml` is manually dispatched, validates the service, publishes it, and deploys to Azure App Service.

Configure `AZURE_WEBAPP_NAME` as an environment variable and `AZURE_WEBAPP_PUBLISH_PROFILE` as an environment secret for manual deployment.

## Documentation

The authoritative service and integration requirements are maintained in the `docs/mth-docs` submodule, especially:

- `doc/backend/claim-service-instructions.md`
- `doc/backend/shared-library-instructions.md`
- `doc/backend/microservice-integration-instructions.md`
