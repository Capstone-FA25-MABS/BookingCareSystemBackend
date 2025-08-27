FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Services/BookingCare.Services.Analytics/BookingCare.Services.Analytics.csproj", "src/Services/BookingCare.Services.Analytics/"]
COPY ["src/Shared/BookingCare.Shared.Common/BookingCare.Shared.Common.csproj", "src/Shared/BookingCare.Shared.Common/"]
COPY ["src/Infrastructure/BookingCare.MessageBus/BookingCare.MessageBus.csproj", "src/Infrastructure/BookingCare.MessageBus/"]

# Restore dependencies
RUN dotnet restore "src/Services/BookingCare.Services.Analytics/BookingCare.Services.Analytics.csproj"

# Copy everything else and build
COPY . .

WORKDIR "/src/src/Services/BookingCare.Services.Analytics"
RUN dotnet build "BookingCare.Services.Analytics.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BookingCare.Services.Analytics.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingCare.Services.Analytics.dll"]
