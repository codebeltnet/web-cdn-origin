# Static Content Provider (Codebelt.Cdn.Origin) container image.
#
# Build from the repository root so central build configuration is available:
#   docker build -t codebeltnet/web-cdn-origin:2.0.0 -f src/Codebelt.Cdn.Origin/Dockerfile .
#
# The image runs as a non-root user on the conventional non-privileged port 8080,
# supports a read-only root filesystem, and treats /cdnroot as a read-only content mount.

FROM dhi.io/dotnet:10-sdk-alpine3.24 AS build
WORKDIR /src

# Copy the central build configuration first so restore is cached independently of source changes.
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/Codebelt.Cdn.Origin/Codebelt.Cdn.Origin.csproj", "src/Codebelt.Cdn.Origin/"]
RUN dotnet restore "src/Codebelt.Cdn.Origin/Codebelt.Cdn.Origin.csproj"

COPY [".editorconfig", "./"]
COPY ["src/", "src/"]
RUN dotnet publish "src/Codebelt.Cdn.Origin/Codebelt.Cdn.Origin.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false
RUN mkdir -p /cdnroot

FROM dhi.io/aspnetcore:10-alpine3.24 AS final
WORKDIR /app

# Serve on the conventional non-privileged container port; the default content mount point is /cdnroot.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    CdnOrigin__ContentRoot=/cdnroot

# The hardened runtime has no shell, so prepare the content mount point in the SDK stage.
COPY --from=build --chown=65532:65532 /cdnroot /cdnroot
COPY --from=build --chown=65532:65532 /app/publish .

EXPOSE 8080
USER 65532

ENTRYPOINT ["dotnet", "Codebelt.Cdn.Origin.dll"]
