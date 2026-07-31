# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0]

A deliberate major-version modernization of the Static Content Provider. The application keeps its focus — a small, read-only, framework-first static content origin for CDN and segregated asset-host scenarios — but the implementation, configuration contract, and container are modernized.

### Added

- Modern minimal hosting on .NET 10 (`WebApplication`) replacing the legacy `Program`/`Startup` model.
- Strongly typed, validated configuration model (`CdnOrigin` section) covering content root, default documents, cache profiles, CORS, compression, content-type mappings, and health checks. Invalid configuration fails fast at startup.
- Two explicit cache profiles: **revalidate** (mutable URLs) and **immutable** (versioned / content-addressed URLs selected by configured path prefixes).
- Framework CORS with configurable origins, exposed headers, `Cross-Origin-Resource-Policy`, and `Timing-Allow-Origin`. Wildcard origins can never be combined with credentials.
- Optional response compression (Brotli/Gzip) restricted to compressible content types, off by default because edge compression is preferred behind a CDN.
- Operational endpoints `/health/live` and `/health/ready` (readiness verifies the content root is present and readable). Health responses are `Cache-Control: no-store`.
- `405 Method Not Allowed` with an `Allow` header for unsupported methods against existing files.
- Structured startup logging of the effective, non-sensitive configuration.
- Hardened container: .NET 10 runtime, non-root user, non-privileged port `8080`, read-only-root-filesystem friendly, minimal image.
- `AGENTS.md`, `.editorconfig`, centralized build/package configuration, CI pipeline, and a comprehensive unit + functional test suite.

### Changed

- **Configuration is now hierarchical and strongly typed.** The flat `1.x` environment variables are replaced by the `CdnOrigin` section (still overridable via environment variables). See the migration table in `README.md`.
- Cache durations are expressed as standard `TimeSpan` values instead of a numeric value plus a separate time-unit variable.
- `ETag` and `Last-Modified` are now produced by the ASP.NET Core static file middleware from file identity and modification metadata. The server no longer reads or hashes file contents to build an `ETag`.
- The default container port is `8080` (was `80`) and the process runs as a non-root user.

### Removed

- **Custom MD5 content hashing for `ETag`** (`ETAG_BYTESTOREAD` and the `StreamExtensions` helper). The framework `ETag` replaces it and removes per-request full-file reads.
- **Case-insensitive path emulation** (`CaseInsensitivePhysicalFileProvider`). File lookups now follow normal filesystem case semantics. This custom resolver had correctness and security concerns; rely on correct casing or a case-insensitive filesystem.
- **`no-transform`** is no longer emitted by default, so a CDN may legitimately transform or optimize responses.
- **`Expires`** header. `Cache-Control: max-age` is authoritative; the redundant `Expires` header is gone.
- **Server-side response caching** (`AddResponseCaching`). A CDN origin should not cache its own responses; the CDN and HTTP clients handle caching.
- **Cuemon dependencies** and the Visual Studio container-tooling package. The production project now references only the ASP.NET Core shared framework.
- **`ServeUnknownFileTypes = true`.** Unknown file types are now rejected by default; add explicit MIME mappings through configuration to serve additional types.

### Migration

See the "Migration from 1.4.0 to 2.0.0" section of `README.md` for the full `1.x` → `2.0.0` configuration mapping and behavioural notes.
