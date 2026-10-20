# Pipeline aset

```
Rodin MCP ──► assets/raw/models/*.glb ──► tools/blender/process_assets.py ──► src/HappyCooking.App/Assets/Models/*.glb
Rodin MCP ──► assets/art/**/*.png     ──► tools/art/prepare_art.py        ──► src/HappyCooking.App/Assets/Images/**
Tur screenshot ──► docs/screenshots/*.png ──► tools/art/optimize_screenshots.py ──► *.jpg
```

## 1. Membuat aset (Rodin MCP)

- **Model 3D** (`Generate3DFromPrompt`): 6 tokoh (Chef Happy, Coco si Panda, Jenggo si Koki Ayam, Mochi si Kucing, Pepper Bot, Tuan Bawang Gosong), 7 hidangan (rendang, sushi, pizza, cupcake pelangi, dim sum, donat galaksi, taco), dan sebuah panci. Setiap GLB mentah sekitar 8–12 MB, dengan ~35 ribu face dan tekstur 2048 px.
- **Art 2D** (`GenerateImageWithNanoBanana2`): logo, peta dunia, ilustrasi para tokoh, 13 ikon UI, dan 8 potret hewan peliharaan. Prompt meminta "plain pure white background" agar latarnya bisa dihapus.

Output mentah disimpan di `assets/raw/models` dan `assets/art` sebagai bahan sumber.

## 2. Optimasi, rigging, dan animasi (Blender)

`tools/blender/process_assets.py` memproses setiap model yang terdaftar di tabel `ASSETS`:

1. Mengimpor GLB, menggabungkan mesh, dan menerapkan transform.
2. **Normalisasi**: tokoh setinggi 0,85–1,0 m, makanan 0,36–0,45 m. Titik origin di tengah bawah, berdiri di atas tanah.
3. **Decimate** sesuai anggaran perangkat mobile: tokoh 12–14 ribu face, makanan 8 ribu, properti 9 ribu.
4. **Mengecilkan tekstur**: tokoh 1024 px, makanan 512 px, di-encode ulang sebagai JPEG.
5. **Rigging tokoh** dengan armature 3 tulang (Root → Body → Head) dan skin weight berdasarkan ketinggian dengan transisi halus di leher. Mesh dari Rodin jarang manifold, sehingga automatic weights tidak bisa diandalkan.
6. **Animasi** tiga klip yang diekspor sebagai animasi glTF: **Idle** (loop napas 2 detik dengan kepala bergoyang), **Happy** (melompat sambil berputar penuh lalu memantul saat mendarat), dan **Talk** (mengangguk dan menggoyangkan kepala).

Jalankan tanpa tampilan (headless) dari root repo:

```bash
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup -P tools/blender/process_assets.py
```

Skrip ini juga bisa ditempel ke `execute_blender_code` di Blender MCP. Skrip hanya menghapus objek yang ia impor sendiri. **Jangan pernah memanggil `bpy.ops.wm.read_factory_settings` lewat Blender MCP: perintah itu menghentikan server MCP.**

Hasilnya: **131 MB → 6,3 MB** untuk 14 model. Tokoh berukuran 0,6–0,7 MB dan makanan 0,25–0,33 MB.

## 3. Menyiapkan art 2D

```bash
python tools/art/prepare_art.py
```

- Ikon dan hewan: latar putih dihapus dengan flood fill dari tepi (sehingga warna putih di dalam gambar tetap ada), lalu gambar dipangkas dan dibuat persegi. Ikon berukuran 192 px dan hewan 256 px.
- Logo: latar dihapus lalu dipangkas menjadi lebar 900 px.
- Peta dunia: JPEG 1600 px, plus satu kartu potongan untuk tiap wilayah.
- Potret: potongan persegi tiap tokoh dari ilustrasi bersama.

## 4. Screenshot

```bash
dotnet run --project src/HappyCooking.Desktop -- --screenshots docs/screenshots
python tools/art/optimize_screenshots.py docs/screenshots
```

Tur ini menjalankan aplikasi sungguhan dengan profil demo ke setiap layar dalam dua bahasa, lalu mengubah PNG menjadi JPEG yang ringkas (sekitar 10x lebih kecil).

---

*Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios*
