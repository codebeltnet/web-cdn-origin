# Agent Instructions for Static Content Provider (Codebelt.Cdn.Origin)

Durable guidance for humans and AI agents working in this repository. Read this before changing code.

## Mission

`Codebelt.Cdn.Origin` is a small, production-grade, **read-only static content provider** built on **.NET 10** and **Kestrel**. It serves physical files supplied at runtime and is designed for two deployment scenarios:

1. An **origin server behind a CDN** (for example AWS CloudFront, Cloudflare, Azure Front Door, Google Cloud CDN).
2. A **separately deployed asset host** that keeps static content outside the website or business application.

The architectural value is **segregation of duties, independent deployment, cacheability, origin offloading, and edge distribution**. It is *not* justified by legacy HTTP/1.x domain sharding or browser connection parallelism.

## Non-goals

This application is **not**, and must not become:

- an upload or object-storage service;
- a directory browser;
- a reverse proxy;
- a dynamic website;
- a place for business logic.

Keep it small, focused, framework-first, and operationally robust.

## Engineering rules

- **.NET 10 is required.** Target `net10.0`. Use modern minimal hosting (`WebApplication`), not the legacy `Program`/`Startup` pair.
- **Framework-first.** Prefer ASP.NET Core and the BCL over custom infrastructure. The production project intentionally has **zero third-party package references**. A new dependency requires a clear, written justification in the pull request and must not merely replace straightforward framework or BCL functionality.
- **Static content is read-only.** The content root is treated as a read-only mount. Never add write, upload, or delete paths.
- **HTTP semantics are part of the public contract.** `ETag`, `Last-Modified`, conditional requests (`If-None-Match`, `If-Modified-Since`, `304`), range requests (`Range`, `If-Range`, `206`, `416`), `HEAD` parity, and cache directives are behavioural guarantees. Changing them is a breaking change and requires tests plus a `README.md`/`CHANGELOG.md` update.
- **Security defaults must remain restrictive.** Unknown file types are rejected by default. Directory browsing is disabled. Path traversal outside the content root is prevented. Wildcard CORS origins must never be combined with credentials. Do not relax a default without justification and tests.
- **Behavioural changes require tests and documentation.** Update the functional HTTP matrix and the `README.md` whenever observable behaviour changes.
- **Version discipline.** The version is set once in `Directory.Build.props` (`2.0.0`). Do **not** repeatedly bump it while the work is unmerged.

## Project structure

- `src/Codebelt.Cdn.Origin/` — production source (framework-only).
- `test/Codebelt.Cdn.Origin.Tests/` — unit tests for isolated decision logic (options validation, cache policy, MIME mappings, content-root validation, health check).
- `test/Codebelt.Cdn.Origin.FunctionalTests/` — integration tests that exercise the real ASP.NET Core pipeline against a temporary physical content directory.
- Root `Directory.Build.props` / `Directory.Packages.props` centralize build configuration and package versions.

## Test conventions

- Unit tests live in `*.Tests`; functional tests live in `*.FunctionalTests`. Both use xUnit v3 with the `Microsoft.Testing.Platform` runner and inherit from the `Codebelt.Extensions.Xunit` `Test` base class.
- Test namespaces mirror the system under test (no `.Tests`/`.FunctionalTests` suffix on the namespace); the `RootNamespace` is overridden in each test `.csproj`.
- Do **not** use `InternalsVisibleTo` or `[ExcludeFromCodeCoverage]`. Decision logic that must be unit-tested is exposed as public API.
- Target **100% line and branch coverage for application-owned decision logic**. Use deterministic fixtures, known timestamps, and isolated temporary directories.

## Commands

Run from the repository root.

```bash
# Restore, build (warnings are errors for source projects)
dotnet restore Codebelt.Cdn.Origin.slnx
dotnet build Codebelt.Cdn.Origin.slnx -c Release

# Formatting, code style and analyzers (must produce no changes)
dotnet format Codebelt.Cdn.Origin.slnx --severity info --verify-no-changes

# Tests
dotnet test Codebelt.Cdn.Origin.slnx -c Release

# Tests with coverage (line + branch)
dotnet test Codebelt.Cdn.Origin.slnx -c Release /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Include="[Codebelt.Cdn.Origin]*"

# Container
docker build -t codebeltnet/web-cdn-origin:2.0.0 -f src/Codebelt.Cdn.Origin/Dockerfile .
```
