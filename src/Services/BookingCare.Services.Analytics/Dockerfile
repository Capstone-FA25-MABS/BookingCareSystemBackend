# Use the official .NET 8 runtime as the base image for final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app

# Expose the ports that the application uses
EXPOSE 6000
EXPOSE 6100

# Use the official .NET 8 SDK for building
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/Services/BookingCare.Services.Analytics/BookingCare.Services.Analytics.csproj", "src/Services/BookingCare.Services.Analytics/"]
COPY ["src/Shared/BookingCare.Shared.Common/BookingCare.Shared.Common.csproj", "src/Shared/BookingCare.Shared.Common/"]
COPY ["src/Shared/BookingCare.Shared.EventBus/BookingCare.Shared.EventBus.csproj", "src/Shared/BookingCare.Shared.EventBus/"]

# Restore NuGet packages
RUN dotnet restore "src/Services/BookingCare.Services.Analytics/BookingCare.Services.Analytics.csproj" -a $TARGETARCH

# Copy the entire source code
COPY . .

# Set the working directory to the project directory
WORKDIR "/src/src/Services/BookingCare.Services.Analytics"

# Build the application
RUN dotnet build "BookingCare.Services.Analytics.csproj" -c $BUILD_CONFIGURATION -o /app/build -a $TARGETARCH

# Publish the application
FROM build AS publish
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BookingCare.Services.Analytics.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -a $TARGETARCH

# Final stage: create the runtime image
FROM base AS final

# Install curl for health checks
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Switch back to app user for security
USER app
WORKDIR /app

# Copy the published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:6000;http://+:6100

# Health check configuration
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:6000/health || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "BookingCare.Services.Analytics.dll"]
