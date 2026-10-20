# Panduan konten

Semua konten game ada di `src/HappyCooking.Core/Content/Data/*.json` dan ditanam di assembly Core. Menambah konten cukup dengan mengedit JSON, tanpa mengubah kode. Kolom teks berupa objek dua bahasa: `{ "en": "...", "id": "..." }`.

Setiap selesai mengubah konten, jalankan:

```bash
dotnet test tests/HappyCooking.Core.Tests --filter "FullyQualifiedName~ContentTests"
```

`GameContent.Validate()` melaporkan id yang tidak dikenal (misalnya resep yang menunjuk bahan yang tidak ada, atau misi dengan target yang salah). Test juga memainkan setiap resep untuk memastikan resep itu bisa mencapai 3 bintang.

## Resep (`recipes.json`)

| Kolom | Arti |
|---|---|
| `id` | Id unik, juga dipakai oleh misi dan event |
| `name`, `description`, `funFact` | Teks dua bahasa. Fakta menarik tampil setelah memasak dan di buku resep. |
| `region` | Id wilayah |
| `difficulty` | 1–3, ditampilkan sebagai titik |
| `unlockLevel` | Level pemain yang dibutuhkan (wilayahnya juga harus sudah terbuka) |
| `coins`, `xp` | Hadiah dasar. 3 bintang dapat 100%, 2 bintang 80%, 1 bintang 60%. |
| `color` | `#RRGGBB`, dipakai untuk warna di UI dan hidangan 3D generik |
| `model` | Nama GLB di `App/Assets/Models` (tanpa `.glb`), atau `null` untuk hidangan prosedural |
| `creative` | `true` untuk hidangan imajinatif (tab Kreasi, trofi) |
| `ingredients` | Id bahan. Masing-masing terpakai satu setiap kali memasak. |
| `steps` | Mini-game, sesuai urutan |

### Langkah

| `kind` | Kolom yang dipakai | Catatan |
|---|---|---|
| `Tap` | `count` | Waktu ideal 0,3 detik per tekanan |
| `Heat` | `target`, `zone`, `speed` | Jarum naik sebesar `speed` per detik. Berhenti di atas `batas atas zona + 0,15` membuat masakan gosong. |
| `Pour` | `target`, `zone` | Terisi 0,42 per detik selama tombol ditahan |
| `Rhythm` | `count`, `zone`, `speed` | Zona ada di tengah. `speed` adalah jumlah ayunan penanda per detik. |
| `Pick` | `items` | Bahan yang benar. 2–3 bahan jebakan ditambahkan otomatis. |
| `Decorate` | `count` | Selalu mendapat nilai penuh |

Setiap langkah butuh `verb`. Teks yang ditampilkan adalah `step.<verb>` dari `strings.json`, diformat dengan nama `item` langkah itu (atau nama resep kalau `item` kosong). **Saat menambah verb baru, tambahkan `step.<verb>` (dan opsional `action.<verb>` untuk label tombol) ke `strings.json` dalam dua bahasa.**

```json
{ "kind": "Heat", "verb": "fry", "item": "egg", "target": 0.6, "zone": 0.18, "speed": 0.4 }
```

## File lain

| File | Kolom penting |
|---|---|
| `ingredients.json` | `id`, `name`, `color`, `source` (`Market`/`Farm`/`Sea`), `price` |
| `regions.json` | `id`, `name`, `description`, `unlockLevel`, `color`, `host` (id tokoh), `mapX`/`mapY` (posisi pin 0–1 di peta dunia), `fish` (id ikan) |
| `characters.json` | `id`, `name`, `role`, `greeting`, `model`, `color`. Potret disimpan di `App/Assets/Images/Portraits/<id>.jpg`. |
| `crops.json` | `id`, `ingredient`, `seedPrice`, `growSeconds`, `yield`, `xp`, `color` |
| `fish.json` | `id`, `name`, `ingredient`, `rarity` (`Common`…`Legendary`), `nightOnly`, `coins`, `color` |
| `pets.json` | `id`, `name`, `description`, `bonus` (`Tips`, `CookingZone`, `Growth`, `FishLuck`, `Xp`, `Patience`), `amount` (0,25 = +25%), `price`, `priceInGems`, `unlockLevel`, `color`. Gambar disimpan di `Images/Pets/<id>.png`. |
| `quests.json` | `goal` (`CookRecipe`, `CookInRegion`, `CookAny`, `EarnStars`, `ServeCustomers`, `Harvest`, `CatchFish`, `FeedPet`, `AdoptPet`, `PlaceDecoration`, `ReachLevel`), `target`, `amount`, hadiah, `requires` (id misi sebelumnya, untuk urutan cerita), `giver` |
| `achievements.json` | `stat` (lihat `AchievementStat`), `threshold`, `rewardGems`, `icon` (nama di `Images/Icons`) |
| `decorations.json` | `price`, `rating` (poin; setiap 4 poin menambah satu bintang restoran), `shape` (`pot`, `lantern`, `plant`, `balloons`, `fountain`, `aquarium`, `rug`, `piano`), `color` |
| `events.json` | `startMonth`/`startDay`, `endMonth`/`endDay` (rentang boleh melewati tahun baru), `coinMultiplier`, `featuredRecipes` |
| `strings.json` | Teks UI: `key → { en, id }`. Kunci yang hilang akan tampil sebagai nama kuncinya, jadi kekurangan mudah terlihat. |

## Kunci string dinamis

Beberapa kunci dirangkai di kode. Pastikan semuanya lengkap saat menambah nilai enum:

`step.<verb>`, `action.<verb>`, `hint.<stepkind>`, `weather.<sunny|cloudy|rain|snow>`, `rarity.<rarity>`, `pet.bonus.<bonus>`, `fishing.status.<phase>`, `fishing.button.<phase>`, `cloud.<outcome>`, `friends.redeem.<error>`.

---

*Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios*
