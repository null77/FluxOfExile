#!/usr/bin/env pwsh
# FluxOfExile Release Build Script
# Builds the release, creates zip, and compiles installer

param(
    [string]$Version = "5.0",
    [switch]$SkipBuild = $false,
    [switch]$SkipZip = $false,
    [switch]$SkipInstaller = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== FluxOfExile Release Build v$Version ===" -ForegroundColor Cyan
Write-Host ""

# 1. Build Release
if (-not $SkipBuild) {
    Write-Host "Step 1: Building release..." -ForegroundColor Yellow
    dotnet publish src/FluxOfExile/FluxOfExile.csproj `
        -c Release `
        -r win-x64 `
        --self-contained `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:PublishTrimmed=false `
        -o publish

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "[OK] Build complete" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "Step 1: Skipping build (using existing files)" -ForegroundColor Gray
    Write-Host ""
}

# 2. Create Zip
if (-not $SkipZip) {
    Write-Host "Step 2: Creating zip package..." -ForegroundColor Yellow
    $zipPath = "releases/FluxOfExile-v$Version.zip"

    # Create releases directory if it doesn't exist
    New-Item -ItemType Directory -Force -Path "releases" | Out-Null

    # Remove old zip if exists
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    # Create zip
    Compress-Archive -Path publish/FluxOfExile.exe,publish/icon.png -DestinationPath $zipPath -CompressionLevel Optimal

    $zipSize = (Get-Item $zipPath).Length / 1MB
    $sizeFormatted = [math]::Round($zipSize, 2)
    Write-Host "[OK] Zip created: $zipPath ($sizeFormatted MB)" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "Step 2: Skipping zip creation" -ForegroundColor Gray
    Write-Host ""
}

# 3. Create Installer
if (-not $SkipInstaller) {
    Write-Host "Step 3: Creating installer..." -ForegroundColor Yellow

    # Check if Inno Setup is installed
    $isccPath = $null
    $possiblePaths = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )

    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $isccPath = $path
            break
        }
    }

    if ($null -eq $isccPath) {
        # Try to find via PATH
        $isccPath = (Get-Command iscc -ErrorAction SilentlyContinue).Source
    }

    if ($null -ne $isccPath) {
        Write-Host "Found Inno Setup: $isccPath" -ForegroundColor Gray

        # Update version in .iss file
        $issContent = Get-Content "FluxOfExile.iss" -Raw
        $issContent = $issContent -replace '#define MyAppVersion ".*"', "#define MyAppVersion ""$Version"""
        Set-Content "FluxOfExile.iss" -Value $issContent -NoNewline

        # Compile installer
        & $isccPath "FluxOfExile.iss"

        if ($LASTEXITCODE -eq 0) {
            $installerPath = "releases/FluxOfExile-v$Version-Setup.exe"
            $installerSize = (Get-Item $installerPath).Length / 1MB
            $sizeFormatted = [math]::Round($installerSize, 2)
            Write-Host "[OK] Installer created: $installerPath ($sizeFormatted MB)" -ForegroundColor Green
        } else {
            Write-Host "[ERROR] Installer compilation failed" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "[WARN] Inno Setup not found. Skipping installer creation." -ForegroundColor Yellow
        Write-Host "  Download from: https://jrsoftware.org/isdl.php" -ForegroundColor Gray
    }
    Write-Host ""
} else {
    Write-Host "Step 3: Skipping installer creation" -ForegroundColor Gray
    Write-Host ""
}

# Summary
Write-Host "=== Release Build Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Release files in ./releases/:" -ForegroundColor White
Get-ChildItem "releases" -Filter "*v$Version*" | ForEach-Object {
    $size = $_.Length / 1MB
    $sizeFormatted = [math]::Round($size, 2)
    Write-Host "  - $($_.Name) ($sizeFormatted MB)" -ForegroundColor Gray
}
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Test the installer" -ForegroundColor Gray
Write-Host "  2. Create git tag: git tag -a v$Version -m 'Release v$Version'" -ForegroundColor Gray
Write-Host "  3. Push tag: git push origin v$Version" -ForegroundColor Gray
Write-Host "  4. Upload to GitHub releases" -ForegroundColor Gray
