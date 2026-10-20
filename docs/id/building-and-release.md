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

Perintah ini menghasilkan APK untuk `android-arm64` dan `android-x64` (minimal API 24, mode landscape).

Untuk APK rilis yang ditandatangani, gunakan skripnya. Keystore tidak pernah disimpan di repo; simpan cadangannya (beserta file kata sandinya), karena setiap pembaruan berikutnya wajib ditandatangani dengan kunci yang sama:

```powershell
# sekali saja: buat keystore (keytool ada di JDK)
keytool -genkeypair -keystore happycooking.keystore -alias happycooking -keyalg RSA -keysize 2048 -validity 10000

# file kunci berisi tiga baris: HC_KEYSTORE=<lokasi>, HC_KEY_ALIAS=happycooking, HC_KEY_PASS=<kata sandi>
powershell -ExecutionPolicy Bypass -File installer/build-android.ps1 -KeyFile C:\path\happycooking.keystore.txt
```

Hasil: `artifacts/android/HappyCooking-1.0.0-android.apk`. Pasang dengan mengaktifkan "Instal aplikasi tidak dikenal" di perangkat, atau lewat `adb install`.

## iOS

Di macOS: `dotnet build src/HappyCooking.iOS -c Release`, lalu lakukan signing dan deploy dengan perangkat Xcode.

---

*Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios*
