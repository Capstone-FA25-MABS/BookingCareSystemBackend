# Use the official .NET 8 runtime as the base image for final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app

# Expose the ports that the application uses
EXPOSE 6002
EXPOSE 6102

# Use the official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/Services/BookingCare.Services.Appointment/BookingCare.Services.Appointment.csproj", "src/Services/BookingCare.Services.Appointment/"]
COPY ["src/Shared/BookingCare.Shared.Common/BookingCare.Shared.Common.csproj", "src/Shared/BookingCare.Shared.Common/"]
COPY ["src/Shared/BookingCare.Shared.EventBus/BookingCare.Shared.EventBus.csproj", "src/Shared/BookingCare.Shared.EventBus/"]
COPY ["src/Shared/BookingCare.Shared.Cache/BookingCare.Shared.Cache.csproj", "src/Shared/BookingCare.Shared.Cache/"]
COPY ["src/Shared/BookingCare.Shared.FileUpload/BookingCare.Shared.FileUpload.csproj", "src/Shared/BookingCare.Shared.FileUpload/"]

# Copy proto files for gRPC dependencies
COPY ["src/Services/BookingCare.Services.Doctor/Protos/", "src/Services/BookingCare.Services.Doctor/Protos/"]
COPY ["src/Services/BookingCare.Services.User/Protos/", "src/Services/BookingCare.Services.User/Protos/"]
COPY ["src/Services/BookingCare.Services.Hospital/Protos/", "src/Services/BookingCare.Services.Hospital/Protos/"]
COPY ["src/Services/BookingCare.Services.ServiceMedical/Protos/", "src/Services/BookingCare.Services.ServiceMedical/Protos/"]
COPY ["src/Services/BookingCare.Services.Payment/Protos/", "src/Services/BookingCare.Services.Payment/Protos/"]

# Restore NuGet packages
RUN dotnet restore "src/Services/BookingCare.Services.Appointment/BookingCare.Services.Appointment.csproj"

# Copy the entire source code
COPY . .

# Set the working directory to the project directory
WORKDIR "/src/src/Services/BookingCare.Services.Appointment"

# Build the application
RUN dotnet build "BookingCare.Services.Appointment.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BookingCare.Services.Appointment.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

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
ENV ASPNETCORE_URLS=http://+:6002;http://+:6102

# Health check configuration
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:6002/health || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "BookingCare.Services.Appointment.dll"]
