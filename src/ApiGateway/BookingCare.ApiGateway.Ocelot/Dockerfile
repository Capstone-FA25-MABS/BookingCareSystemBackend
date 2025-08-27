FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/ApiGateway/BookingCare.ApiGateway.Ocelot/BookingCare.ApiGateway.Ocelot.csproj", "src/ApiGateway/BookingCare.ApiGateway.Ocelot/"]
RUN dotnet restore "src/ApiGateway/BookingCare.ApiGateway.Ocelot/BookingCare.ApiGateway.Ocelot.csproj"
COPY . .
WORKDIR "/src/src/ApiGateway/BookingCare.ApiGateway.Ocelot"
RUN dotnet build "BookingCare.ApiGateway.Ocelot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BookingCare.ApiGateway.Ocelot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingCare.ApiGateway.Ocelot.dll"]
