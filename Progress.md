# Happy Cooking: Progress / Catatan Pengembangan

*Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios.* Roadmap: [PLAN.md](PLAN.md).

[English](#english) · [Bahasa Indonesia](#bahasa-indonesia)

---

## English

### Log

**2026-10-20: v1.0 built**

- **Assets (Rodin MCP)**: 6 rigged characters, 7 sculpted dishes and 1 cooking pot (GLB); logo, world map, cast lineup, 13 UI icons and 8 pet portraits.
- **Asset pipeline (Blender)**: `tools/blender/process_assets.py` normalises, decimates, downsizes textures, rigs (Root/Body/Head) and adds Idle/Happy/Talk clips. 131 MB → 6.3 MB. `tools/art/prepare_art.py` keys out backgrounds and crops portraits and region cards.
- **Core**: `GameSession` with 12 systems (Economy, Inventory, Kitchen + CookingSession, Farm, Fishing, Pets, Restaurant, Quests, Achievements, Social, World, Seasonal) and a struct event bus. Bilingual JSON content: 6 regions, 65 ingredients, 33 recipes, 12 crops, 10 fish, 8 pets, 18 quests, 14 achievements, 8 decorations, 5 events.
- **Data**: SQLite save store (WAL, cooking log) and synced-folder cloud save with newest-wins sync.
- **App**: Avalonia 12 + ThreeNet. Title, map, kitchen, restaurant, farm, fishing, pets, recipe book, trophies, friends and settings screens. Five 3D stages plus a turntable preview, synthesised audio, a sticker-style theme with Lilita One and Nunito.
- **Platforms**: Desktop runs; Android and iOS heads compile.
- **Tests**: 176 unit tests passing.
- **Docs**: README, EN/ID docs, screenshots from the automated tour (`--screenshots`).
- **Release**: moved into the `SyubadubinStudios/HappyCooking` repo. Windows installer (Inno Setup, English + Indonesian wizard, per-user install) and portable zip built with `installer/build-installer.ps1`. Smoke-tested: silent install, full screenshot tour from the installed copy, clean uninstall.
- **Android release**: signed APK (arm64 + x64) built with `installer/build-android.ps1` and attached to v1.0.0. Not yet run on a device or emulator (the local emulator needs WHPX).

### Status by SDD module

| Module | Status | Notes |
|---|---|---|
| Recipe System | ✅ | 33 recipes, 6 mini-game kinds, stars, fun facts |
| Customer System | ✅ | Guests, patience, tips, rating |
| Quest System | ✅ | 18 chained quests |
| Pet System | ✅ | 8 pets, hunger, bonuses (2D portraits) |
| Farm System | ✅ | Real-time growth, rain bonus, up to 12 plots |
| Fishing System | ✅ | Rarity, night-only fish, per-island tables |
| Achievement System | ✅ | 14 trophies |
| Multiplayer Social System | 🟡 Partial | Offline friend and gift codes; no online play yet |

### Status by SDD feature

| Feature | Status |
|---|---|
| 3D cooking gameplay | ✅ |
| World exploration (6 regions) | ✅ |
| 300+ recipes | 🟡 33 so far |
| Restaurant building | 🟡 Decorations on fixed spots |
| Pet collection | ✅ |
| Farming / Fishing | ✅ |
| Multiplayer social | 🟡 Offline codes |
| Cultural food education | ✅ Fun facts EN/ID |
| Seasonal events | ✅ 5 events |
| ThreeNet showcase: HDR, PBR, bloom, particles, ocean, day/night, weather | ✅ |
| Toon shading | 🟡 Soft PBR "clay" style used instead |
| Cloud save | 🟡 Synced folder |
| Platforms | ✅ Desktop · 🟡 Android/iOS compile, not yet store-released |

### Known limitations

- The iOS build requires macOS for linking and signing.
- After a cloud sync downloads a newer save, the game must be restarted to load it. Saving is locked until then.
- On some GPUs, effects (fire, particles, water) fall back silently if unsupported.
- Graphics quality applies to the next screen opened.

---

## Bahasa Indonesia

### Catatan

**2026-10-20: v1.0 selesai dibangun**

- **Aset (Rodin MCP)**: 6 tokoh ber-rig, 7 hidangan yang dipahat, dan 1 panci (GLB); logo, peta dunia, ilustrasi tokoh, 13 ikon UI, dan 8 potret hewan peliharaan.
- **Pipeline aset (Blender)**: `tools/blender/process_assets.py` menormalisasi, men-decimate, mengecilkan tekstur, membuat rig (Root/Body/Head), dan menambah klip Idle/Happy/Talk. 131 MB → 6,3 MB. `tools/art/prepare_art.py` menghapus latar serta memotong potret dan kartu wilayah.
- **Core**: `GameSession` dengan 12 sistem (Economy, Inventory, Kitchen + CookingSession, Farm, Fishing, Pets, Restaurant, Quests, Achievements, Social, World, Seasonal) dan event bus berbasis struct. Konten JSON dua bahasa: 6 wilayah, 65 bahan, 33 resep, 12 tanaman, 10 ikan, 8 hewan, 18 misi, 14 trofi, 8 dekorasi, 5 event.
- **Data**: penyimpanan SQLite (WAL, riwayat memasak) dan simpan cloud via folder tersinkron, dengan aturan salinan terbaru yang menang.
- **Aplikasi**: Avalonia 12 + ThreeNet. Layar judul, peta, dapur, restoran, kebun, memancing, hewan, buku resep, trofi, teman, dan pengaturan. Lima stage 3D plus pratinjau meja putar, audio sintesis, tema gaya stiker dengan Lilita One dan Nunito.
- **Platform**: Desktop berjalan; head Android dan iOS berhasil di-compile.
- **Test**: 176 unit test lulus.
- **Dokumentasi**: README, dokumen EN/ID, screenshot dari tur otomatis (`--screenshots`).
- **Rilis**: dipindahkan ke repo `SyubadubinStudios/HappyCooking`. Installer Windows (Inno Setup, wizard Inggris + Indonesia, pemasangan per pengguna) dan zip portable dibuat dengan `installer/build-installer.ps1`. Diuji: pemasangan senyap, tur screenshot penuh dari hasil instalasi, uninstall bersih.
- **Rilis Android**: APK bertanda tangan (arm64 + x64) dibuat dengan `installer/build-android.ps1` dan dilampirkan di v1.0.0. Belum dijalankan di perangkat atau emulator (emulator lokal butuh WHPX).

### Status per modul SDD

| Modul | Status | Catatan |
|---|---|---|
| Recipe System | ✅ | 33 resep, 6 jenis mini-game, bintang, fakta menarik |
| Customer System | ✅ | Tamu, kesabaran, tip, rating |
| Quest System | ✅ | 18 misi berantai |
| Pet System | ✅ | 8 hewan, rasa lapar, bonus (potret 2D) |
| Farm System | ✅ | Tumbuh real-time, bonus hujan, hingga 12 petak |
| Fishing System | ✅ | Kelangkaan, ikan malam, tabel per pulau |
| Achievement System | ✅ | 14 trofi |
| Multiplayer Social System | 🟡 Sebagian | Kode teman dan hadiah tanpa internet; belum ada main online |

### Status per fitur SDD

| Fitur | Status |
|---|---|
| Gameplay memasak 3D | ✅ |
| Jelajah dunia (6 wilayah) | ✅ |
| 300+ resep | 🟡 Baru 33 |
| Membangun restoran | 🟡 Dekorasi di posisi tetap |
| Koleksi hewan | ✅ |
| Berkebun / memancing | ✅ |
| Sosial multiplayer | 🟡 Kode tanpa internet |
| Edukasi budaya kuliner | ✅ Fakta menarik EN/ID |
| Event musiman | ✅ 5 event |
| Pamer ThreeNet: HDR, PBR, bloom, partikel, laut, siang/malam, cuaca | ✅ |
| Toon shading | 🟡 Memakai gaya PBR lembut "tanah liat" |
| Simpan cloud | 🟡 Folder tersinkron |
| Platform | ✅ Desktop · 🟡 Android/iOS ter-compile, belum rilis di toko aplikasi |

### Keterbatasan yang diketahui

- Build iOS membutuhkan macOS untuk linking dan signing.
- Setelah sinkron cloud mengunduh simpanan yang lebih baru, game harus dimulai ulang untuk memuatnya. Sampai saat itu penyimpanan dikunci.
- Di sebagian GPU, efek (api, partikel, air) otomatis dinonaktifkan kalau tidak didukung.
- Kualitas grafis berlaku di layar berikutnya yang dibuka.
