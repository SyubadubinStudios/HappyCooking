# Building & release

## Prerequisites

- .NET 10 SDK
- For Android: `dotnet workload install android`
- For iOS: `dotnet workload install ios`, plus macOS with Xcode (or a paired Mac) to build and sign
- For the Windows installer: [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`ISCC.exe`)
- A GPU with DirectX 12, Metal or Vulkan

## Build, run, test

```bash
dotnet build src/HappyCooking.Desktop                       # desktop app (+ Core, Data, App)
dotnet run --project src/HappyCooking.Desktop                # normal play
dotnet run --project src/HappyCooking.Desktop -- --demo      # demo profile, not saved
dotnet test tests/HappyCooking.Core.Tests                    # all unit tests
dotnet test tests/HappyCooking.Core.Tests --filter "FullyQualifiedName~CookingSessionTests"
dotnet build HappyCooking.slnx                               # everything (needs the android + ios workloads)
```

Package versions are managed centrally in `Directory.Packages.props`. Shared build settings are in `Directory.Build.props`.

## Save location

`%LOCALAPPDATA%\HappyCooking\happycooking.db` on Windows, the same `LocalApplicationData` folder on other platforms. Use `--db <path>` to point elsewhere.

## Desktop publish

```bash
dotnet publish src/HappyCooking.Desktop -c Release -r win-x64 --self-contained -o artifacts/publish/win-x64
dotnet publish src/HappyCooking.Desktop -c Release -r linux-x64 --self-contained -o artifacts/publish/linux-x64
dotnet publish src/HappyCooking.Desktop -c Release -r osx-arm64 --self-contained -o artifacts/publish/osx-arm64
```

ThreeNet ships its native core for `win-x64`, `linux-x64`, `osx-arm64`, `osx-x64`, Android and iOS.

## Windows installer

```powershell
powershell -ExecutionPolicy Bypass -File installer/build-installer.ps1
```

The script publishes a self-contained `win-x64` build and runs Inno Setup (`ISCC.exe installer/HappyCooking.iss`). The output is:

```
artifacts/installer/HappyCooking-Setup-1.0.0-win-x64.exe
```

Attach that file to a GitHub release.

## Android

```bash
dotnet publish src/HappyCooking.Android -c Release
```

This produces an APK for `android-arm64` and `android-x64` (min API 24, landscape).

For a signed release APK use the script. The keystore never goes in the repo; keep it (and its password file) backed up, because every future update must be signed with the same key:

```powershell
# one time: create a keystore (keytool ships with the JDK)
keytool -genkeypair -keystore happycooking.keystore -alias happycooking -keyalg RSA -keysize 2048 -validity 10000

# key file with three lines: HC_KEYSTORE=<path>, HC_KEY_ALIAS=happycooking, HC_KEY_PASS=<password>
powershell -ExecutionPolicy Bypass -File installer/build-android.ps1 -KeyFile C:\path\happycooking.keystore.txt
```

Output: `artifacts/android/HappyCooking-1.0.0-android.apk`. Install it by enabling "Install unknown apps" on the device, or with `adb install`.

## iOS

On macOS: `dotnet build src/HappyCooking.iOS -c Release`, then sign and deploy with Xcode tooling.

---

*Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios*
