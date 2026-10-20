# Build & rilis

## Prasyarat

- .NET 10 SDK
- Untuk Android: `dotnet workload install android`
- Untuk iOS: `dotnet workload install ios`, plus macOS dengan Xcode (atau Mac yang dipasangkan) untuk build dan signing
- Untuk installer Windows: [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`ISCC.exe`)
- GPU dengan DirectX 12, Metal, atau Vulkan

## Build, jalankan, test

```bash
dotnet build src/HappyCooking.Desktop                       # aplikasi desktop (+ Core, Data, App)
dotnet run --project src/HappyCooking.Desktop                # main normal
dotnet run --project src/HappyCooking.Desktop -- --demo      # profil demo, tidak disimpan
dotnet test tests/HappyCooking.Core.Tests                    # semua unit test
dotnet test tests/HappyCooking.Core.Tests --filter "FullyQualifiedName~CookingSessionTests"
dotnet build HappyCooking.slnx                               # semua proyek (butuh workload android + ios)
```

Versi paket diatur terpusat di `Directory.Packages.props`. Pengaturan build bersama ada di `Directory.Build.props`.

## Lokasi simpanan

`%LOCALAPPDATA%\HappyCooking\happycooking.db` di Windows, dan folder `LocalApplicationData` yang sama di platform lain. Pakai `--db <path>` untuk memakai lokasi lain.

## Publish desktop

```bash
dotnet publish src/HappyCooking.Desktop -c Release -r win-x64 --self-contained -o artifacts/publish/win-x64
dotnet publish src/HappyCooking.Desktop -c Release -r linux-x64 --self-contained -o artifacts/publish/linux-x64
dotnet publish src/HappyCooking.Desktop -c Release -r osx-arm64 --self-contained -o artifacts/publish/osx-arm64
```

ThreeNet menyertakan inti native untuk `win-x64`, `linux-x64`, `osx-arm64`, `osx-x64`, Android, dan iOS.

## Installer Windows

```powershell
powershell -ExecutionPolicy Bypass -File installer/build-installer.ps1
```

Skrip ini mem-publish build `win-x64` self-contained lalu menjalankan Inno Setup (`ISCC.exe installer/HappyCooking.iss`). Hasilnya:

```
artifacts/installer/HappyCooking-Setup-1.0.0-win-x64.exe
```

Lampirkan file itu ke rilis GitHub.

## Android

```bash
dotnet publish src/HappyCooking.Android -c Release
```

Perintah ini menghasilkan APK untuk `android-arm64` dan `android-x64` (minimal API 24, mode landscape). Tandatangani dengan keystore milikmu sebelum dibagikan.

## iOS

Di macOS: `dotnet build src/HappyCooking.iOS -c Release`, lalu lakukan signing dan deploy dengan perangkat Xcode.

---

*Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios*
