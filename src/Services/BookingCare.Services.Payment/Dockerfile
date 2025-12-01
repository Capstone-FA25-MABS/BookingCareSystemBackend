# Use the official .NET 8 runtime as the base image for final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app

# Expose the ports that the application uses
EXPOSE 6011
EXPOSE 6111

# Use the official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/Services/BookingCare.Services.Payment/BookingCare.Services.Payment.csproj", "src/Services/BookingCare.Services.Payment/"]
COPY ["src/Shared/BookingCare.Shared.Common/BookingCare.Shared.Common.csproj", "src/Shared/BookingCare.Shared.Common/"]
COPY ["src/Shared/BookingCare.Shared.EventBus/BookingCare.Shared.EventBus.csproj", "src/Shared/BookingCare.Shared.EventBus/"]

# Restore NuGet packages
RUN dotnet restore "src/Services/BookingCare.Services.Payment/BookingCare.Services.Payment.csproj"

# Copy the entire source code
COPY . .

# Set the working directory to the project directory
WORKDIR "/src/src/Services/BookingCare.Services.Payment"

# Build the application
RUN dotnet build "BookingCare.Services.Payment.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BookingCare.Services.Payment.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

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
ENV ASPNETCORE_URLS=http://+:6011;http://+:6111

# Health check configuration
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:6011/health || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "BookingCare.Services.Payment.dll"]
