"""Painted materials for the art pack: tileable, physically based (colour, normal, roughness), made with numpy.
Every map wraps at its edges, so it can repeat across a wall. Sizes are in metres per repeat."""
import numpy as np

N = 512


def fbm(beta, seed, n=N):
    """Tileable 1/f noise in 0..1 (it wraps because it is built in frequency space)."""
    rng = np.random.default_rng(seed)
    f = np.fft.fftfreq(n)
    fx, fy = np.meshgrid(f, f)
    r = np.sqrt(fx ** 2 + fy ** 2)
    r[0, 0] = 1
    spec = (rng.normal(size=(n, n)) + 1j * rng.normal(size=(n, n))) / r ** beta
    spec[0, 0] = 0
    x = np.fft.ifft2(spec).real
    return (x - x.min()) / (x.max() - x.min())


def blur(a, k):
    """Wrapping box blur, k pixels each way."""
    if k <= 0:
        return a
    for axis in (0, 1):
        out = np.zeros_like(a)
        for shift in range(-k, k + 1):
            out += np.roll(a, shift, axis)
        a = out / (2 * k + 1)
    return a


def normal_map(height, strength):
    dx = np.roll(height, -1, 1) - np.roll(height, 1, 1)
    dy = np.roll(height, -1, 0) - np.roll(height, 1, 0)
    n = np.dstack([-dx * strength, -dy * strength, np.ones_like(height)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n * 0.5 + 0.5


def rgb(hexcolor):
    return np.array([(hexcolor >> 16 & 255), (hexcolor >> 8 & 255), (hexcolor & 255)]) / 255.0


def mix(a, b, t):
    t = np.clip(t, 0, 1)[..., None]
    return a * (1 - t) + b * t


def cells(count, seed, n=N):
    """Tileable Voronoi: distance to the nearest and second-nearest point, and the nearest point's id."""
    rng = np.random.default_rng(seed)
    pts = rng.random((count, 2)) * n
    yy, xx = np.mgrid[0:n, 0:n]
    d1 = np.full((n, n), 1e9); d2 = np.full((n, n), 1e9); ident = np.zeros((n, n), int)
    for i, (px, py) in enumerate(pts):
        dx = np.abs(xx - px); dx = np.minimum(dx, n - dx)
        dy = np.abs(yy - py); dy = np.minimum(dy, n - dy)
        d = np.sqrt(dx * dx + dy * dy)
        closer = d < d1
        d2 = np.where(closer, d1, np.minimum(d2, d)); ident = np.where(closer, i, ident); d1 = np.where(closer, d, d1)
    return d1, d2, ident, count


def plaster():
    """Lime plaster: fine grain, soft trowel swells, rain streaks and patches of wear. Neutral, so a material tints it."""
    fine, swell, stain = fbm(0.6, 1), fbm(2.2, 2), fbm(1.8, 3)
    streak = blur(np.tile(fbm(1.2, 4)[:1], (N, 1)) * fbm(1.5, 5), 2)
    worn = np.clip((fbm(2.0, 6) - 0.62) * 9, 0, 1)
    col = mix(np.full((N, N, 3), 0.93) * np.array([1, 0.985, 0.95]), np.full((N, N, 3), 0.62) * np.array([1, 0.93, 0.82]), stain * 0.28 + streak * 0.4)
    col = mix(col, np.full((N, N, 3), 0.72) * np.array([0.95, 0.9, 0.85]), worn * 0.8)
    col *= (0.94 + fine * 0.1)[..., None]
    height = swell * 0.5 + fine * 0.18 - worn * 0.35
    rough = 0.86 + fine * 0.1 - worn * 0.05
    return col, normal_map(height, 5), rough


def brick():
    """Opus latericium: thin Roman bricks with thick lime mortar. One repeat is 1.2 m: 16 courses, bricks about 30 cm long."""
    yy, xx = np.mgrid[0:N, 0:N]
    course = (yy * 16 // N)
    off = (course % 2) * (N // 8)
    u = ((xx + off) % (N // 4)) / (N // 4); v = (yy % (N // 16)) / (N // 16)
    joint = np.clip(np.minimum(np.minimum(u, 1 - u) * (N // 4), np.minimum(v, 1 - v) * (N // 16)) / 3.2, 0, 1)
    ident = course * 7 + ((xx + off) // (N // 4))
    rng = np.random.default_rng(11); tone = rng.random(200)[ident % 200]
    base = mix(np.tile(rgb(0xb5623f), (N, N, 1)), np.tile(rgb(0xd08a5a), (N, N, 1)), tone)
    base = mix(base, np.tile(rgb(0x8f4a34), (N, N, 1)), (tone > 0.8) * 0.7)
    col = mix(np.tile(rgb(0xd8cdb8), (N, N, 1)), base, joint ** 0.6) * (0.9 + fbm(0.8, 12) * 0.2)[..., None]
    height = joint * 0.8 + fbm(1.0, 13) * 0.25
    return col, normal_map(height, 7), 0.9 - joint * 0.08 + fbm(0.5, 14) * 0.06


def travertine():
    """Travertine ashlar: warm white stone with horizontal pitted bands, in blocks about 60 cm high and 1.2 m long."""
    band = blur(np.tile(fbm(1.4, 21)[:, :1], (1, N)), 1) * 0.6 + fbm(1.9, 22) * 0.4
    pits = np.clip((fbm(0.2, 23) - 0.78) * 14, 0, 1) * (band > 0.45)
    yy, xx = np.mgrid[0:N, 0:N]
    course = yy * 4 // N; off = (course % 2) * (N // 4)
    u = ((xx + off) % (N // 2)) / (N // 2); v = (yy % (N // 4)) / (N // 4)
    joint = np.clip(np.minimum(np.minimum(u, 1 - u) * (N // 2), np.minimum(v, 1 - v) * (N // 4)) / 2.2, 0, 1)
    col = mix(np.tile(rgb(0xe9dfc9), (N, N, 1)), np.tile(rgb(0xc9b894), (N, N, 1)), band * 0.8)
    col = mix(col, np.tile(rgb(0x6f6250), (N, N, 1)), pits * 0.7 + (1 - joint) * 0.75)
    height = joint * 0.7 - pits * 0.5 + band * 0.12
    return col, normal_map(height, 6), 0.7 + pits * 0.2 + fbm(0.6, 24) * 0.08


def marble():
    """White marble with grey veining, lightly weathered (for columns, entablatures and steps)."""
    warp = fbm(2.2, 31)
    vein = np.abs(np.sin((np.mgrid[0:N, 0:N][1] / N * 6 + warp * 9 + fbm(1.6, 32) * 3) * np.pi)) ** 14
    vein2 = np.abs(np.sin((np.mgrid[0:N, 0:N][0] / N * 4 + fbm(2.0, 33) * 7) * np.pi)) ** 22
    soot = fbm(2.0, 34)
    col = mix(np.tile(rgb(0xf2eee4), (N, N, 1)), np.tile(rgb(0x9d9a94), (N, N, 1)), vein * 0.55 + vein2 * 0.4)
    col = mix(col, np.tile(rgb(0xcfc3a8), (N, N, 1)), np.clip(soot - 0.45, 0, 1) * 0.9)
    return col, normal_map(fbm(1.2, 35) * 0.2 - vein * 0.05, 3), 0.42 + soot * 0.25


def basalt():
    """Roman street paving: big irregular polygons of dark basalt, worn round on top, tight dark joints. Repeat 3 m."""
    d1, d2, ident, count = cells(30, 41)
    edge = np.clip((d2 - d1) / 9.0, 0, 1)
    rng = np.random.default_rng(42); tone = rng.random(count)[ident]
    stone = mix(np.tile(rgb(0x4b4a4c), (N, N, 1)), np.tile(rgb(0x77736e), (N, N, 1)), tone * 0.8 + fbm(1.2, 43) * 0.3)
    col = mix(np.tile(rgb(0x2a2622), (N, N, 1)), stone, edge ** 0.5)
    col = mix(col, np.tile(rgb(0x8a7f6c), (N, N, 1)), np.clip(fbm(1.8, 44) - 0.6, 0, 1) * 1.2 * edge)     # dust in the hollows
    height = np.sqrt(edge) * 0.9 + fbm(0.9, 45) * 0.15
    return col, normal_map(height, 4.5), 0.62 + (1 - edge) * 0.3 - tone * 0.12


def rooftile():
    """Tegulae and imbrices: flat pans with half-round cover tiles over each joint, lapped down the slope. Repeat 1.8 m (4 x 4)."""
    yy, xx = np.mgrid[0:N, 0:N]
    u = (xx % (N // 4)) / (N // 4); v = (yy % (N // 4)) / (N // 4)
    cover = np.clip(1 - (np.abs(u - 0.5) / 0.19) ** 2, 0, 1) ** 0.5        # the imbrex over the joint
    lap = v                                                                 # each tile rises to its upper edge
    ident = (xx // (N // 4)) + 4 * (yy // (N // 4))
    rng = np.random.default_rng(51); tone = rng.random(16)[ident % 16]
    col = mix(np.tile(rgb(0xb65a34), (N, N, 1)), np.tile(rgb(0xd88a55), (N, N, 1)), tone * 0.7 + fbm(1.4, 52) * 0.4)
    col = mix(col, np.tile(rgb(0x6f5a3a), (N, N, 1)), np.clip(fbm(1.9, 53) - 0.55, 0, 1) * 1.6)          # lichen and soot
    col *= (0.72 + 0.28 * np.clip(v * 6, 0, 1))[..., None] * (0.85 + cover * 0.15)[..., None]            # shadow under each lap
    height = cover * 1.0 + lap * 0.35
    return col, normal_map(height, 10), 0.8 + fbm(0.7, 54) * 0.12


def wood():
    """Sawn timber planks, grain running along v. Neutral brown; a material tints it for painted shutters."""
    yy, xx = np.mgrid[0:N, 0:N]
    plank = xx // (N // 8)
    rng = np.random.default_rng(61); tone = rng.random(8)[plank % 8]
    grain = blur(np.tile(fbm(0.9, 62)[:1], (N, 1)), 0) * 0.5 + np.sin((xx / N * 80 + fbm(2.2, 63) * 14)) * 0.25 + 0.25
    gap = np.clip(np.minimum(xx % (N // 8), (N // 8) - xx % (N // 8)) / 2.0, 0, 1)
    col = mix(np.tile(rgb(0x8a6844), (N, N, 1)), np.tile(rgb(0x5d4329), (N, N, 1)), grain * 0.7 + tone * 0.35)
    col = mix(np.tile(rgb(0x241a10), (N, N, 1)), col, gap)
    return col, normal_map(gap * 0.6 + grain * 0.25, 6), 0.78 + grain * 0.12


def terracotta():
    """Fired clay with wheel marks (amphorae, storage jars)."""
    rings = np.sin(np.mgrid[0:N, 0:N][0] / N * 2 * np.pi * 28) * 0.5 + 0.5
    col = mix(np.tile(rgb(0xc47a4a), (N, N, 1)), np.tile(rgb(0x9a5a36), (N, N, 1)), fbm(1.6, 71) * 0.9)
    col = mix(col, np.tile(rgb(0xd9c7a8), (N, N, 1)), np.clip(fbm(2.0, 72) - 0.62, 0, 1) * 1.8)          # salt bloom
    return col, normal_map(rings * 0.25 + fbm(0.8, 73) * 0.2, 4), 0.8 + fbm(0.6, 74) * 0.1


def bronze():
    """Bronze with verdigris in the hollows (doors, statues, fittings)."""
    patina = np.clip(fbm(1.8, 81) - 0.45, 0, 1) * 1.8
    col = mix(np.tile(rgb(0x8f6a32), (N, N, 1)), np.tile(rgb(0x4f8a72), (N, N, 1)), patina)
    return col, normal_map(fbm(1.0, 82) * 0.3, 3), 0.35 + patina * 0.45


ALL = {'plaster': (plaster, 3.0, 0.0), 'brick': (brick, 1.2, 0.0), 'travertine': (travertine, 2.4, 0.0), 'marble': (marble, 3.0, 0.0),
       'basalt': (basalt, 3.0, 0.0), 'rooftile': (rooftile, 1.8, 0.0), 'wood': (wood, 1.5, 0.0), 'terracotta': (terracotta, 1.0, 0.0),
       'bronze': (bronze, 1.5, 1.0)}
