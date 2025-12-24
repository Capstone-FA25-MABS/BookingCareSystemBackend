# Use the official .NET 8 runtime as the base image for final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS base
USER app
WORKDIR /app

# Expose the ports that the application uses
EXPOSE 6001
EXPOSE 6101

# Use the official .NET 8 SDK for building
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/Services/BookingCare.Services.AI/BookingCare.Services.AI.csproj", "src/Services/BookingCare.Services.AI/"]
COPY ["src/Shared/BookingCare.Shared.Common/BookingCare.Shared.Common.csproj", "src/Shared/BookingCare.Shared.Common/"]
COPY ["src/Shared/BookingCare.Shared.EventBus/BookingCare.Shared.EventBus.csproj", "src/Shared/BookingCare.Shared.EventBus/"]

# Restore NuGet packages
RUN dotnet restore "src/Services/BookingCare.Services.AI/BookingCare.Services.AI.csproj" -a $TARGETARCH

# Copy the entire source code
COPY . .

# Set the working directory to the project directory
WORKDIR "/src/src/Services/BookingCare.Services.AI"

# Build the application
RUN dotnet build "BookingCare.Services.AI.csproj" -c $BUILD_CONFIGURATION -o /app/build -a $TARGETARCH

# Publish the application
FROM build AS publish
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BookingCare.Services.AI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -a $TARGETARCH

# Final stage: create the runtime image
FROM base AS final

# Switch to root user to install packages
USER root

# Hack to Allow Tesseract to work
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        curl \
        tesseract-ocr \
        tesseract-ocr-eng \
        tesseract-ocr-vie \
        libleptonica-dev \
        libtesseract-dev \
        libfontconfig1 \
        libfreetype6 \
        libx11-6 \
        libxcb1 \
    && rm -rf /var/lib/apt/lists/*

# Download Vietnamese and English language data if not available
RUN apt-get update && \
    apt-get install -y --allow-unauthenticated wget && \
    mkdir -p /usr/share/tesseract-ocr/tessdata && \
    cd /usr/share/tesseract-ocr/tessdata && \
    echo "Downloading Vietnamese traineddata..." && \
    (wget -q https://raw.githubusercontent.com/tesseract-ocr/tessdata_best/main/vie.traineddata -O vie.traineddata || \
     wget -q https://github.com/tesseract-ocr/tessdata/raw/main/vie.traineddata -O vie.traineddata || \
     echo "Failed to download vie.traineddata") && \
    echo "Downloading English traineddata..." && \
    (wget -q https://raw.githubusercontent.com/tesseract-ocr/tessdata_best/main/eng.traineddata -O eng.traineddata || \
     wget -q https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata -O eng.traineddata || \
     echo "Failed to download eng.traineddata") && \
    apt-get remove -y wget && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Verify tessdata location and files
RUN echo "=== Checking tesseract data directory ===" && \
    ls -la /usr/share/tesseract-ocr/tessdata/ && \
    echo "=== Language files available ===" && \
    ls -la /usr/share/tesseract-ocr/tessdata/*.traineddata

RUN ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so

WORKDIR /app/x64

RUN ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 /app/x64/libleptonica-1.82.0.so
RUN ln -sf /usr/lib/x86_64-linux-gnu/libtesseract.so.4 /app/x64/libtesseract50.so

# Debug: Verify symlinks were created
RUN echo "=== Symlinks in /app/x64 ===" && \
    ls -la /app/x64/ && \
    echo "=== Available tesseract libs ===" && \
    find /usr/lib/x86_64-linux-gnu -name "*tesseract*" && \
    echo "=== Available leptonica libs ===" && \
    find /usr/lib/x86_64-linux-gnu -name "*lept*"

# Set library paths
ENV LD_LIBRARY_PATH=/app/x64:/usr/lib/x86_64-linux-gnu:/usr/lib
ENV TESSDATA_PREFIX=/usr/share/tesseract-ocr/tessdata

# Copy tessdata into /app directory for easy access
RUN mkdir -p /app/tessdata && \
    cp -r /usr/share/tesseract-ocr/tessdata/* /app/tessdata/ 2>/dev/null || true

# Verify tessdata files
RUN echo "=== Tesseract data files in /app/tessdata ===" && \
    ls -la /app/tessdata/

# Clean up to reduce image size
RUN apt-get clean && rm -rf /var/lib/apt/lists/*
# Hack to Allow Tesseract to work

# Switch back to the non-root user
USER app
WORKDIR /app

# Copy the published application
COPY --from=publish /app/publish .

# Copy SQL initialization scripts
COPY --chown=app:app ["data/db_ai.sql", "./data/"]

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:6001;http://+:6101

# Health check configuration
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:6001/health || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "BookingCare.Services.AI.dll"]
