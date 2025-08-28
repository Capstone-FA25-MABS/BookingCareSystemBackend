FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project files
COPY ["tests/EventBusIntegrationTest/EventBusIntegrationTest.csproj", "tests/EventBusIntegrationTest/"]
COPY ["src/Shared/BookingCare.Shared.EventBus/BookingCare.Shared.EventBus.csproj", "src/Shared/BookingCare.Shared.EventBus/"]

# Restore dependencies
RUN dotnet restore "tests/EventBusIntegrationTest/EventBusIntegrationTest.csproj"

# Copy all source files
COPY ["tests/EventBusIntegrationTest/", "tests/EventBusIntegrationTest/"]
COPY ["src/Shared/BookingCare.Shared.EventBus/", "src/Shared/BookingCare.Shared.EventBus/"]

# Build the application
WORKDIR "/src/tests/EventBusIntegrationTest"
RUN dotnet build "EventBusIntegrationTest.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventBusIntegrationTest.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventBusIntegrationTest.dll"]
