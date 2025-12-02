# Download Tesseract Language Data Files
# This script downloads Vietnamese and English trained data files for Tesseract OCR

$tessdataDir = "tessdata"
$baseUrl = "https://github.com/tesseract-ocr/tessdata/raw/main"

# Create tessdata directory if it doesn't exist
if (-not (Test-Path $tessdataDir)) {
    New-Item -ItemType Directory -Path $tessdataDir
    Write-Host "Created tessdata directory"
}

# Download Vietnamese trained data
$vieFile = Join-Path $tessdataDir "vie.traineddata"
if (-not (Test-Path $vieFile)) {
    Write-Host "Downloading Vietnamese language data..."
    Invoke-WebRequest -Uri "$baseUrl/vie.traineddata" -OutFile $vieFile
    Write-Host "Downloaded vie.traineddata"
} else {
    Write-Host "vie.traineddata already exists"
}

# Download English trained data
$engFile = Join-Path $tessdataDir "eng.traineddata"
if (-not (Test-Path $engFile)) {
    Write-Host "Downloading English language data..."
    Invoke-WebRequest -Uri "$baseUrl/eng.traineddata" -OutFile $engFile
    Write-Host "Downloaded eng.traineddata"
} else {
    Write-Host "eng.traineddata already exists"
}

Write-Host "`nTesseract data files downloaded successfully!"
Write-Host "Files location: $((Get-Location).Path)\$tessdataDir"
