<p align="center">
  <img src="src/HappyCooking.App/Assets/Images/logo.png" alt="Happy Cooking" width="520" />
</p>

<h3 align="center">3D Cooking Adventure for Kids · Petualangan Memasak 3D untuk Anak</h3>

<p align="center">
  <b>Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios</b><br/>
  <i>Made by Ariana Mischa Fadhila of Syubadubin Studios</i>
</p>

<p align="center">
  <a href="#english">English</a> · <a href="#bahasa-indonesia">Bahasa Indonesia</a>
</p>

---

## English

Happy Cooking is a colourful 3D cooking game for children aged 5–12. Players sail between six culinary islands, from Nusantara to Dessert Wonderland, cook dishes through small touch-friendly mini-games, serve guests in their own restaurant, grow ingredients on a farm, go fishing, adopt pets and learn a fun fact about every dish. The whole game works in English and Bahasa Indonesia.

### Screenshots

| | |
|---|---|
| ![Title](docs/screenshots/01-title-en.jpg) | ![World map](docs/screenshots/02-world-map-en.jpg) |
| ![Kitchen: grill the satay](docs/screenshots/05-kitchen-grill-en.jpg) | ![Dish result](docs/screenshots/06-kitchen-result-en.jpg) |
| ![Restaurant](docs/screenshots/07-restaurant-en.jpg) | ![Farm](docs/screenshots/08-farm-en.jpg) |
| ![Fishing pier at night](docs/screenshots/09-fishing-en.jpg) | ![Pet house](docs/screenshots/10-pets-en.jpg) |
| ![Recipe book](docs/screenshots/11-recipe-book-en.jpg) | ![Trophies](docs/screenshots/12-trophies-en.jpg) |

### Features

- **3D cooking**: 33 recipes from 6 islands. Each recipe is a sequence of mini-games: Tap (chop, knead), Heat (stop the needle in the green), Pour (hold and release), Rhythm (stir, flip), Pick (choose the right ingredients among decoys) and Decorate (place toppings). Nothing can be failed: every dish earns at least one star.
- **World exploration**: Nusantara Island, Sakura Island, Pasta Kingdom, Dragon Wok City, Fiesta Mexico and Dessert Wonderland unlock as the player levels up. A day/night cycle and weather change in real time.
- **Restaurant**: guests order dishes you have learned and pay, plus a tip for quick service. Decorations raise the restaurant rating.
- **Farm**: 12 crops grow in real time. Rain and a gardening pet speed them up.
- **Fishing**: 10 fish with rarities. Some only bite at night.
- **Pets**: 8 pets, each with a bonus (tips, wider cooking zones, faster crops, rare fish luck, XP, patient guests). Keep them fed.
- **Quests and trophies**: 18 story quests from the cast and 14 trophies.
- **Friends and gifts, no accounts**: share an 8-letter chef code and send gifts as short checksummed codes.
- **Culture and learning**: a fun fact for every dish, in both languages.
- **Seasonal events**: Lunar New Year, Blossom Picnic, 17 August cook-off, Spooky Snacks and Winter Treats boost featured recipes.
- **Saving**: local SQLite autosave, plus optional cloud save through a folder that OneDrive, Google Drive, Dropbox or iCloud already syncs.

### Cast

Chef Happy, Coco the Panda, Jenggo the Chicken Chef, Mochi the Cat, Pepper Bot, and the grumpy Mr. Burnt Onion, who pops up when a dish gets burnt.

### Tech stack

| Area | Technology |
|---|---|
| Runtime | .NET 10 (C#) |
| UI | Avalonia UI 12.1.2, CommunityToolkit.Mvvm |
| 3D | ThreeNet 0.12 (Vibe_Graphics): Rust + wgpu core (DirectX 12, Metal, Vulkan) |
| Storage | SQLite (Microsoft.Data.Sqlite), JSON content |
| Assets | Rodin MCP (3D models and 2D art), Blender (optimisation, rigging, animation) |
| Platforms | Windows, Linux, macOS, Android, iOS |

### Requirements

- A GPU with DirectX 12, Metal or Vulkan support.
- Windows 10/11 x64 for the installer. Building from source needs the .NET 10 SDK.

### Quick start

**Play**: download `HappyCooking-Setup-1.0.0-win-x64.exe` from the [Releases](../../releases) page and run it.

**Build from source**:

```bash
dotnet build src/HappyCooking.Desktop            # build the desktop app
dotnet run --project src/HappyCooking.Desktop     # play with your own save
dotnet run --project src/HappyCooking.Desktop -- --demo   # mid-game demo profile, never saved
dotnet test tests/HappyCooking.Core.Tests         # 176 unit tests
dotnet build HappyCooking.slnx                    # everything, needs the android and ios workloads
```

| Flag | Meaning |
|---|---|
| `--demo` | Play with a mid-game profile kept in memory only |
| `--skip-title` | Open straight on the world map |
| `--db <path>` | Use a specific save database |
| `--screenshots <dir>` | Run the screenshot tour (EN + ID) and exit |

### Project structure

```
src/
  HappyCooking.Core      game rules, content JSON, systems (no UI)
  HappyCooking.Data      SQLite save store, folder cloud sync
  HappyCooking.App       Avalonia views, view models, ThreeNet 3D stages, assets
  HappyCooking.Desktop   Windows/Linux/macOS launcher, screenshot tour
  HappyCooking.Android   Android launcher
  HappyCooking.iOS       iOS launcher (build on macOS)
tests/HappyCooking.Core.Tests
tools/blender, tools/art  asset pipeline scripts
assets/                   raw Rodin output (source art)
docs/en, docs/id          documentation
```

### Documentation

- English: [docs/en](docs/en/index.md): [player guide](docs/en/player-guide.md), [architecture](docs/en/architecture.md), [content guide](docs/en/content-guide.md), [asset pipeline](docs/en/asset-pipeline.md), [building & release](docs/en/building-and-release.md)
- Roadmap: [PLAN.md](PLAN.md) · Development log: [Progress.md](Progress.md)

### License

Code is released under the MIT License (see [LICENSE](LICENSE)). The Lilita One and Nunito fonts are under the SIL Open Font License (`src/HappyCooking.App/Assets/Fonts`).

---

## Bahasa Indonesia

Happy Cooking adalah game memasak 3D penuh warna untuk anak usia 5–12 tahun. Pemain berlayar ke enam pulau kuliner, dari Nusantara sampai Negeri Ajaib Hidangan Penutup. Mereka memasak lewat mini-game sederhana yang ramah layar sentuh, melayani tamu di restoran sendiri, menanam bahan di kebun, memancing, mengadopsi hewan peliharaan, dan mempelajari fakta menarik dari setiap hidangan. Seluruh game tersedia dalam Bahasa Indonesia dan Inggris.

### Tangkapan layar

| | |
|---|---|
| ![Judul](docs/screenshots/01-title-id.jpg) | ![Peta dunia](docs/screenshots/02-world-map-id.jpg) |
| ![Dapur: bakar sate](docs/screenshots/05-kitchen-grill-id.jpg) | ![Hasil masakan](docs/screenshots/06-kitchen-result-id.jpg) |
| ![Restoran](docs/screenshots/07-restaurant-id.jpg) | ![Kebun](docs/screenshots/08-farm-id.jpg) |
| ![Dermaga mancing malam hari](docs/screenshots/09-fishing-id.jpg) | ![Rumah hewan](docs/screenshots/10-pets-id.jpg) |
| ![Buku resep](docs/screenshots/11-recipe-book-id.jpg) | ![Trofi](docs/screenshots/12-trophies-id.jpg) |

### Fitur

- **Memasak 3D**: 33 resep dari 6 pulau. Setiap resep berupa rangkaian mini-game: Tekan (potong, uleni), Panas (hentikan jarum di area hijau), Tuang (tahan lalu lepas), Ritme (aduk, balik), Pilih (pilih bahan yang benar di antara bahan jebakan), dan Hias (taruh topping). Tidak ada yang gagal: setiap hidangan minimal mendapat satu bintang.
- **Jelajah dunia**: Pulau Nusantara, Pulau Sakura, Kerajaan Pasta, Kota Dragon Wok, Fiesta Meksiko, dan Negeri Ajaib Hidangan Penutup terbuka seiring naiknya level. Siang/malam dan cuaca berubah secara real-time.
- **Restoran**: tamu memesan hidangan yang sudah kamu pelajari dan membayar, ditambah tip kalau dilayani cepat. Dekorasi menaikkan rating restoran.
- **Kebun**: 12 tanaman tumbuh secara real-time. Hujan dan hewan peliharaan berkebun mempercepatnya.
- **Memancing**: 10 jenis ikan dengan tingkat kelangkaan. Beberapa hanya muncul di malam hari.
- **Hewan peliharaan**: 8 hewan, masing-masing dengan bonus (tip, zona masak lebih lebar, tanaman lebih cepat, ikan langka, XP, tamu lebih sabar). Jangan lupa beri makan.
- **Misi dan trofi**: 18 misi cerita dari para tokoh dan 14 trofi.
- **Teman dan hadiah tanpa akun**: bagikan kode koki 8 huruf dan kirim hadiah lewat kode pendek yang punya checksum.
- **Budaya dan edukasi**: fakta menarik untuk setiap hidangan, dalam dua bahasa.
- **Event musiman**: Imlek, Piknik Musim Semi, Lomba Masak 17 Agustus, Camilan Seram, dan Hidangan Musim Dingin memberi bonus koin untuk resep pilihan.
- **Penyimpanan**: autosave lokal di SQLite, plus simpan di cloud lewat folder yang sudah disinkronkan OneDrive, Google Drive, Dropbox, atau iCloud.

### Para tokoh

Chef Happy, Coco si Panda, Jenggo si Koki Ayam, Mochi si Kucing, Pepper Bot, dan Tuan Bawang Gosong yang pemarah. Dia muncul kalau masakanmu gosong.

### Teknologi

| Bagian | Teknologi |
|---|---|
| Runtime | .NET 10 (C#) |
| UI | Avalonia UI 12.1.2, CommunityToolkit.Mvvm |
| 3D | ThreeNet 0.12 (Vibe_Graphics): inti Rust + wgpu (DirectX 12, Metal, Vulkan) |
| Penyimpanan | SQLite (Microsoft.Data.Sqlite), konten JSON |
| Aset | Rodin MCP (model 3D dan art 2D), Blender (optimasi, rigging, animasi) |
| Platform | Windows, Linux, macOS, Android, iOS |

### Kebutuhan sistem

- GPU yang mendukung DirectX 12, Metal, atau Vulkan.
- Windows 10/11 x64 untuk installer. Build dari source butuh .NET 10 SDK.

### Mulai cepat

**Bermain**: unduh `HappyCooking-Setup-1.0.0-win-x64.exe` dari halaman [Releases](../../releases), lalu jalankan.

**Build dari source**:

```bash
dotnet build src/HappyCooking.Desktop            # build aplikasi desktop
dotnet run --project src/HappyCooking.Desktop     # main dengan simpanan sendiri
dotnet run --project src/HappyCooking.Desktop -- --demo   # profil demo pertengahan game, tidak disimpan
dotnet test tests/HappyCooking.Core.Tests         # 176 unit test
dotnet build HappyCooking.slnx                    # semua proyek, butuh workload android dan ios
```

| Opsi | Arti |
|---|---|
| `--demo` | Main dengan profil pertengahan game yang hanya ada di memori |
| `--skip-title` | Langsung membuka peta dunia |
| `--db <path>` | Memakai file database simpanan tertentu |
| `--screenshots <dir>` | Menjalankan tur screenshot (EN + ID) lalu keluar |

### Struktur proyek

```
src/
  HappyCooking.Core      aturan game, konten JSON, sistem (tanpa UI)
  HappyCooking.Data      penyimpanan SQLite, sinkronisasi folder cloud
  HappyCooking.App       view Avalonia, view model, stage 3D ThreeNet, aset
  HappyCooking.Desktop   peluncur Windows/Linux/macOS, tur screenshot
  HappyCooking.Android   peluncur Android
  HappyCooking.iOS       peluncur iOS (build di macOS)
tests/HappyCooking.Core.Tests
tools/blender, tools/art  skrip pipeline aset
assets/                   output mentah Rodin (sumber art)
docs/en, docs/id          dokumentasi
```

### Dokumentasi

- Bahasa Indonesia: [docs/id](docs/id/index.md): [panduan pemain](docs/id/player-guide.md), [arsitektur](docs/id/architecture.md), [panduan konten](docs/id/content-guide.md), [pipeline aset](docs/id/asset-pipeline.md), [build & rilis](docs/id/building-and-release.md)
- Roadmap: [PLAN.md](PLAN.md) · Catatan pengembangan: [Progress.md](Progress.md)

### Lisensi

Kode dirilis dengan Lisensi MIT (lihat [LICENSE](LICENSE)). Font Lilita One dan Nunito memakai SIL Open Font License (`src/HappyCooking.App/Assets/Fonts`).
