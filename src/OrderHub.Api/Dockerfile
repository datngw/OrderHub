# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore (cached layer)
COPY ["Directory.Build.props", "."]
COPY ["src/OrderHub.Api/OrderHub.Api.csproj", "src/OrderHub.Api/"]
COPY ["src/OrderHub.Infrastructure/OrderHub.Infrastructure.csproj", "src/OrderHub.Infrastructure/"]
COPY ["src/OrderHub.Application/OrderHub.Application.csproj", "src/OrderHub.Application/"]
COPY ["src/OrderHub.Domain/OrderHub.Domain.csproj", "src/OrderHub.Domain/"]
RUN dotnet restore "src/OrderHub.Api/OrderHub.Api.csproj"

# Copy source and publish
COPY . .
RUN dotnet publish "src/OrderHub.Api/OrderHub.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user (built into .NET 8+ images)
USER app

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD ["wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/health"]

ENTRYPOINT ["dotnet", "OrderHub.Api.dll"]
