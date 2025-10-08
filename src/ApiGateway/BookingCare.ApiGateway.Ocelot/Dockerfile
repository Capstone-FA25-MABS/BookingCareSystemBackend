# Multi-stage build for optimized Docker image
# Stage 1: Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create non-root user for security
RUN addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -s /bin/sh -D appuser && \
    chown -R appuser:appuser /app

# Stage 2: Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only project files first for better caching
COPY ["src/ApiGateway/BookingCare.ApiGateway.Ocelot/BookingCare.ApiGateway.Ocelot.csproj", "src/ApiGateway/BookingCare.ApiGateway.Ocelot/"]
COPY ["src/Shared/BookingCare.Shared.Common/BookingCare.Shared.Common.csproj", "src/Shared/BookingCare.Shared.Common/"]

# Restore dependencies
RUN dotnet restore "src/ApiGateway/BookingCare.ApiGateway.Ocelot/BookingCare.ApiGateway.Ocelot.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/ApiGateway/BookingCare.ApiGateway.Ocelot"
RUN dotnet build "BookingCare.ApiGateway.Ocelot.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BookingCare.ApiGateway.Ocelot.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    --self-contained false \
    /p:UseAppHost=false

# Stage 4: Final runtime image
FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apk add --no-cache curl

# Copy published files
COPY --from=publish --chown=appuser:appuser /app/publish .

# Switch to non-root user
USER appuser

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "BookingCare.ApiGateway.Ocelot.dll"]
