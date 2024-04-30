FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY ["Codebelt.Cdn.Origin.csproj", "Codebelt.Cdn.Origin/"]
RUN dotnet restore "Codebelt.Cdn.Origin/Codebelt.Cdn.Origin.csproj"

COPY [".", "Codebelt.Cdn.Origin/"]

WORKDIR "/src/Codebelt.Cdn.Origin"
RUN dotnet build "Codebelt.Cdn.Origin.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Codebelt.Cdn.Origin.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Codebelt.Cdn.Origin.dll"]
