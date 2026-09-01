<#
.SYNOPSIS
Downloads the latest official libusb Windows release and extracts libusb-1.0.dll
into XboxPortalProbe/native, where the .csproj copies it to the build output.

.DESCRIPTION
The DLL itself is not committed to source control (see .gitignore); run this
script to (re)fetch it. Uses Windows' built-in tar.exe (bsdtar/libarchive),
which supports 7z extraction natively - no separate tool required.
#>
[CmdletBinding()]
param(
    # Matches the folder libusb ships prebuilt DLLs under inside the release .7z (e.g. VS2025/MS64/dll).
    [string]$ArchiveSubPath = "VS2025/MS64/dll/libusb-1.0.dll"
)

$ErrorActionPreference = "Stop"

$nativeDir = Join-Path (Split-Path -Parent $PSScriptRoot) "native"
New-Item -ItemType Directory -Force -Path $nativeDir | Out-Null

Write-Host "Querying the latest libusb release..."
$release = Invoke-RestMethod -Uri "https://api.github.com/repos/libusb/libusb/releases/latest" -Headers @{ "User-Agent" = "LegoDimensions-libusb-updater" }
$asset = $release.assets | Where-Object { $_.name -match "^libusb-[\d.]+\.7z$" } | Select-Object -First 1
if (-not $asset) {
    throw "Could not find a libusb-*.7z asset in the latest release ($($release.tag_name))."
}

Write-Host "Latest release: $($release.tag_name) ($($asset.name))"

$archivePath = Join-Path ([System.IO.Path]::GetTempPath()) $asset.name
$extractDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
try {
    Write-Host "Downloading $($asset.browser_download_url)..."
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $archivePath

    New-Item -ItemType Directory -Force -Path $extractDir | Out-Null
    Write-Host "Extracting $ArchiveSubPath..."
    tar -xf $archivePath -C $extractDir $ArchiveSubPath
    if ($LASTEXITCODE -ne 0) {
        throw "tar exited with code $LASTEXITCODE while extracting $ArchiveSubPath from $($asset.name)."
    }

    $extractedDll = Join-Path $extractDir $ArchiveSubPath.Replace("/", [System.IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path $extractedDll)) {
        throw "Extraction failed: $extractedDll was not created. Check that $ArchiveSubPath exists in $($asset.name)."
    }

    $dllPath = Join-Path $nativeDir "libusb-1.0.dll"
    Copy-Item $extractedDll $dllPath -Force
    Write-Host "libusb-1.0.dll ($($release.tag_name)) is now at $dllPath"
}
finally {
    Remove-Item $archivePath -Force -ErrorAction SilentlyContinue
    Remove-Item $extractDir -Recurse -Force -ErrorAction SilentlyContinue
}
