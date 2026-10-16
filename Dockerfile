# StageTix API — container image for Render (or any Docker host).
# Build:  docker build -t stagetix-api .
# Run:    docker run -p 8080:8080 -e ConnectionStrings__Default=... -e Jwt__SigningKey=... stagetix-api

# ---------- 1. Build stage: full SDK, compiles and publishes ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first, so "restore" is cached until a dependency changes.
COPY global.json Directory.Build.props ./
COPY StageTix.Core/StageTix.Core.csproj StageTix.Core/
COPY StageTix.Data/StageTix.Data.csproj StageTix.Data/
COPY StageTix.Service/StageTix.Service.csproj StageTix.Service/
COPY StageTix.API/StageTix.API.csproj StageTix.API/
RUN dotnet restore StageTix.API/StageTix.API.csproj

COPY . .
RUN dotnet publish StageTix.API/StageTix.API.csproj -c Release -o /app/publish --no-restore

# ---------- 2. Runtime stage: small ASP.NET image, no SDK, no source code ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
# Render sets PORT (Program.cs listens on it). 8080 is the default when PORT is not set.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# NLog's file target writes to /app/logs; the app runs as a non-root user, so give it that folder.
RUN mkdir -p /app/logs && chown "$APP_UID" /app/logs
USER $APP_UID

# Migrations and demo data are applied automatically at startup (see Program.cs).
ENTRYPOINT ["dotnet", "StageTix.API.dll"]
