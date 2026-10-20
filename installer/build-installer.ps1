<#
.SYNOPSIS
  Builds the Happy Cooking Windows release: publishes the desktop app (self-contained,
  win-x64, ReadyToRun), zips it as a portable build, and compiles the Inno Setup installer.

.EXAMPLE
  pwsh installer/build-installer.ps1
  pwsh installer/build-installer.ps1 -Version 1.0.1

  Output:
    artifacts/installer/HappyCooking-Setup-<version>-win-x64.exe
    artifacts/installer/HappyCooking-<version>-win-x64-portable.zip
#>
param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release",
    [string]$Iscc = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publish = Join-Path $root "artifacts\publish\win-x64"
$output = Join-Path $root "artifacts\installer"

if (-not $Iscc) {
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    )
    $Iscc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $Iscc) {
        throw "Inno Setup 6 was not found. Install it (winget install JRSoftware.InnoSetup) or pass -Iscc <path to ISCC.exe>."
    }
}

Write-Host "==> Running tests"
dotnet test (Join-Path $root "tests\HappyCooking.Core.Tests") -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Tests failed." }

Write-Host "==> Publishing win-x64 $Version"
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
dotnet publish (Join-Path $root "src\HappyCooking.Desktop") -c $Configuration -r win-x64 --self-contained true `
    -p:PublishReadyToRun=true -p:Version=$Version -p:DebugType=none -o $publish
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

New-Item -ItemType Directory -Force $output | Out-Null

Write-Host "==> Portable zip"
$zip = Join-Path $output "HappyCooking-$Version-win-x64-portable.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $publish "*") -DestinationPath $zip -CompressionLevel Optimal

Write-Host "==> Installer"
& $Iscc "/DAppVersion=$Version" "/DPublishDir=$publish" "/DOutputDir=$output" (Join-Path $PSScriptRoot "HappyCooking.iss")
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed." }

Get-ChildItem $output | Format-Table Name, @{ Name = "MB"; Expression = { [math]::Round($_.Length / 1MB, 1) } }
