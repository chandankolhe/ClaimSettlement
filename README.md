# Claim Settlement Decision API

A small ASP.NET Core (.NET 10) backend that automates settlement decisions for
low-value home-insurance claims. It accepts a claim, retrieves policy data from a
remote Policy Admin System, applies the business rules, records the outcome, and
returns a decision.

## Quick start

```powershell
# from the repository root
dotnet test            # restore, build, and run all tests
dotnet run --project ClaimSettlement.Api   # run the API (Swagger UI at /swagger in Development)
```

By default the API listens on `http://localhost:5080`. Send requests with the
samples in [requests.http](requests.http), or via Swagger UI.

### Example request

```http
POST http://localhost:5080/claim-settlement
Content-Type: application/json

{ "policyNumber": "POL-1", "claimAmount": 2500, "propertyAgeYears": 10 }
```

```json
{ "policyNumber": "POL-1", "isApproved": true, "statusReason": "Auto-Settled" }
```

## Configuration

The Policy Admin System base address is supplied through configuration and is
**not** hard-coded. Set it via `appsettings.json`, environment variables, or user
secrets:

| Setting                  | Description                             | Example                     |
| ------------------------ | --------------------------------------- | --------------------------- |
| `PolicyAdmin:BaseAddress`| Base URL of the Policy Admin System     | `https://localhost:5099/`   |

```powershell
# environment-variable form (double underscore = nested key)
$env:PolicyAdmin__BaseAddress = "https://policy-admin.internal/"
```

The configured value is validated at startup (`ValidateOnStart`); the app fails
fast if it is missing. No secrets are committed to the repository.

## Business rules

A claim is **Auto-Settled** (`IsApproved = true`) only when all hold:

1. The policy is active.
2. `ClaimAmount <= CoverageLimit`.
3. `ClaimAmount` is within the automatic-settlement threshold:
   - property aged **30 years or less** → threshold **3000**
   - property aged **over 30 years** → threshold **1000**

Otherwise `IsApproved = false` and the claim is routed for review.
`StatusReason` follows strict precedence:

1. `Policy Inactive`
2. `Coverage Limit Exceeded`
3. `Adjuster Review Required`
4. `Auto-Settled`

Exceeding the automatic-settlement threshold does not reject a claim; it only
makes it ineligible for automated settlement.

## HTTP behaviour

| Outcome                         | Response                                        |
| ------------------------------- | ----------------------------------------------- |
| Valid claim, policy found       | `200 OK` with `SettlementDecision`              |
| Invalid input                   | `400 Bad Request` (validation problem details)  |
| Policy not found                | `404 Not Found` (dependency outcome, not recorded) |
| Policy dependency failure       | `502 Bad Gateway` (generic, no internal detail) |
| Request cancelled / client gone | `499` (no further work, nothing recorded)       |

Validation: `PolicyNumber` must not be blank, `ClaimAmount` must be greater than
zero, and `PropertyAgeYears` must not be negative.

## Design

The solution follows a Clean Architecture / Ports-and-Adapters layout split across
four application projects (plus a test project), so core decision-making stays
isolated from infrastructure and is testable without network or storage.

```
ClaimSettlement.Domain          (pure, no dependencies)
  Models/           ClaimRequest, SettlementDecision  (the fixed HTTP contract)
  Rules/            PolicyDetails, StatusReasons, SettlementRules  (pure, no I/O)

ClaimSettlement.Application      → Domain
  Interfaces/       IPolicyProvider, IDecisionStore, IClaimSettlementService,
                    PolicyProviderException
  Services/         ClaimSettlementService, ClaimEvaluationResult

ClaimSettlement.Infrastructure   → Application, Domain
                    HttpPolicyProvider (+ PolicyAdminOptions / PolicyResponse),
                    InMemoryDecisionStore

ClaimSettlement.Api              → Application, Infrastructure  (ASP.NET Core Web API)
  Controllers/      ClaimSettlementController  (POST /claim-settlement)
  Validations/      ClaimRequestValidator
  Program.cs        composition root / DI wiring

ClaimSettlement.Tests            → all of the above
  Unit/             SettlementRules, ClaimSettlementService, HttpPolicyProvider
  Integration/      ClaimSettlementApiFactory, ClaimSettlementApiTests
```

Project dependencies point inward only: `Domain` depends on nothing, `Application`
depends on `Domain`, `Infrastructure` and `Api` depend on `Application`, and nothing
depends on `Api`. This enforces the boundaries at compile time.

Key decisions:

- **Pure rules.** `SettlementRules.Evaluate` is a static, side-effect-free function.
  All worked examples from the brief are covered by unit tests.
- **Ports and adapters.** `IPolicyProvider` and `IDecisionStore` keep transport and
  storage behind interfaces. `HttpPolicyProvider` maps transport outcomes to domain
  outcomes: `200 → details`, `404 → null (not found)`, and `5xx / invalid body /
  network error → PolicyProviderException`.
- **Not-found vs. decision.** The service returns a `ClaimEvaluationResult` that
  distinguishes "policy not found" (a dependency outcome — nothing recorded) from a
  produced decision. Only produced decisions are recorded.
- **Typed HttpClient** via `IHttpClientFactory` for pooled handlers and a natural
  seam for resilience policies.
- **Cancellation** is propagated end-to-end; the token is checked before work and
  passed to every async downstream call.
- **No sensitive logging.** The policy number (potential PII) and connection detail
  are not logged; dependency failures return a generic message to the caller.

## Testing

```powershell
dotnet test
```

- **Unit** — rule precedence, the brief's five worked examples, the older-property
  threshold boundary, recording behaviour, not-found handling, cancellation, and
  `HttpPolicyProvider` transport mapping (success / 404 / 5xx / invalid JSON / network).
- **Integration** — the full HTTP pipeline via `WebApplicationFactory` with an
  in-process policy stub (no network): auto-settled path, non-approval path,
  validation `400`, not-found `404`, and dependency `502` without leaking detail.

All tests are deterministic and require no live external system.

## The Policy Admin System

The external service is **not** built here. It is simulated in tests with an
in-process `IPolicyProvider` stub and a fake `HttpMessageHandler`. For manual runs,
point `PolicyAdmin:BaseAddress` at any stub that implements
`GET /policies/{policyNumber}` returning `{ "coverageLimit": 10000, "isActive": true }`
(e.g. a tool such as WireMock, json-server, or a tiny local endpoint).

## Assumptions and known limitations

- A claim amount **equal to** the coverage limit or threshold is within the limit
  (`<=`), per the brief.
- Policy-not-found is treated as a dependency outcome and returns `404`; no decision
  is recorded.
- Decision recording is in-memory and not durable; it resets on restart.
- Minimal validation only, as specified. No authentication/authorization.
- Resilience (retries, circuit breaker, timeouts) and observability (tracing,
  metrics) are intentionally out of scope — see below.

## Trade-offs

- **Shared integration-test fixture.** The integration tests share a single
  `WebApplicationFactory` (`IClassFixture`) and mutate one in-process
  `StubPolicyProvider` (`Policy` / `ExceptionToThrow`). This keeps the tests fast and
  simple and is safe because xUnit runs tests within a class sequentially; the
  dependency-failure test resets the shared state. It is not safe for cross-class
  parallelism — a per-test factory or a fresh stub per test would be the next step.
- **`499` for client cancellation.** When the request is cancelled the controller
  returns `499 Client Closed Request`, a non-standard (nginx-style) code chosen
  because no standard 4xx cleanly expresses "the client went away". It is never a
  decision outcome and nothing is recorded.
- **Multi-project layout for a small slice.** The solution is split into four
  projects (plus tests) to make the architectural boundaries explicit and
  compile-time enforced. For a slice this size a single project with folders would
  also be defensible; the trade-off is a little more ceremony for clearer intent.

## Next steps for production (discussion)

- **Resilience:** add `Microsoft.Extensions.Http.Resilience` (retry with jitter,
  circuit breaker, timeout) to the typed `HttpClient` for transient downstream faults.
- **Observability:** structured logging with correlation IDs, OpenTelemetry traces
  and metrics around the dependency call and decision outcomes, and health checks.
- **Durability:** replace `InMemoryDecisionStore` with a real store behind the same
  `IDecisionStore` interface.
- **Contract hardening:** stronger problem-details responses and richer validation.
```
