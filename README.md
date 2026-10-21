# Conway's Game of Life API

A RESTful API for [Conway's Game of Life](https://en.wikipedia.org/wiki/Conway%27s_Game_of_Life), built on **.NET 8** with ASP.NET Core controllers, EF Core and SQLite.

- Upload a board and get back an id
- Get the next generation, or the state N generations ahead
- Get the final state: a still life, extinction, or the first state of an oscillation, or a `422` if the board does not settle within a generation budget
- Boards are stored in SQLite, so they survive restarts and crashes

## Quick start

Prerequisites: the .NET 8 SDK. `global.json` pins the 8.0 feature band.

```bash
dotnet run --project src/GameOfLife.Api
```

The API listens on `http://localhost:5080`. On startup it creates `data/gameoflife.db` (relative to the working directory) and applies EF Core migrations.

```bash
dotnet test
```

## API

| Method | Route | Description | Success |
|---|---|---|---|
| `POST` | `/api/boards` | Upload a board | `201` + `Location` |
| `GET` | `/api/boards/{id}` | The uploaded board (generation 0) | `200` |
| `GET` | `/api/boards/{id}/next` | Generation 1 | `200` |
| `GET` | `/api/boards/{id}/generations/{n}` | Generation `n` (`0 ≤ n ≤ MaxGenerationsAhead`) | `200` |
| `GET` | `/api/boards/{id}/final?maxIterations=` | The final state | `200` |

### Upload

```http
POST /api/boards
Content-Type: application/json

{ "cells": [[false, true, false],
            [false, true, false],
            [false, true, false]] }
```

```http
HTTP/1.1 201 Created
Location: http://localhost:5080/api/boards/4a212384-aee5-4f40-9dd1-43c7f0f949e3

{ "id": "4a212384-aee5-4f40-9dd1-43c7f0f949e3" }
```

`cells` is a list of rows, where `true` means alive. The grid must be non-empty and rectangular, and no larger than `MaxBoardDimension` in either direction.

### Board state (`/{id}`, `/next`, `/generations/{n}`)

```json
{ "id": "4a21…", "generation": 1, "rows": 3, "columns": 3,
  "cells": [[false,false,false],[true,true,true],[false,false,false]] }
```

### Final state (`/final`)

```json
{ "id": "4a21…", "generation": 0, "outcome": "Oscillating", "period": 2,
  "rows": 3, "columns": 3, "cells": [[false,true,false],[false,true,false],[false,true,false]] }
```

| `outcome` | Meaning | `period` |
|---|---|---|
| `Stable` | A still life: the board no longer changes | 1 |
| `Extinct` | Every cell has died | 1 |
| `Oscillating` | The board cycles through `period` states | > 1 |

`generation` is the first generation of the final cycle, and `cells` is the board at that generation. The board counts as settled if its state repeats within `maxIterations` generations: `generation + period ≤ maxIterations`. `maxIterations` defaults to `DefaultMaxIterations` and may be raised up to `MaxIterationsLimit`. If the board has not settled within that budget, the API responds `422`.

### Errors

Every error is an [RFC 7807](https://www.rfc-editor.org/rfc/rfc7807) `application/problem+json` response with a `traceId`. Application errors also carry a stable, machine-readable `errorCode`:

```json
{ "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "Request could not be processed.", "status": 422,
  "detail": "The board did not reach a stable or repeating state within 2 generations.",
  "errorCode": "board.final_state_not_reached", "traceId": "00-2f77…" }
```

| Status | When | `errorCode` |
|---|---|---|
| 400 | Malformed or non-rectangular board, or a value out of range | `board.too_large`, `board.generation_out_of_range`, `board.max_iterations_out_of_range` (or none for model-binding errors) |
| 404 | Unknown board id (or an id that is not a GUID) | `board.not_found` |
| 413 | Request body over `MaxRequestBodyBytes` | |
| 422 | No final state within `maxIterations` | `board.final_state_not_reached` |
| 429 | Rate limit exceeded (with a `Retry-After` header) | |
| 503 | Request exceeded `RequestTimeoutSeconds` | |

## Configuration

`appsettings.json`, or environment variables such as `GameOfLife__MaxBoardDimension=500`. Options are validated at startup, so a misconfiguration fails fast.

| Key | Default | Purpose |
|---|---|---|
| `ConnectionStrings:GameOfLife` | `Data Source=data/gameoflife.db` | SQLite database |
| `GameOfLife:MaxBoardDimension` | 1000 | Maximum rows and maximum columns |
| `GameOfLife:MaxGenerationsAhead` | 10000 | Maximum `n` for `/generations/{n}` |
| `GameOfLife:DefaultMaxIterations` | 1000 | Final-state budget when `maxIterations` is omitted |
| `GameOfLife:MaxIterationsLimit` | 10000 | Largest `maxIterations` a client may request |
| `ApiLimits:MaxRequestBodyBytes` | 8 MiB | Kestrel request body limit |
| `ApiLimits:RequestTimeoutSeconds` | 30 | Requests running longer are cancelled (`503`) |
| `ApiLimits:RateLimitPermitLimit` | 100 | Requests per client IP in each window |
| `ApiLimits:RateLimitWindowSeconds` | 60 | Fixed rate-limit window |

## Architecture

```
src/
  GameOfLife.Domain          Pure logic, no dependencies: Board, LifeRules, GameOfLifeEngine, CycleDetector
  GameOfLife.Application     Use cases (BoardService), IBoardRepository, Result/Error, options + validation
  GameOfLife.Infrastructure  EF Core + SQLite: DbContext, repository, bit-packing, migrations
  GameOfLife.Api             Controllers, contracts, error mapping, rate limiting, timeouts, composition root
tests/
  GameOfLife.Domain.Tests           Rules, engine, Brent's algorithm (with a randomized check against a naive detector)
  GameOfLife.Application.Tests      BoardService against an in-memory repository; options validation
  GameOfLife.Infrastructure.Tests   Bit-packing; repository against a real SQLite file
  GameOfLife.Api.IntegrationTests   End to end through WebApplicationFactory: endpoints, errors, restart, rate limiting
```

Dependencies point inward: Api → Application → Domain, and Infrastructure → Application. The domain is plain C# with no framework references, so the core logic is trivial to unit test.

## Design decisions

**Stored boards are immutable; reads are pure computations.** A stored board is the uploaded seed. `next`, `generations/{n}` and `final` compute from it on demand, so they are safe, idempotent `GET`s: the same request always returns the same answer, and responses could be cached by HTTP intermediaries. The alternative, a `POST /next` that advances a persisted "current" generation, makes the API stateful and order-dependent under concurrent clients.

**Bounded grid; cells beyond the edge are dead.** This matches "upload a 2D grid" most directly. A consequence worth knowing: a glider eventually hits a corner and collapses into a block, which the final-state endpoint reports as `Stable`. Topology is isolated in `GameOfLifeEngine.NextGeneration`, so a toroidal engine would be a new `IGameOfLifeEngine` implementation.

**Final state = still life, extinction, or a cycle, found with Brent's algorithm.** Oscillators such as blinkers never stop changing, but they have clearly concluded, so a cycle of any period counts as final. The response reports `outcome` and `period` so clients can tell the cases apart.

Brent's algorithm keeps at most two boards in memory and compares them with full equality, so there are no hash collisions to worry about. The main alternative is a hash per generation in a dictionary, with verification on a match. That runs a single pass (about `μ + λ` generations) but needs collision handling. Brent trades CPU for memory: the worst case is about 5 × `maxIterations` generations (the bound is derived in `CycleDetector`'s remarks). Storing every board would need up to 1M cells × 10k generations ≈ 10 GB at the configured limits.

**`Result<T>` for expected failures, not exceptions.** "Not found", "out of range" and "didn't settle" are normal outcomes, so the service returns `Result<T>`, and the controller maps each `Error` to a problem response with a stable `errorCode`. This keeps exceptions for genuinely unexpected failures. It also matters in .NET 8, where `UseExceptionHandler` logs every exception it sees at Error level, which would flood the logs with 404s.

**Two layers of validation.** The API contract validates the request shape (present, non-empty, rectangular) and returns a standard `ValidationProblemDetails`. The application layer validates business limits (dimensions, generation range, iteration budget) because they apply to any caller. `Board` also guards its own invariants, as defense in depth.

**SQLite + EF Core, cells bit-packed into a BLOB.** SQLite gives transactional durability with no external infrastructure. EF Core plus a repository interface make switching to PostgreSQL or SQL Server a provider and connection-string change. Cells are packed one bit per cell (row-major, least significant bit first), so a 1000×1000 board takes 125 KB instead of about 6 MB of JSON. Migrations are checked in and applied at startup.

**Production protections.**
- **Kestrel body limit:** caps upload size before deserialization (413).
- **Per-IP fixed-window rate limiting:** the compute endpoints are CPU-bound (429 with `Retry-After`).
- **Request timeout middleware:** cancels runaway computations. The engine checks the `CancellationToken` every generation, so a timeout or a client disconnect actually stops the work (503).
- **Configuration limits:** board size, generation count and iteration budget, all validated at startup.
- **Problem details everywhere:** including framework-generated 404s and 429s, via `UseStatusCodePages`.

**Extensibility.** `LifeRules` expresses any Life-like rule in B/S notation (`LifeRules.Conway` is B3/S23, and HighLife would be `new LifeRules([3, 6], [2, 3])`), so supporting other rule sets would only need an API parameter and a stored column. The engine, repository and service are each behind an interface.

## Measured performance

On the development machine, one generation of a 1000×1000 board takes about 70 ms end to end. The naive engine (count up to 9 cells per cell) is deliberately simple and easy to verify.

## Trade-offs and future improvements

- **Caching:** `final` and large `n` are deterministic and expensive. Caching or persisting results keyed by `(boardId, generation)` would make repeated requests O(1). It was left out to keep the first version simple and obviously correct.
- **Faster engines:** bit-parallel (64 cells per `ulong`) or HashLife implementations would speed up large boards and long horizons by orders of magnitude, behind the same `IGameOfLifeEngine` interface.
- **Async jobs for long computations:** instead of failing at the timeout, `final` could return `202 Accepted` with a job resource to poll.
- **Multi-instance deployments:** apply migrations with a migration bundle in the deployment pipeline rather than at startup. Move to a server database. Distribute rate limiting, for example with Redis. Configure forwarded headers so rate limiting sees real client IPs behind a proxy.
- **Operational extras not included here:** health checks (`/health/live`, `/health/ready`), OpenAPI/Swagger, a Dockerfile, structured log shipping, and OpenTelemetry metrics.
- **Relative SQLite path:** the default path resolves against the working directory. In a container you would point it at a mounted volume, e.g. `ConnectionStrings__GameOfLife="Data Source=/var/data/gameoflife.db"`.
- **Request timeouts** are not enforced while a debugger is attached (ASP.NET Core behavior).
