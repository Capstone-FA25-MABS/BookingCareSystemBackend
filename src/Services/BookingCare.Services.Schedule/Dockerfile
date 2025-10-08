# Multi-stage build for optimized Docker image
# Stage 1: Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create non-root user for security
RUN groupadd -g 1000 appuser && \
    useradd -u 1000 -g appuser -s /bin/sh -d /home/appuser -m appuser && \
    chown -R appuser:appuser /app

# Stage 2: Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only project files first for better caching
COPY ["src/Services/BookingCare.Services.Schedule/BookingCare.Services.Schedule.csproj", "src/Services/BookingCare.Services.Schedule/"]
COPY ["src/Services/BookingCare.Services.Doctor/BookingCare.Services.Doctor.csproj", "src/Services/BookingCare.Services.Doctor/"]
COPY ["src/Services/BookingCare.Services.ServiceMedical/BookingCare.Services.ServiceMedical.csproj", "src/Services/BookingCare.Services.ServiceMedical/"]
COPY ["src/Shared/BookingCare.Shared.Common/BookingCare.Shared.Common.csproj", "src/Shared/BookingCare.Shared.Common/"]
COPY ["src/Shared/BookingCare.Shared.Cache/BookingCare.Shared.Cache.csproj", "src/Shared/BookingCare.Shared.Cache/"]
COPY ["src/Shared/BookingCare.Shared.EventBus/BookingCare.Shared.EventBus.csproj", "src/Shared/BookingCare.Shared.EventBus/"]

# Copy proto files needed for gRPC
COPY ["src/Services/BookingCare.Services.Doctor/Protos/", "src/Services/BookingCare.Services.Doctor/Protos/"]
COPY ["src/Services/BookingCare.Services.ServiceMedical/Protos/", "src/Services/BookingCare.Services.ServiceMedical/Protos/"]

# Restore dependencies
RUN dotnet restore "src/Services/BookingCare.Services.Schedule/BookingCare.Services.Schedule.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/Services/BookingCare.Services.Schedule"
RUN dotnet build "BookingCare.Services.Schedule.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BookingCare.Services.Schedule.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    --self-contained false \
    /p:UseAppHost=false

# Stage 4: Final runtime image
FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

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

ENTRYPOINT ["dotnet", "BookingCare.Services.Schedule.dll"]
