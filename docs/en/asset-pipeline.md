# Asset pipeline

```
Rodin MCP ──► assets/raw/models/*.glb ──► tools/blender/process_assets.py ──► src/HappyCooking.App/Assets/Models/*.glb
Rodin MCP ──► assets/art/**/*.png     ──► tools/art/prepare_art.py        ──► src/HappyCooking.App/Assets/Images/**
Screenshot tour ──► docs/screenshots/*.png ──► tools/art/optimize_screenshots.py ──► *.jpg
```

## 1. Generate (Rodin MCP)

- **3D models** (`Generate3DFromPrompt`): 6 characters (Chef Happy, Coco the Panda, Jenggo the Chicken Chef, Mochi the Cat, Pepper Bot, Mr. Burnt Onion), 7 dishes (rendang, sushi, pizza, rainbow cupcake, dim sum, galaxy donut, taco) and a cooking pot. Each raw GLB is about 8–12 MB, with ~35k faces and 2048 px textures.
- **2D art** (`GenerateImageWithNanoBanana2`): logo, world map, cast lineup, 13 UI icons and 8 pet portraits. Prompts ask for "plain pure white background" so the background can be keyed out.

Raw output is kept in `assets/raw/models` and `assets/art` as source material.

## 2. Optimise, rig and animate (Blender)

`tools/blender/process_assets.py` processes every model listed in its `ASSETS` table:

1. Imports the GLB, joins meshes and applies transforms.
2. **Normalises**: characters to 0.85–1.0 m tall, food to 0.36–0.45 m. Origin at the bottom centre, standing on the ground.
3. **Decimates** to a mobile budget: characters 12–14k faces, food 8k, props 9k.
4. **Downsizes textures**: characters 1024 px, food 512 px, re-encoded as JPEG.
5. **Rigs characters** with a 3-bone armature (Root → Body → Head) and height-based skin weights with a smooth blend at the neck. Rodin meshes are rarely manifold, so automatic weights are unreliable.
6. **Animates** three clips exported as glTF animations: **Idle** (2 s breathing loop with a head sway), **Happy** (a hop with a full spin and a squash on landing) and **Talk** (nods and a head waggle).

Run headless from the repo root:

```bash
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup -P tools/blender/process_assets.py
```

The script can also be pasted into Blender MCP's `execute_blender_code`. It only removes objects it imported. **Never call `bpy.ops.wm.read_factory_settings` through Blender MCP: it stops the MCP server.**

Result: **131 MB → 6.3 MB** for 14 models. Characters are 0.6–0.7 MB and food 0.25–0.33 MB.

## 3. Prepare 2D art

```bash
python tools/art/prepare_art.py
```

- Icons and pets: the white background is keyed out by flood fill from the edges (so white highlights inside survive), then the image is trimmed and squared. Icons are 192 px and pets 256 px.
- Logo: keyed out and trimmed to 900 px wide.
- World map: 1600 px JPEG, plus one cropped card per region.
- Portraits: square crops of each character from the lineup.

## 4. Screenshots

```bash
dotnet run --project src/HappyCooking.Desktop -- --screenshots docs/screenshots
python tools/art/optimize_screenshots.py docs/screenshots
```

The tour drives the real app with the demo profile through every screen in both languages, then converts the PNGs to compact JPEGs (about 10x smaller).

---

*Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios*
