<#
.SYNOPSIS
  Builds the signed Android release APK of Happy Cooking (arm64 + x64).

.DESCRIPTION
  The signing keystore is never stored in the repo. Pass its details as
  parameters, environment variables (HC_KEYSTORE, HC_KEY_ALIAS, HC_KEY_PASS),
  or a KEY=VALUE file with those three entries via -KeyFile.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File installer/build-android.ps1 -KeyFile C:\path\happycooking.keystore.txt

  Output: artifacts/android/HappyCooking-<version>-android.apk
#>
param(
    [string]$Version = "1.0.0",
    [int]$VersionCode = 1,
    [string]$KeyFile = "",
    [string]$Keystore = $env:HC_KEYSTORE,
    [string]$Alias = $env:HC_KEY_ALIAS,
    [string]$Password = $env:HC_KEY_PASS
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publish = Join-Path $root "artifacts\publish\android"
$output = Join-Path $root "artifacts\android"

if ($KeyFile) {
    foreach ($line in Get-Content $KeyFile) {
        $k, $v = $line -split "=", 2
        switch ($k.Trim()) {
            "HC_KEYSTORE" { $Keystore = $v.Trim() }
            "HC_KEY_ALIAS" { $Alias = $v.Trim() }
            "HC_KEY_PASS" { $Password = $v.Trim() }
        }
    }
}

if (-not $Keystore -or -not (Test-Path $Keystore) -or -not $Alias -or -not $Password) {
    throw "Signing keystore not configured. Create one with keytool and pass -KeyFile, or set HC_KEYSTORE/HC_KEY_ALIAS/HC_KEY_PASS."
}

Write-Host "==> Publishing Android $Version ($VersionCode)"
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
dotnet publish (Join-Path $root "src\HappyCooking.Android") -c Release -o $publish `
    -p:ApplicationDisplayVersion=$Version -p:ApplicationVersion=$VersionCode `
    -p:AndroidKeyStore=true -p:AndroidSigningKeyStore=$Keystore -p:AndroidSigningKeyAlias=$Alias `
    -p:AndroidSigningKeyPass=$Password -p:AndroidSigningStorePass=$Password
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

New-Item -ItemType Directory -Force $output | Out-Null
$apk = Get-ChildItem $publish -Filter "*-Signed.apk" | Select-Object -First 1
if (-not $apk) { throw "No signed APK was produced." }
$target = Join-Path $output "HappyCooking-$Version-android.apk"
Copy-Item $apk.FullName $target -Force
Get-Item $target | Format-Table Name, @{ Name = "MB"; Expression = { [math]::Round($_.Length / 1MB, 1) } }
