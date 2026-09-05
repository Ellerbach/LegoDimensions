[CmdletBinding()]
param(
    [string]$PicoSdkPath = $env:PICO_SDK_PATH
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($PicoSdkPath)) {
    throw 'Set PICO_SDK_PATH or pass -PicoSdkPath with a Pico SDK 2.3.0 or newer checkout.'
}

$firmwareRoot = $PSScriptRoot
$projects = @(
    @{ Name = 'pico_portal_simulator'; Board = 'pico2_w' },
    @{ Name = 'pico_portal_xsm3_sidecar'; Board = 'pico2' }
)

foreach ($project in $projects) {
    $source = Join-Path $firmwareRoot $project.Name
    $output = Join-Path $source 'build'
    cmake -S $source -B $output "-DPICO_SDK_PATH=$PicoSdkPath" "-DPICO_BOARD=$($project.Board)" -DCMAKE_BUILD_TYPE=Release
    if ($LASTEXITCODE -ne 0) { throw "CMake configuration failed for $($project.Name)." }
    cmake --build $output --parallel
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $($project.Name)." }
}
