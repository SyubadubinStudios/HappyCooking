"""
Converts the PNG screenshots written by the screenshot tour into compact JPEGs
for the README and docs (about 10x smaller), then removes the PNGs.

Usage (repo root):
  dotnet run --project src/HappyCooking.Desktop -- --screenshots docs/screenshots
  python tools/art/optimize_screenshots.py docs/screenshots
"""
import sys
from pathlib import Path

from PIL import Image

folder = Path(sys.argv[1] if len(sys.argv) > 1 else "docs/screenshots")
for png in sorted(folder.glob("*.png")):
    Image.open(png).convert("RGB").save(png.with_suffix(".jpg"), quality=86, optimize=True, progressive=True)
    png.unlink()
    print("optimised", png.stem)
