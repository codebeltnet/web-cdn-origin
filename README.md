# Static Content Provider (`Codebelt.Cdn.Origin`)

A small, production-grade, **read-only static content provider** built on **.NET 10** and **Kestrel**. It serves physical files supplied at runtime and is designed to sit either behind a CDN as an **origin**, or as a **separately deployed asset host** that keeps static content out of your website or business application.

> The provider is deliberately minimal and framework-first. The production assembly references only the ASP.NET Core shared framework — no third-party packages.

## Contents

- [What it is](#what-it-is)
- [Deployment scenarios](#deployment-scenarios)
- [Architecture and request flow](#architecture-and-request-flow)
- [Supported HTTP capabilities](#supported-http-capabilities)
- [Cache-policy modes](#cache-policy-modes)
- [Configuration reference](#configuration-reference)
- [Running locally](#running-locally)
- [Docker](#docker)
- [CI and container promotion](#ci-and-container-promotion)
- [Kubernetes](#kubernetes)
- [AWS CloudFront origin](#aws-cloudfront-origin)
- [Security considerations](#security-considerations)
- [Migration from 1.4.0 to 2.0.0](#migration-from-140-to-200)
- [Local verification](#local-verification)

## What it is

`Codebelt.Cdn.Origin` serves files from a configured **content root** over HTTP with correct, standards-compliant caching and conditional-request semantics. It does exactly one job — serve static content safely — and nothing else.

It is **not**:

- an upload or object-storage service;
- a directory browser;
- a reverse proxy;
- a dynamic website;
- a place for business logic.

## Deployment scenarios

### 1. CDN origin

The provider runs as the **origin** behind a CDN such as AWS CloudFront, Cloudflare, Azure Front Door, or Google Cloud CDN. The CDN caches and distributes the content at the edge; the origin only serves cache misses and revalidations.

### 2. Segregated asset host

The provider hosts public assets (JavaScript, CSS, fonts, images) on a host that is **separate** from the website or business application, even without a CDN in front.

### Why the separation exists

The value of hosting static content separately is **architectural**, not a browser-connection trick:

- **Segregation of duties** — static delivery is isolated from application logic and its failure modes.
- **Independent deployment and scaling** — assets ship and scale on their own cadence.
- **Cacheability** — a dedicated, cache-friendly surface with explicit, correct cache headers.
- **Origin offloading** — the CDN absorbs the vast majority of requests; the origin stays small and cheap.
- **Edge distribution** — content is served close to users through the CDN.

> Note: On modern HTTP/2 and HTTP/3, serving assets from a separate domain does **not** improve performance through extra browser connection parallelism (that was an HTTP/1.x "domain sharding" technique and is now usually counter-productive because it prevents connection coalescing). The benefits above are about architecture, operability, and edge caching — not connection count.

## Architecture and request flow

```
Client ──▶ CDN (edge cache) ──▶ Codebelt.Cdn.Origin (Kestrel) ──▶ Content root (read-only files)
```

The ASP.NET Core pipeline, in order:

1. **Response compression** (optional, off by default) — Brotli/Gzip for compressible types only.
2. **CORS** (optional, on by default) — applies the configured policy and answers preflight requests.
3. **Health endpoints** — `/health/live` and `/health/ready` (mapped when enabled).
4. **Default documents** — rewrites a directory request to a default document when one exists.
5. **Static files** — serves `GET`/`HEAD` for existing files with the correct content type, validators, and cache headers; unknown file types are rejected.
6. **Terminal handler** — returns `404 Not Found`, or `405 Method Not Allowed` with an `Allow` header for an unsupported method against an existing file.

The content root is validated at startup (exists, is a directory, is readable, and does not overlap the application directory). Invalid configuration fails fast.

## Supported HTTP capabilities

All of the following are provided by the framework static-file middleware and are part of the provider's public contract:

| Capability | Behaviour |
| --- | --- |
| Methods | `GET` and `HEAD` (with correct `HEAD` parity — headers, no body) |
| `Content-Type` | Explicit, safe MIME mapping; unknown extensions rejected by default |
| `Content-Length` | Set for full and `HEAD` responses |
| `Last-Modified` | From file modification metadata |
| `ETag` | Derived from file identity and modification metadata — **the file is never read or hashed per request** |
| `If-None-Match` / `If-Modified-Since` | Conditional requests return `304 Not Modified` |
| `Range` / `If-Range` | Byte-range requests return `206 Partial Content`; `416 Range Not Satisfiable` for unsatisfiable ranges |
| Default documents | Configurable; `default.htm`, `default.html`, `index.htm`, `index.html` by default |
| Missing files | `404 Not Found` |
| Unsupported methods | `405 Method Not Allowed` with `Allow: GET, HEAD, OPTIONS` |
| Directory browsing | Disabled |
| Path casing | Case-insensitive lookup across case-sensitive and case-insensitive file systems |
| Path traversal | Prevented — access is confined to the content root |

## Cache-policy modes

Rather than emitting one set of directives for every file, the provider supports two explicit cache **profiles**:

- **Revalidate** — for mutable URLs. Default: `public, max-age=12h, s-maxage=7d, must-revalidate`.
- **Immutable** — for versioned or content-addressed URLs. Default: `public, max-age=365d, immutable`.

A request uses the **immutable** profile when its path starts with one of the configured `Cache:ImmutablePathPrefixes` (for example `/assets/`), matched case-insensitively; otherwise it uses the **revalidate** profile.

Each profile exposes the relevant `Cache-Control` directives: `public`/`private`, `max-age`, `s-maxage`, `must-revalidate`, `no-cache`, `no-store`, `immutable`, `stale-while-revalidate`, `stale-if-error`, and `no-transform`. Contradictory combinations are rejected at startup, and `no-transform` is **not** emitted by default so a CDN may legitimately transform or optimize responses.

## Configuration reference

Configuration binds from the `CdnOrigin` section (via `appsettings.json`) and can be overridden with environment variables using the `__` (double underscore) separator, for example `CdnOrigin__ContentRoot`.

### Static content

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `CdnOrigin:ContentRoot` | path | `/cdnroot` | Directory of physical files to serve. Must exist at startup. |
| `CdnOrigin:DefaultDocuments` | string[] | `default.htm`, `default.html`, `index.htm`, `index.html` | Default documents, tried in order. Leave empty to use the standard defaults. |

### Cache

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `CdnOrigin:Cache:ImmutablePathPrefixes` | string[] | *(empty)* | Request-path prefixes served with the immutable profile, matched case-insensitively. |
| `CdnOrigin:Cache:Revalidate` | profile | `public`, `12:00:00`, `7.00:00:00`, `must-revalidate` | Profile for mutable URLs. |
| `CdnOrigin:Cache:Immutable` | profile | `public`, `365.00:00:00`, `immutable` | Profile for versioned/content-addressed URLs. |

Each profile supports: `Public` (bool), `MaxAge` (`TimeSpan`), `SharedMaxAge` (`TimeSpan`), `MustRevalidate` (bool), `NoCache` (bool), `NoStore` (bool), `Immutable` (bool), `StaleWhileRevalidate` (`TimeSpan`), `StaleIfError` (`TimeSpan`), `NoTransform` (bool). Durations use standard `TimeSpan` strings (`hh:mm:ss` or `d.hh:mm:ss`).

### CORS

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `CdnOrigin:Cors:Enabled` | bool | `true` | Enable CORS handling. |
| `CdnOrigin:Cors:AllowedOrigins` | string[] | *(empty = public)* | Allowed origins. Empty or `*` means public (any origin). |
| `CdnOrigin:Cors:ExposedHeaders` | string[] | *(empty)* | `Access-Control-Expose-Headers` values. |
| `CdnOrigin:Cors:AllowCredentials` | bool | `false` | Allow credentialed requests. Cannot be combined with a wildcard/public origin. |
| `CdnOrigin:Cors:CrossOriginResourcePolicy` | string | `cross-origin` | `Cross-Origin-Resource-Policy` header value; empty to omit. |
| `CdnOrigin:Cors:TimingAllowOrigin` | bool | `false` | Emit a `Timing-Allow-Origin` header. |

### Compression

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `CdnOrigin:Compression:Enabled` | bool | `false` | Enable origin compression. Edge compression is usually preferred behind a CDN. |
| `CdnOrigin:Compression:EnableForHttps` | bool | `true` | Compress HTTPS responses. |
| `CdnOrigin:Compression:AdditionalMimeTypes` | string[] | *(empty)* | Extra compressible MIME types beyond the built-in defaults. |

Already-compressed formats (images, video, WOFF2 fonts) are never compressed, and `Vary: Accept-Encoding` is set when compression applies.

### Content types

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `CdnOrigin:ContentTypes:ServeUnknownFileTypes` | bool | `false` | Serve files with an unknown extension. Off by default. |
| `CdnOrigin:ContentTypes:DefaultContentType` | string | *(none)* | Required when `ServeUnknownFileTypes` is `true`. |
| `CdnOrigin:ContentTypes:Mappings` | map | *(empty)* | Additional extension→MIME mappings, e.g. `CdnOrigin:ContentTypes:Mappings:.foo = application/x-foo`. |

### Health

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `CdnOrigin:Health:Enabled` | bool | `true` | Map the health endpoints. |
| `CdnOrigin:Health:LivePath` | path | `/health/live` | Liveness endpoint (process is up). |
| `CdnOrigin:Health:ReadyPath` | path | `/health/ready` | Readiness endpoint (content root available and readable). |

Health responses are always `Cache-Control: no-store` so a CDN cannot cache them.

## Running locally

```bash
dotnet run --project src/Codebelt.Cdn.Origin/Codebelt.Cdn.Origin.csproj
```

Point the content root at a local directory:

```bash
# bash
CdnOrigin__ContentRoot=/path/to/content dotnet run --project src/Codebelt.Cdn.Origin/Codebelt.Cdn.Origin.csproj
```

```powershell
# PowerShell
$env:CdnOrigin__ContentRoot = "C:\path\to\content"; dotnet run --project src/Codebelt.Cdn.Origin/Codebelt.Cdn.Origin.csproj
```

## Docker

The image runs as a non-root user on the conventional non-privileged port `8080`, supports a read-only root filesystem, and treats `/cdnroot` as a read-only content mount.

Build from the repository root (so central build configuration is available):

```bash
docker build -t codebeltnet/web-cdn-origin:2.0.0 -f src/Codebelt.Cdn.Origin/Dockerfile .
```

Mount content at runtime:

```bash
docker run -d --name cdn-origin \
  --read-only \
  -p 8080:8080 \
  -v /path/to/content:/cdnroot:ro \
  codebeltnet/web-cdn-origin:2.0.0
```

Or bake content into a derived image:

```dockerfile
FROM codebeltnet/web-cdn-origin:2.0.0
COPY ./cdnroot /cdnroot
```

## CI and container promotion

Pull requests run the Debug/Release build and Linux, Windows, and macOS test matrices. They also build the Dockerfile once on Linux/amd64, generate an SPDX JSON SBOM, save the image with `docker save`, and upload the tarball as an artifact. No registry credentials or push permissions are available to pull-request builds. Manually dispatched runs keep macOS optional through `run_mac_tests` because of its additional cost and runtime.

The saved image receives two tags:

- SemVer from `needs.build.outputs.version`, with one leading `v` removed. For example, `v2.0.0` becomes `2.0.0` for compatibility with the existing Docker Hub `1.4.0` naming.
- A [TrunkVer](https://trunkver.org/) generated once during the container build and reused for both registries.

Use a manually dispatched run with `publish_image: true` to publish the saved tarball to the `Staging` environment. The default staging repository is `jcr.codebelt.net/geekle/web-cdn-origin`; override `container_repository` when needed. Configure the exact `REGISTRY_USERNAME` and `REGISTRY_PASSWORD` secret names in the `Staging` environment. The values must be accepted by the selected container registry, so the same generic secret names work when `container_repository` is overridden.

To keep the exact same run artifact, set both `publish_image: true` and `promote_dockerhub: true` on the manual dispatch. The workflow publishes to Staging and then pauses at the protected `Production` environment, allowing you to verify JCR before approving Docker Hub publication. Configure required reviewers for `Production` and add the exact `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` secret names to that environment. `DOCKERHUB_TOKEN` must be a Docker Hub access token with permission to push to the target namespace; a normal account password is not expected. The default Docker Hub repository is `codebeltnet/web-cdn-origin`; override `dockerhub_repository` when needed. A run with `promote_dockerhub: false` intentionally stops after Staging; enabling it on a later run creates a new build artifact.

This is a promotion of one immutable build artifact: the Docker Hub job downloads the same `docker save` tarball, uses `docker load`, retags it for Docker Hub, and pushes both tags. It does not rebuild or pull a new image. The build job carries the image and SBOM artifact names to downstream jobs, so rerunning only a failed publish or attestation job reuses the artifact from the successful build attempt. Digest gates verify that the SemVer and TrunkVer tags point to the same image in each registry and that the Docker Hub digest matches Staging. Attestation jobs publish GitHub build-provenance and SBOM attestations for the pushed digest, so each target registry must accept OCI attestation artifacts.

## Kubernetes

Deploy with the content mounted read-only and a hardened security context:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: cdn-origin
  labels:
    app: cdn-origin
spec:
  replicas: 2
  selector:
    matchLabels:
      app: cdn-origin
  template:
    metadata:
      labels:
        app: cdn-origin
    spec:
      containers:
        - name: cdn-origin
          image: codebeltnet/web-cdn-origin:2.0.0
          ports:
            - containerPort: 8080
          env:
            - name: CdnOrigin__Cache__ImmutablePathPrefixes__0
              value: "/assets/"
          securityContext:
            runAsNonRoot: true
            runAsUser: 1654
            allowPrivilegeEscalation: false
            readOnlyRootFilesystem: true
            capabilities:
              drop: ["ALL"]
          readinessProbe:
            httpGet:
              path: /health/ready
              port: 8080
          livenessProbe:
            httpGet:
              path: /health/live
              port: 8080
          volumeMounts:
            - name: content
              mountPath: /cdnroot
              readOnly: true
      volumes:
        - name: content
          persistentVolumeClaim:
            claimName: cdn-content
            readOnly: true
```

## AWS CloudFront origin

Run the provider as a custom origin behind CloudFront:

1. Deploy the provider (Kubernetes, ECS, a VM, etc.) and expose it over HTTPS through a load balancer.
2. Create a CloudFront distribution with a **custom origin** pointing at the provider's hostname.
3. Let the origin's `Cache-Control` drive edge TTLs (CloudFront "Use origin cache headers"). The **revalidate** profile governs mutable URLs; place versioned/fingerprinted assets under an `ImmutablePathPrefixes` entry so they receive the **immutable** profile.
4. Forward the `Origin` header if you serve cross-origin assets so CORS behaves correctly, and forward `Range` for media.

Because the origin emits correct validators and cache directives, CloudFront revalidates efficiently with `If-None-Match`/`If-Modified-Since` and serves ranges natively.

## Security considerations

- **Read-only by design** — there are no write, upload, or delete paths.
- **Restrictive defaults** — unknown file types are rejected, directory browsing is disabled, and path traversal outside the content root is prevented.
- **CORS safety** — a wildcard/public origin can never be combined with credentials; this is enforced at startup.
- **No application-file exposure** — the content root is validated at startup, resolving symbolic links and junctions before checking for overlap with the application directory.
- **Hardened container** — non-root user, non-privileged port, read-only-root-filesystem friendly, minimal image, and no baked-in credentials or CDN configuration.
- **Health is not cacheable** — health responses are `Cache-Control: no-store`.

## Migration from 1.4.0 to 2.0.0

`2.0.0` is a deliberate major-version modernization. Behaviour that remained sound is preserved; breaking changes are listed below. See `CHANGELOG.md` for the full list.

### Configuration mapping

| `1.x` (environment variable) | `2.0.0` |
| --- | --- |
| `CDNROOT` | `CdnOrigin__ContentRoot` |
| `CDNROOT_DEFAULTFILES` (`;`-delimited) | `CdnOrigin__DefaultDocuments__0`, `__1`, … (array) |
| `CACHECONTROL_MAXAGE` + `CACHECONTROL_MAXAGE_TIMEUNIT` | `CdnOrigin__Cache__Revalidate__MaxAge` (`TimeSpan`, e.g. `12:00:00`) |
| `CACHECONTROL_SHAREDMAXAGE` + `CACHECONTROL_SHAREDMAXAGE_TIMEUNIT` | `CdnOrigin__Cache__Revalidate__SharedMaxAge` (`TimeSpan`, e.g. `7.00:00:00`) |
| `ETAG_BYTESTOREAD` | *(removed)* — `ETag` is produced from file metadata |

### Behavioural changes

- **`ETag`** is now produced by the framework from file identity and modification metadata. The server no longer reads or hashes file contents per request (`ETAG_BYTESTOREAD` and the custom MD5 hashing are gone).
- **Cache durations** are standard `TimeSpan` values instead of a number plus a separate time-unit variable.
- **`no-transform`** is no longer emitted by default.
- **`Expires`** is removed; `Cache-Control: max-age` is authoritative.
- **Server-side response caching** is removed — the CDN and HTTP clients handle caching.
- **Case-insensitive path lookup** is preserved for compatibility with asset URLs on every supported file system; ambiguous case-only matches are rejected.
- **Unknown file types are rejected by default** (previously served); add explicit MIME mappings to serve additional types.
- **CORS is configurable** instead of always emitting `Access-Control-Allow-Origin: *`; the default remains public.
- **The container port is `8080`** (was `80`) and the process runs as a **non-root** user.

## Local verification

```bash
# Restore, build (warnings are errors for source projects)
dotnet restore Codebelt.Cdn.Origin.slnx
dotnet build Codebelt.Cdn.Origin.slnx -c Release

# Formatting, code style, and analyzers (must produce no changes)
dotnet format Codebelt.Cdn.Origin.slnx --severity info --verify-no-changes

# Tests
dotnet test Codebelt.Cdn.Origin.slnx -c Release

# Tests with line + branch coverage for the application assembly (generated code excluded)
dotnet test test/Codebelt.Cdn.Origin.Tests/Codebelt.Cdn.Origin.Tests.csproj -c Release \
  /p:CollectCoverage=true /p:CoverletOutputFormat=json /p:CoverletOutput=./artifacts/coverage.json \
  /p:Include="[Codebelt.Cdn.Origin]*" /p:ExcludeByFile="**/*.g.cs"
dotnet test test/Codebelt.Cdn.Origin.FunctionalTests/Codebelt.Cdn.Origin.FunctionalTests.csproj -c Release \
  /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./artifacts/coverage.cobertura.xml \
  /p:MergeWith=./artifacts/coverage.json /p:Include="[Codebelt.Cdn.Origin]*" /p:ExcludeByFile="**/*.g.cs"

# Container
docker build -t codebeltnet/web-cdn-origin:2.0.0 -f src/Codebelt.Cdn.Origin/Dockerfile .
```

Code with passion; love your code; deliver with confidence 👨‍💻️🔥❤️🚀😎
