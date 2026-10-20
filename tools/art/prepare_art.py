"""
Happy Cooking - 2D art pipeline.

Takes the Rodin (Nano Banana 2) renders in assets/art and writes the
app-ready, size-budgeted versions into src/HappyCooking.App/Assets/Images:

  * icons:      white background keyed out (edge flood fill), 192 px PNG
  * pets:       keyed out, 256 px PNG
  * logo:       keyed out, trimmed, 900 px wide PNG
  * world map:  1600 px JPEG, plus one cropped card per region
  * portraits:  one square crop per character from the lineup illustration

Usage (repo root):  python tools/art/prepare_art.py
"""
from collections import deque
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "assets" / "art"
OUT = ROOT / "src" / "HappyCooking.App" / "Assets" / "Images"

# Crops on the 1376x768 world map: region id -> (left, top, right, bottom)
REGION_CROPS = {
    "nusantara": (100, 20, 620, 300),
    "sakura": (40, 240, 440, 560),
    "pasta": (470, 190, 940, 500),
    "dragonwok": (870, 30, 1340, 390),
    "fiesta": (210, 480, 740, 760),
    "dessert": (800, 390, 1340, 710),
}

# Square crops on the 1376x768 character lineup: id -> (centre x, centre y, half size)
PORTRAITS = {
    "chef_happy": (165, 330, 150),
    "coco_panda": (385, 400, 130),
    "jenggo_chicken": (600, 420, 130),
    "mochi_cat": (825, 440, 125),
    "pepper_bot": (1020, 480, 115),
    "burnt_onion": (1220, 470, 125),
}


def key_out_white(img: Image.Image, threshold: int = 232) -> Image.Image:
    """Makes the near-white background transparent, flood filling from the
    border so white highlights inside the subject survive."""
    img = img.convert("RGBA")
    w, h = img.size
    px = img.load()
    seen = bytearray(w * h)
    q = deque()
    for x in range(w):
        q.append((x, 0))
        q.append((x, h - 1))
    for y in range(h):
        q.append((0, y))
        q.append((w - 1, y))

    def is_bg(p):
        r, g, b, _ = p
        return r >= threshold and g >= threshold and b >= threshold

    mask = Image.new("L", (w, h), 255)
    mp = mask.load()
    while q:
        x, y = q.popleft()
        i = y * w + x
        if seen[i]:
            continue
        seen[i] = 1
        if not is_bg(px[x, y]):
            continue
        mp[x, y] = 0
        if x > 0:
            q.append((x - 1, y))
        if x < w - 1:
            q.append((x + 1, y))
        if y > 0:
            q.append((x, y - 1))
        if y < h - 1:
            q.append((x, y + 1))
    # Soften the cut so edges do not alias against coloured backgrounds.
    mask = mask.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(1.2))
    img.putalpha(mask)
    return img


def trim(img: Image.Image, pad: int = 8) -> Image.Image:
    box = img.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox()
    if not box:
        return img
    l, t, r, b = box
    return img.crop((max(0, l - pad), max(0, t - pad), min(img.width, r + pad), min(img.height, b + pad)))


def fit_square(img: Image.Image, size: int) -> Image.Image:
    img = trim(img)
    side = max(img.size)
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.paste(img, ((side - img.width) // 2, (side - img.height) // 2))
    return canvas.resize((size, size), Image.LANCZOS)


def main():
    (OUT / "Icons").mkdir(parents=True, exist_ok=True)
    (OUT / "Regions").mkdir(parents=True, exist_ok=True)
    (OUT / "Portraits").mkdir(parents=True, exist_ok=True)
    (OUT / "Pets").mkdir(parents=True, exist_ok=True)

    for icon in sorted((SRC / "icons").glob("*.png")):
        fit_square(key_out_white(Image.open(icon)), 192).save(OUT / "Icons" / icon.name, optimize=True)
        print("icon", icon.stem)

    for pet in sorted((SRC / "pets").glob("*.png")):
        fit_square(key_out_white(Image.open(pet)), 256).save(OUT / "Pets" / pet.name, optimize=True)
        print("pet", pet.stem)

    logo = trim(key_out_white(Image.open(SRC / "logo.png"), threshold=240))
    logo.resize((900, round(900 * logo.height / logo.width)), Image.LANCZOS).save(OUT / "logo.png", optimize=True)
    print("logo")

    world = Image.open(SRC / "world_map.png").convert("RGB")
    world.resize((1600, round(1600 * world.height / world.width)), Image.LANCZOS).save(
        OUT / "world_map.jpg", quality=86, optimize=True)
    for region, box in REGION_CROPS.items():
        crop = world.crop(box)
        crop.thumbnail((560, 360), Image.LANCZOS)
        crop.save(OUT / "Regions" / f"{region}.jpg", quality=86, optimize=True)
        print("region", region)

    lineup = Image.open(SRC / "characters.png").convert("RGB")
    lineup.resize((1280, round(1280 * lineup.height / lineup.width)), Image.LANCZOS).save(
        OUT / "characters.jpg", quality=86, optimize=True)
    for cid, (cx, cy, half) in PORTRAITS.items():
        lineup.crop((cx - half, cy - half, cx + half, cy + half)).resize((256, 256), Image.LANCZOS).save(
            OUT / "Portraits" / f"{cid}.jpg", quality=88, optimize=True)
        print("portrait", cid)


if __name__ == "__main__":
    main()
