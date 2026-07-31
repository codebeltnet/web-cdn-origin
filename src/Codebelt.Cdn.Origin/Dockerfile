# Static Content Provider (Codebelt.Cdn.Origin) container image.
#
# Build from the repository root so central build configuration is available:
#   docker build -t codebeltnet/web-cdn-origin:2.0.0 -f src/Codebelt.Cdn.Origin/Dockerfile .
#
# The image runs as a non-root user on the conventional non-privileged port 8080,
# supports a read-only root filesystem, and treats /cdnroot as a read-only content mount.

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy the central build configuration first so restore is cached independently of source changes.
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/Codebelt.Cdn.Origin/Codebelt.Cdn.Origin.csproj", "src/Codebelt.Cdn.Origin/"]
RUN dotnet restore "src/Codebelt.Cdn.Origin/Codebelt.Cdn.Origin.csproj"

COPY [".editorconfig", "./"]
COPY ["src/", "src/"]
RUN dotnet publish "src/Codebelt.Cdn.Origin/Codebelt.Cdn.Origin.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Serve on the conventional non-privileged container port; the default content mount point is /cdnroot.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    CdnOrigin__ContentRoot=/cdnroot

# Create the content mount point owned by the non-root 'app' user provided by the base image.
RUN mkdir -p /cdnroot && chown app:app /cdnroot

COPY --from=build /app/publish .

EXPOSE 8080
USER app

ENTRYPOINT ["dotnet", "Codebelt.Cdn.Origin.dll"]
