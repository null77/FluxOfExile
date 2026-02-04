# Building Releases

This document explains how to build FluxOfExile releases.

## Prerequisites

### Required
- .NET SDK 10.0 or later
- Windows 10/11

### Optional (for installer)
- [Inno Setup 6](https://jrsoftware.org/isdl.php) - Required to create Windows installer

## Quick Start

### Option 1: Using the Build Script (Recommended)

```powershell
# Build everything (zip + installer)
.\build-release.ps1 -Version "5.0"

# Build only zip (skip installer)
.\build-release.ps1 -Version "5.0" -SkipInstaller

# Use existing build (only create packages)
.\build-release.ps1 -Version "5.0" -SkipBuild
```

### Option 2: Manual Build

1. **Build the release:**
   ```powershell
   dotnet publish src/FluxOfExile/FluxOfExile.csproj `
       -c Release `
       -r win-x64 `
       --self-contained `
       -p:PublishSingleFile=true `
       -p:IncludeNativeLibrariesForSelfExtract=true `
       -p:PublishTrimmed=false `
       -o publish
   ```

2. **Create zip package:**
   ```powershell
   New-Item -ItemType Directory -Force -Path "releases"
   Compress-Archive -Path publish/FluxOfExile.exe,publish/icon.png `
                    -DestinationPath releases/FluxOfExile-v5.0.zip `
                    -Force
   ```

3. **Create installer (requires Inno Setup):**
   ```powershell
   # Update version in FluxOfExile.iss first
   iscc FluxOfExile.iss
   ```

## Output

After running the build script, you'll find:

```
releases/
├── FluxOfExile-v5.0.zip         # Portable version (44 MB)
└── FluxOfExile-v5.0-Setup.exe   # Installer (44 MB)
```

## Installing Inno Setup

1. Download from: https://jrsoftware.org/isdl.php
2. Run the installer (use default options)
3. Inno Setup will be added to your PATH automatically

## Release Checklist

- [ ] Update version number in build script
- [ ] Update version in `FluxOfExile.iss`
- [ ] Run `.\build-release.ps1 -Version "X.X"`
- [ ] Test the zip (extract and run)
- [ ] Test the installer (install, run, uninstall)
- [ ] Create git tag: `git tag -a vX.X -m "Release vX.X"`
- [ ] Push tag: `git push origin vX.X`
- [ ] Create GitHub release and upload files
- [ ] Update release notes

## Troubleshooting

### "Inno Setup not found"
- Install Inno Setup from the link above
- Or use `-SkipInstaller` flag to build only the zip

### "Access denied" during build
- Close any running FluxOfExile.exe instances
- The script will show this error if the exe is locked

### Version number mismatch
- Update version in three places:
  1. Build script parameter: `-Version "X.X"`
  2. FluxOfExile.iss: `#define MyAppVersion "X.X"`
  3. Git tag: `vX.X`
