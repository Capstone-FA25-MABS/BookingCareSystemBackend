# Clean and Build Script for Doctor Service (PowerShell)
# This script helps resolve build issues by cleaning and rebuilding

param(
    [switch]$Verbose
)

# Colors for output
$Red = "Red"
$Green = "Green"
$Yellow = "Yellow"
$Blue = "Blue"

function Write-Header {
    param([string]$Message)
    Write-Host "=== $Message ===" -ForegroundColor $Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor $Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor $Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor $Yellow
}

Write-Header "Cleaning Doctor Service Project"

# Clean bin and obj directories
Write-Info "Cleaning bin and obj directories..."
if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" }
if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" }
Write-Success "Clean completed"

# Clean NuGet cache for this project
Write-Info "Cleaning NuGet cache..."
dotnet nuget locals all --clear
Write-Success "NuGet cache cleared"

# Restore packages
Write-Info "Restoring NuGet packages..."
dotnet restore
Write-Success "Packages restored"

# Build project
Write-Info "Building project..."
dotnet build --no-restore
Write-Success "Build completed"

# Build with detailed output if there are errors
Write-Info "Building with detailed output..."
$buildResult = dotnet build --no-restore --verbosity normal 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Showing detailed errors..."
    dotnet build --no-restore --verbosity detailed
    exit 1
}

Write-Success "Doctor Service project cleaned and built successfully!"
Write-Info "You can now run: dotnet run"
