#!/usr/bin/env python3
"""
MOHINI — Texture generation pipeline.
Derives the face albedo from the authoritative reference image (lighting neutralised)
and procedurally authors skin / dress / hair PBR maps.

Outputs to ../assets/mohini/textures/
"""
import os
import numpy as np
from PIL import Image, ImageFilter, ImageEnhance

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REF = "/Users/shivashant/Library/Application Support/Tendedero/Screenshots/Clipboard 2026-11-20 at 21.26.36.png"
OUT = os.path.join(ROOT, "assets", "mohini", "textures")
os.makedirs(OUT, exist_ok=True)


# ---------------------------------------------------------------------------
# noise helpers
# ---------------------------------------------------------------------------
def value_noise(shape, cells, seed=0):
    """Smooth value noise over a 2D grid using PIL bicubic upsampling."""
    h, w = shape
    rng = np.random.RandomState(seed)
    ch, cw = max(2, int(cells * h / max(h, w))), max(2, int(cells))
    grid = rng.rand(ch, cw).astype(np.float32)
    im = Image.fromarray((grid * 255).astype(np.uint8), "L").resize((w, h), Image.BICUBIC)
    return np.asarray(im, dtype=np.float32) / 255.0


def fbm(shape, seed=0, octaves=(1, 4, 16, 64, 256)):
    out = np.zeros(shape, dtype=np.float32)
    total = 0.0
    amp = 1.0
    for i, c in enumerate(octaves):
        out += amp * value_noise(shape, c, seed + i * 13)
        total += amp
        amp *= 0.5
    return out / total


def save(arr, name):
    a = np.clip(arr * 255.0, 0, 255).astype(np.uint8)
    Image.fromarray(a).save(os.path.join(OUT, name))
    print("  wrote", name, a.shape)


def normal_from_height(height, strength=2.5):
    """Convert a height map [0,1] into a tangent-space normal map RGB."""
    gy, gx = np.gradient(height.astype(np.float32))
    nx = -gx * strength
    ny = -gy * strength
    nz = np.ones_like(height, dtype=np.float32)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz) + 1e-8
    return np.stack([(nx / ln) * 0.5 + 0.5, (ny / ln) * 0.5 + 0.5, (nz / ln) * 0.5 + 0.5], axis=-1)


# ---------------------------------------------------------------------------
# 1. FACE — derived from the reference
# ---------------------------------------------------------------------------
def build_face():
    print("FACE")
    im = Image.open(REF).convert("RGB")
    # Tight crop around head/face (from analysis of the 1597x2048 reference).
    crop = im.crop((628, 152, 968, 566))  # 340 x 414
    crop = crop.resize((680, 828), Image.LANCZOS)
    a = np.asarray(crop, dtype=np.float32) / 255.0

    L = a[..., 0] * 0.3 + a[..., 1] * 0.59 + a[..., 2] * 0.11

    # Lighting neutralisation: lift midtones/shadows but keep the deepest eye
    # sockets dark. A soft knee avoids blowing out the pale forehead.
    lift = np.power(np.clip(L, 0, 1), 0.62)
    knee = np.clip((L - 0.015) / 0.22, 0, 1)
    knee = knee * knee * (3 - 2 * knee)
    newL = L * (1 - knee) + lift * knee
    newL = np.clip(newL * 0.96 + 0.03, 0, 1)
    scale = np.where(L > 0.005, newL / (L + 1e-6), 1.0)
    scale = np.clip(scale, 0.6, 3.2)
    albedo = np.clip(a * scale[..., None], 0, 1)

    # Shift the sickly green cast toward neutral ashen skin while keeping it pale.
    albedo[..., 0] = np.clip(albedo[..., 0] * 1.03 + 0.012, 0, 1)
    albedo[..., 1] = np.clip(albedo[..., 1] * 0.985 + 0.004, 0, 1)
    albedo[..., 2] = np.clip(albedo[..., 2] * 0.985 + 0.008, 0, 1)

    # Skin micro detail: pores + fine mottling, masked to the pale skin only.
    h, w = L.shape
    pores_n = fbm((h, w), seed=11, octaves=(2, 8, 32, 128, 512))
    fine = np.random.RandomState(7).rand(h, w).astype(np.float32)
    fine = np.asarray(Image.fromarray((fine * 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(0.4)), dtype=np.float32) / 255.0
    skinmask = np.clip((L - 0.06) / 0.25, 0, 1) * (1 - np.clip((L - 0.85) / 0.15, 0, 1))
    pore_amt = (((pores_n - 0.5) * 0.055 + (fine - 0.5) * 0.03))[..., None] * skinmask[..., None]
    albedo = np.clip(albedo + pore_amt, 0, 1)

    # soft elliptical alpha so the flat projected face card dissolves into the
    # 3D hair behind it instead of showing a hard rectangular boundary.
    vv, uu = np.mgrid[0:h, 0:w].astype(np.float32)
    uu /= (w - 1); vv = 1.0 - vv / (h - 1)
    rr = np.sqrt(((uu - 0.48) / 0.46) ** 2 + ((vv - 0.52) / 0.48) ** 2)
    alpha = np.clip((1.0 - rr) / 0.10, 0, 1)
    rgba = np.concatenate([albedo, alpha[..., None]], axis=-1)
    save(rgba, "TEX_Mohini_Face_Albedo.png")

    # Height / normal: blurred luminance gives the big forms, pores give detail.
    base = np.asarray(Image.fromarray((L * 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(2.2)), dtype=np.float32) / 255.0
    height = base * 0.7 + pores_n * 0.22 + fine * 0.08
    height = height[..., None] * skinmask[..., None] + 0.5 * (1 - skinmask[..., None])
    height = height[..., 0]
    nm = normal_from_height(height, strength=3.6)
    save(nm, "TEX_Mohini_Face_Normal.png")

    # Roughness: skin is mostly matte with slight shine on nose/cheek highs.
    rough = np.clip(0.72 - (newL - 0.45) * 0.25 + (pores_n - 0.5) * 0.12, 0.45, 0.95)
    save(np.stack([np.zeros_like(rough), rough, np.zeros_like(rough)], -1), "TEX_Mohini_Face_ORM.png")


# ---------------------------------------------------------------------------
# 2. BODY SKIN — pale, damaged, ashen
# ---------------------------------------------------------------------------
def build_skin():
    print("SKIN")
    S = 1024
    h = w = S
    n1 = fbm((h, w), seed=101, octaves=(1, 3, 8, 24, 96))
    n2 = fbm((h, w), seed=202, octaves=(2, 6, 16, 64, 256))
    pores = np.random.RandomState(3).rand(h, w).astype(np.float32)
    pores = np.asarray(Image.fromarray((pores * 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(0.5)), dtype=np.float32) / 255.0

    base = np.array([0.36, 0.35, 0.34], dtype=np.float32)  # pale ashen, darkened for in-engine exposure
    albedo = np.zeros((h, w, 3), dtype=np.float32)
    albedo[:] = base
    # mottled pigmentation
    mott = (n1 - 0.5) * 0.16 + (n2 - 0.5) * 0.06
    albedo += mott[..., None] * np.array([1.0, 0.96, 0.92], dtype=np.float32)
    # unhealthy grey-green patches
    patch = np.clip((n1 - 0.58) * 3.0, 0, 1) * 0.10
    albedo[..., 0] -= patch * 0.5
    albedo[..., 1] -= patch * 0.15
    albedo[..., 2] -= patch * 0.35
    # veins / discoloration lines
    veinmask = np.clip((n2 - 0.52) * 8, 0, 1) * np.clip((0.62 - n1) * 6, 0, 1) * 0.16
    albedo[..., 0] -= veinmask * 0.45
    albedo[..., 1] -= veinmask * 0.55
    albedo[..., 2] -= veinmask * 0.20
    # faint old blood / bruise spots
    rng = np.random.RandomState(55)
    bruises = np.zeros((h, w), dtype=np.float32)
    for _ in range(22):
        cx, cy = rng.randint(0, w), rng.randint(0, h)
        r = rng.randint(8, 46)
        yy, xx = np.ogrid[:h, :w]
        d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / r
        bruises += np.clip(1 - d, 0, 1) ** 2 * rng.uniform(0.12, 0.4)
    bruises = np.clip(bruises, 0, 1)
    albedo[..., 0] -= bruises * 0.10
    albedo[..., 1] -= bruises * 0.16
    albedo[..., 2] -= bruises * 0.16

    albedo += ((pores - 0.5) * 0.035)[..., None]
    albedo = np.clip(albedo, 0.02, 1)
    save(albedo, "TEX_Mohini_Skin_Albedo.png")

    height = n1 * 0.45 + pores * 0.15 + bruises * 0.08
    save(normal_from_height(height, strength=2.2), "TEX_Mohini_Skin_Normal.png")

    rough = np.clip(0.66 + (n1 - 0.5) * 0.22 + bruises * 0.1, 0.45, 0.92)
    save(np.stack([np.zeros_like(rough), rough, np.zeros_like(rough)], -1), "TEX_Mohini_Skin_ORM.png")


# ---------------------------------------------------------------------------
# 3. DRESS — old, dark, worn charcoal fabric
# ---------------------------------------------------------------------------
def build_dress():
    print("DRESS")
    S = 1024
    h = w = S
    n1 = fbm((h, w), seed=301, octaves=(1, 4, 12, 48))
    n2 = fbm((h, w), seed=302, octaves=(2, 8, 24, 96, 384))

    base = np.array([0.115, 0.112, 0.104], dtype=np.float32)
    albedo = np.zeros((h, w, 3), dtype=np.float32)
    albedo[:] = base
    # woven cross-hatch
    yy, xx = np.mgrid[0:h, 0:w]
    weave = (np.sin(xx * 2.1) * np.sin(yy * 2.1)) * 0.012 + (np.sin(xx * 0.5) + np.sin(yy * 0.5)) * 0.006
    albedo += weave[..., None]
    # large folds + discoloration
    fold = (n1 - 0.5) * 0.07 + (n2 - 0.5) * 0.025
    albedo += fold[..., None] * np.array([1.0, 0.98, 0.92], dtype=np.float32)
    # faded / worn patches
    worn = np.clip((n2 - 0.55) * 3.5, 0, 1) * 0.06
    albedo += worn[..., None] * np.array([1.0, 0.95, 0.85], dtype=np.float32)
    albedo = np.clip(albedo, 0.02, 0.4)
    save(albedo, "TEX_Mohini_Shroud_Albedo.png")

    height = 0.5 + weave * 6 + (n1 - 0.5) * 0.4 + (n2 - 0.5) * 0.2
    save(normal_from_height(height, strength=2.0), "TEX_Mohini_Shroud_Normal.png")

    ao = np.clip(0.85 + (n1 - 0.5) * 0.5, 0.4, 1.0)
    rough = np.clip(0.88 + (n2 - 0.5) * 0.12, 0.75, 1.0)
    save(np.stack([ao, rough, np.zeros_like(ao)], -1), "TEX_Mohini_Shroud_ORM.png")


# ---------------------------------------------------------------------------
# 4. HAIR — near-black strands with subtle variation
# ---------------------------------------------------------------------------
def build_hair():
    print("HAIR")
    h, w = 1024, 1024
    n = fbm((h, w), seed=401, octaves=(1, 4, 16, 64, 256))
    yy, xx = np.mgrid[0:h, 0:w]
    # vertical strand streaks
    strand = np.sin(xx * 0.9 + np.sin(yy * 0.01) * 3.0) * 0.5 + 0.5
    strand2 = np.sin(xx * 2.7 + np.sin(yy * 0.02) * 5.0) * 0.5 + 0.5
    v = (strand * 0.55 + strand2 * 0.25 + n * 0.2)
    base = 0.012 + v * 0.05
    albedo = np.zeros((h, w, 3), dtype=np.float32)
    albedo[..., 0] = base * 1.15
    albedo[..., 1] = base * 1.0
    albedo[..., 2] = base * 1.05
    save(np.clip(albedo, 0, 1), "TEX_Mohini_Hair_Albedo.png")

    height = 0.5 + (strand - 0.5) * 0.35 + (n - 0.5) * 0.2
    save(normal_from_height(height, strength=1.4), "TEX_Mohini_Hair_Normal.png")

    ao = np.clip(0.7 + (strand - 0.5) * 0.5, 0.35, 1.0)
    rough = np.clip(0.42 + (n - 0.5) * 0.2 + (strand2 - 0.5) * 0.1, 0.3, 0.7)
    save(np.stack([ao, rough, np.zeros_like(ao)], -1), "TEX_Mohini_Hair_ORM.png")


if __name__ == "__main__":
    build_face()
    build_skin()
    build_dress()
    build_hair()
    print("DONE ->", OUT)
