# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2026-08-10

This is a **major** release representing a deliberate modernization of the Static Content Provider. The application keeps its focus — a small, read-only, framework-first static content origin for CDN and segregated asset-host scenarios — but the implementation, configuration contract, container, and DevOps are fully modernized for .NET 10 and contemporary cloud-native patterns.

### Added

- Modern minimal hosting on .NET 10 (`WebApplication`) replacing the legacy `Program`/`Startup` model,
- Strongly typed, validated configuration model (`CdnOrigin` section) covering content root, default documents, cache profiles, CORS, compression, content-type mappings, and health checks. Invalid configuration fails fast at startup,
- Two explicit cache profiles: **revalidate** (mutable URLs) and **immutable** (versioned / content-addressed URLs selected by configured path prefixes),
- Framework CORS with configurable origins, exposed headers, `Cross-Origin-Resource-Policy`, and `Timing-Allow-Origin`. Wildcard origins can never be combined with credentials,
- Optional response compression (Brotli/Gzip) restricted to compressible content types, off by default because edge compression is preferred behind a CDN,
- Operational endpoints `/health/live` and `/health/ready` (readiness verifies the content root is present and readable). Health responses are `Cache-Control: no-store`,
- `405 Method Not Allowed` with an `Allow` header for unsupported methods against existing files,
- Structured startup logging of the effective, non-sensitive configuration,
- Case-insensitive path lookup via `PortablePhysicalFileProvider` from the Codebelt ecosystem, so asset URLs work consistently on case-sensitive and case-insensitive file systems. Symlink and junction resolution prevents traversal-based bypasses,
- Cache prefix matching is now case-insensitive for robustness on case-insensitive filesystems,
- Hardened container: .NET 10 runtime, non-root user, non-privileged port `8080`, read-only-root-filesystem compatible, minimal image,
- Multi-architecture container support: X64 and ARM64 images built and published to registries,
- Reusable GitHub composite actions for container lifecycle: `docker-build`, `docker-load`, `docker-save`, `docker-push`, `docker-login`, `docker-tag-semver`, `docker-tag-trunkver`, and supply-chain security actions (`container-sbom`, `container-attest-sbom`, `container-attest-provenance`),
- Docker Hub promotion workflow with gated Production environment approval and SemVer/TrunkVer tagging strategy,
- Multi-OS test matrix validating across Ubuntu 24.04, Windows 2025, and macOS 26 in both Debug and Release configurations,
- Test environment configuration (`testEnvironments.json`) for consistent Docker-based test execution,
- Comprehensive unit and functional test suite using xUnit v3 with `Codebelt.Extensions.Xunit` patterns,
- Governance and engineering documentation: `AGENTS.md` defining build rules, test conventions, and version discipline; `.editorconfig` for project-wide consistency; centralized `Directory.Build.props` and `Directory.Packages.props`; and complete CI pipeline with artifact management.

### Changed

- **Configuration is now hierarchical and strongly typed.** The flat `1.x` environment variables are replaced by the `CdnOrigin` section (still overridable via environment variables). See the migration table in `README.md`,
- Cache durations are expressed as standard `TimeSpan` values instead of a numeric value plus a separate time-unit variable,
- `ETag` and `Last-Modified` are now produced by the ASP.NET Core static file middleware from file identity and modification metadata. The server no longer reads or hashes file contents to build an `ETag`,
- The default container port is `8080` (was `80`) and the process runs as a non-root user,
- Application startup now follows `MinimalWebProgram` bootstrap pattern for framework-first initialization,
- Functional tests migrated to `Codebelt.Extensions.Xunit` patterns with modern `WebApplication` test server and `Microsoft.Testing.Platform` runner.

### Removed

- **Custom MD5 content hashing for `ETag`** (`ETAG_BYTESTOREAD` and the `StreamExtensions` helper). The framework `ETag` replaces it and removes per-request full-file reads,
- **`no-transform`** is no longer emitted by default, so a CDN may legitimately transform or optimize responses,
- **`Expires`** header. `Cache-Control: max-age` is authoritative; the redundant `Expires` header is gone,
- **Server-side response caching** (`AddResponseCaching`). A CDN origin should not cache its own responses; the CDN and HTTP clients handle caching,
- **Cuemon dependencies** and the Visual Studio container-tooling package. The production project now references only the ASP.NET Core shared framework,
- **`ServeUnknownFileTypes = true`.** Unknown file types are now rejected by default; add explicit MIME mappings through configuration to serve additional types,
- Legacy `Startup` class and associated extension methods. Modern minimal hosting replaces them.

### Fixed

- Symbolic link resolution in `ContentRootValidator` now recursively follows intermediate symlinks instead of stopping at the first target, preventing traversal bypasses when symlinks chain through multiple levels,
- CI workflow now runs macOS tests on pull request events (previously only on manual dispatch) for complete multi-OS coverage in validation gates,
- Test coverage expanded to include chained symbolic link scenarios and root directory edge cases.

### Migration

See the "Migration from 1.4.0 to 2.0.0" section of `README.md` for the full `1.x` → `2.0.0` configuration mapping and behavioural notes.

## [1.4.0] - 2022-11-22

This is a **minor** release focused on runtime modernization and dependency updates.

### Added

- Support for .NET 7.

### Changed

- Dependency updates and lifecycle maintenance.

## [1.3.0] - 2021-12-11

This is a **minor** release bringing .NET 6 support.

### Changed

- Updated to .NET 6 runtime.

## [1.2.0] - 2021-05-20

This is a **minor** release adding response compression.

### Added

- Default implementation of response compression (Gzip/Brotli) for compressible content types.

## [1.1.5] - 2021-05-02

This is a **patch** release with configuration and CORS improvements.

### Changed

- CORS support refactored; `Access-Control-Allow-Origin` now added as part of `OnPrepareResponse` for better static file compatibility,
- Project restructured, moving files to repository root for better discoverability.

### Added

- License and readme files at repository root.

## [1.1.0] - 2021-04-30

This is a **minor** release focused on performance tuning and case-sensitivity fixes.

### Added

- Configurable ETag hashing with `ETAG_BYTESTOREAD` to control how many bytes are read per file,
- Opt-in MD5 hashing mode for faster ETag generation.

### Fixed

- Case-sensitivity bug on Windows and other case-insensitive filesystems. Static file serving now uses `CaseInsensitivePhysicalFileProvider` for cross-platform compatibility.

## [1.0.0] - 2021-04-29

This is the initial release of the Static Content Provider.

### Added

- Environment-variable-only configuration model for container-first operation,
- Support for default files (e.g., `index.html`, `default.htm`),
- Custom cache control headers (`Cache-Control` and optional `Expires`),
- Content-type mappings for common static asset types,
- Content-based `ETag` generation with strong validation,
- Last-Modified header support for HTTP conditional requests,
- Basic license and documentation.

[2.0.0]: https://github.com/codebeltnet/web-cdn-origin/compare/1.4.0...2.0.0
[1.4.0]: https://github.com/codebeltnet/web-cdn-origin/compare/1.3.0...1.4.0
[1.3.0]: https://github.com/codebeltnet/web-cdn-origin/compare/1.2.0...1.3.0
[1.2.0]: https://github.com/codebeltnet/web-cdn-origin/compare/1.1.5...1.2.0
[1.1.5]: https://github.com/codebeltnet/web-cdn-origin/compare/1.1.0...1.1.5
[1.1.0]: https://github.com/codebeltnet/web-cdn-origin/compare/1.0.0...1.1.0
[1.0.0]: https://github.com/codebeltnet/web-cdn-origin/releases/tag/1.0.0
