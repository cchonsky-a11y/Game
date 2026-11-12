"""Builds the art pack: Roman street architecture and props for AD 155, modelled in Blender by script and exported as one
glTF file (kit.glb) that the game loads. Run with Blender's Python:  python build_kit.py <out.glb> [preview.png]
Scale: 1 unit = 1 metre, Z up, fronts face -Y. Every asset sits on the ground at the origin. See art/README.md."""
import math
import sys
import os
import bpy
import bmesh
import numpy as np
from mathutils import Vector, Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import textures

OUT = sys.argv[1]
PREVIEW = sys.argv[2] if len(sys.argv) > 2 else None
bpy.ops.wm.read_factory_settings(use_empty=True)

# ---------------------------------------------------------------------------------------------------------------------
# Materials

import tempfile
TEXDIR = tempfile.mkdtemp(prefix='kit-textures-')
TILE = {}          # material name -> metres per texture repeat
IMAGES = {}


def image(name, array, colour):
    n = array.shape[0]
    img = bpy.data.images.new(name, n, n, alpha=False)
    rgba = np.ones((n, n, 4), np.float32)
    rgba[..., :3] = np.clip(array if array.ndim == 3 else np.repeat(array[..., None], 3, 2), 0, 1)
    img.pixels.foreach_set(rgba.ravel())
    img.update()
    # Written to disk and read back: a painted image only reaches the export reliably as a real file.
    path = os.path.join(TEXDIR, name + '.png')
    img.filepath_raw = path; img.file_format = 'PNG'; img.save()
    bpy.data.images.remove(img)
    img = bpy.data.images.load(path)
    img.name = name
    img.colorspace_settings.name = 'sRGB' if colour else 'Non-Color'
    img.pack()
    return img


def textured(base):
    if base not in IMAGES:
        paint, size, metal = textures.ALL[base]
        col, nrm, rough = paint()
        orm = np.dstack([np.ones_like(rough), rough, np.full_like(rough, metal)])      # occlusion, roughness, metal: the glTF packing
        IMAGES[base] = (image(base + '_colour', col, True), image(base + '_normal', nrm, False), image(base + '_orm', orm, False), size, metal)
    return IMAGES[base]


def material(name, base=None, tint=None, flat=None, rough=0.9, metal=0.0, emit=None):
    """A material named base#rrggbb: the game multiplies the painted texture by the tint after loading."""
    full = name if tint is None else '%s#%06x' % (name, tint)
    if full in bpy.data.materials:
        return bpy.data.materials[full]
    m = bpy.data.materials.new(full)
    m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes['Principled BSDF']
    if base:
        col, nrm, rgh, size, mtl = textured(base)
        TILE[full] = size
        t = nodes.new('ShaderNodeTexImage'); t.image = col; links.new(t.outputs['Color'], bsdf.inputs['Base Color'])
        n = nodes.new('ShaderNodeTexImage'); n.image = nrm
        nm = nodes.new('ShaderNodeNormalMap'); links.new(n.outputs['Color'], nm.inputs['Color']); links.new(nm.outputs['Normal'], bsdf.inputs['Normal'])
        r = nodes.new('ShaderNodeTexImage'); r.image = rgh
        sep = nodes.new('ShaderNodeSeparateColor'); links.new(r.outputs['Color'], sep.inputs['Color'])
        links.new(sep.outputs['Green'], bsdf.inputs['Roughness']); links.new(sep.outputs['Blue'], bsdf.inputs['Metallic'])
    else:
        TILE[full] = 1.0
        c = textures.rgb(flat)
        bsdf.inputs['Base Color'].default_value = (c[0] ** 2.2, c[1] ** 2.2, c[2] ** 2.2, 1)
        bsdf.inputs['Roughness'].default_value = rough
        bsdf.inputs['Metallic'].default_value = metal
        if emit is not None:
            e = textures.rgb(emit); bsdf.inputs['Emission Color'].default_value = (e[0] ** 2.2, e[1] ** 2.2, e[2] ** 2.2, 1); bsdf.inputs['Emission Strength'].default_value = 1.0
    return m


PLASTERS = [0xf0dcb8, 0xe8c79a, 0xf3e8d2, 0xe2b48e, 0xf0e0c0, 0xddcfb4]      # ochres, creams and a faded red, as on Roman street fronts
SHUTTERS = [0x5f7d5c, 0x7d4a3a, 0x3f6278, 0x7c6b40, 0x6a5444]


def M(kind, tint=None):
    table = {
        'plaster': lambda: material('plaster', 'plaster', tint or PLASTERS[0]),
        'dado': lambda: material('plaster', 'plaster', tint or 0x8f3626),
        'brick': lambda: material('brick', 'brick'),
        'travertine': lambda: material('travertine', 'travertine'),
        'marble': lambda: material('marble', 'marble'),
        'basalt': lambda: material('basalt', 'basalt'),
        'rooftile': lambda: material('rooftile', 'rooftile'),
        'wood': lambda: material('wood', 'wood', tint) if tint else material('wood', 'wood'),
        'terracotta': lambda: material('terracotta', 'terracotta'),
        'bronze': lambda: material('bronze', 'bronze'),
        'dark': lambda: material('interior', flat=0x17120e, rough=1.0),
        'lamp': lambda: material('lamplight', flat=0x3a2a18, rough=1.0, emit=0xffb45a),
        'lead': lambda: material('lead', flat=0x6f7478, rough=0.5, metal=0.8),
        'water': lambda: material('water', flat=0x3f86a8, rough=0.08),
        'leaf': lambda: material('leaf', flat=0x4f7a45, rough=1.0),
        'steel': lambda: material('steel', flat=0x5a646e, rough=0.42, metal=0.7),
        'scorch': lambda: material('scorch', flat=0x15120f, rough=0.9, metal=0.2),
        'coreglow': lambda: material('coreglow', flat=0x0a1a2a, rough=0.3, emit=0x5fb4ff),
        'seat': lambda: material('seat', flat=0x2a2622, rough=0.8),
        'cloth': lambda: material('cloth#%06x' % (tint or 0xa8482c), flat=tint or 0xa8482c, rough=1.0),
    }
    return table[kind]()


# ---------------------------------------------------------------------------------------------------------------------
# Geometry

class Builder:
    """Collects one asset's geometry; finish() bevels it, lays out UVs at real scale and makes the object."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.mats = []
        self.smooth = self.bm.faces.layers.int.new('smooth')

    def slot(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def face(self, pts, mat, smooth=False):
        vs = [self.bm.verts.new(p) for p in pts]
        f = self.bm.faces.new(vs)
        f.material_index = self.slot(mat); f[self.smooth] = 1 if smooth else 0
        return f

    def box(self, x0, x1, y0, y1, z0, z1, mat, rot=0.0, about=None):
        x0, x1 = sorted((x0, x1)); y0, y1 = sorted((y0, y1)); z0, z1 = sorted((z0, z1))
        c = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
        if rot:
            ax, ay = about if about else ((x0 + x1) / 2, (y0 + y1) / 2)
            cs, sn = math.cos(rot), math.sin(rot)
            c = [(ax + (x - ax) * cs - (y - ay) * sn, ay + (x - ax) * sn + (y - ay) * cs, z) for x, y, z in c]
        vs = [self.bm.verts.new(p) for p in c]
        idx = self.slot(mat)
        for q in ((0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)):
            f = self.bm.faces.new([vs[i] for i in q]); f.material_index = idx

    def lathe(self, profile, mat, cx=0.0, cy=0.0, seg=20, flutes=0, flute_depth=0.0, squash=1.0):
        """Turns a profile [(radius, z), ...] round the vertical axis. flutes cuts shallow channels (a fluted shaft)."""
        idx = self.slot(mat)
        rings = []
        for r, z in profile:
            ring = []
            for k in range(seg):
                a = 2 * math.pi * k / seg
                rr = r * (1 - flute_depth * (0.5 - 0.5 * math.cos(flutes * a))) if flutes else r
                ring.append(self.bm.verts.new((cx + math.cos(a) * rr, cy + math.sin(a) * rr * squash, z)))
            rings.append(ring)
        for a, b in zip(rings, rings[1:]):
            for k in range(seg):
                f = self.bm.faces.new([a[k], a[(k + 1) % seg], b[(k + 1) % seg], b[k]]); f.material_index = idx; f[self.smooth] = 1
        for ring, flip in ((rings[0], True), (rings[-1], False)):
            if abs(profile[0 if flip else -1][0]) > 1e-4:
                f = self.bm.faces.new(list(reversed(ring)) if flip else ring); f.material_index = idx

    def tube(self, pts, r, mat, seg=8):
        """A round bar swept along a line of points (handles, rails, pipes, ropes)."""
        idx = self.slot(mat)
        rings = []
        for i, p in enumerate(pts):
            p = Vector(p)
            d = (Vector(pts[min(i + 1, len(pts) - 1)]) - Vector(pts[max(i - 1, 0)])).normalized()
            side = d.cross(Vector((0, 0, 1)));
            if side.length < 1e-4: side = d.cross(Vector((1, 0, 0)))
            side.normalize(); up = side.cross(d).normalized()
            rings.append([self.bm.verts.new(p + (side * math.cos(2 * math.pi * k / seg) + up * math.sin(2 * math.pi * k / seg)) * r) for k in range(seg)])
        for a, b in zip(rings, rings[1:]):
            for k in range(seg):
                f = self.bm.faces.new([a[k], a[(k + 1) % seg], b[(k + 1) % seg], b[k]]); f.material_index = idx; f[self.smooth] = 1
        self.bm.faces.new(list(reversed(rings[0]))).material_index = idx
        self.bm.faces.new(rings[-1]).material_index = idx

    def prism(self, outline, y0, y1, mat):
        """A flat outline in x,z pushed through from y0 to y1 (pediments, gables, brackets)."""
        idx = self.slot(mat)
        a = [self.bm.verts.new((x, y0, z)) for x, z in outline]; b = [self.bm.verts.new((x, y1, z)) for x, z in outline]
        self.bm.faces.new(a).material_index = idx
        self.bm.faces.new(list(reversed(b))).material_index = idx
        n = len(outline)
        for k in range(n):
            self.bm.faces.new([a[(k + 1) % n], a[k], b[k], b[(k + 1) % n]]).material_index = idx

    def arch(self, cx, r, y0, y1, zs, ztop, x_lo, x_hi, mat, ring=None, seg=12):
        """A wall from zs up to ztop between x_lo and x_hi, pierced by a round arch of radius r springing at zs.
        `ring` is the material of the voussoirs, which stand a little proud of the wall."""
        idx = self.slot(mat)
        def quad(p, m=idx, smooth=False):
            f = self.bm.faces.new([self.bm.verts.new(q) for q in p]); f.material_index = m; f[self.smooth] = 1 if smooth else 0
        pts = [(cx + r * math.cos(math.pi - math.pi * i / seg), zs + r * math.sin(math.pi * i / seg)) for i in range(seg + 1)]
        for y in (y0, y1):
            quad([(x_lo, y, zs), (cx - r, y, zs), (cx - r, y, ztop), (x_lo, y, ztop)]); quad([(cx + r, y, zs), (x_hi, y, zs), (x_hi, y, ztop), (cx + r, y, ztop)])
            for (ax, az), (bx, bz) in zip(pts, pts[1:]):
                quad([(ax, y, az), (bx, y, bz), (bx, y, ztop), (ax, y, ztop)])
        for (ax, az), (bx, bz) in zip(pts, pts[1:]):
            quad([(ax, y0, az), (bx, y0, bz), (bx, y1, bz), (ax, y1, az)], smooth=True)                     # the soffit
        quad([(x_lo, y0, ztop), (x_hi, y0, ztop), (x_hi, y1, ztop), (x_lo, y1, ztop)])
        if ring:
            ridx, out, proud = self.slot(ring), r + 0.3, y0 - 0.05
            outer = [(cx + out * math.cos(math.pi - math.pi * i / seg), zs + out * math.sin(math.pi * i / seg)) for i in range(seg + 1)]
            for i in range(seg):
                quad([(pts[i][0], proud, pts[i][1]), (pts[i + 1][0], proud, pts[i + 1][1]), (outer[i + 1][0], proud, outer[i + 1][1]), (outer[i][0], proud, outer[i][1])], ridx)
                quad([(outer[i][0], proud, outer[i][1]), (outer[i + 1][0], proud, outer[i + 1][1]), (outer[i + 1][0], y0, outer[i + 1][1]), (outer[i][0], y0, outer[i][1])], ridx)
                quad([(pts[i][0], proud, pts[i][1]), (pts[i + 1][0], proud, pts[i + 1][1]), (pts[i + 1][0], y0, pts[i + 1][1]), (pts[i][0], y0, pts[i][1])], ridx)

    def cloth(self, corner, across, out, mat, sag=0.3, nu=7, nv=4):
        """A sheet hung from a wall and sagging between its supports: corner, then the vectors across and outward."""
        idx = self.slot(mat)
        c, a, o = Vector(corner), Vector(across), Vector(out)
        grid = [[self.bm.verts.new(c + a * (u / nu) + o * (v / nv) - Vector((0, 0, sag * math.sin(math.pi * u / nu) * (0.35 + 0.65 * v / nv)))) for u in range(nu + 1)] for v in range(nv + 1)]
        for v in range(nv):
            for u in range(nu):
                f = self.bm.faces.new([grid[v][u], grid[v][u + 1], grid[v + 1][u + 1], grid[v + 1][u]]); f.material_index = idx; f[self.smooth] = 1

    def finish(self, bevel=0.018, footprint=None, wobble=0.0, lean=(0.0, 0.0), shade=True):
        self.bm.normal_update()
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        me = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(me); self.bm.free()
        ob = bpy.data.objects.new(self.name, me)
        bpy.context.scene.collection.objects.link(ob)
        for m in self.mats:
            me.materials.append(m)
        if bevel:
            mod = ob.modifiers.new('bevel', 'BEVEL'); mod.width = bevel; mod.segments = 1; mod.limit_method = 'ANGLE'; mod.angle_limit = math.radians(50); mod.harden_normals = False
            dg = bpy.context.evaluated_depsgraph_get()
            new = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
            ob.modifiers.clear(); ob.data = new; bpy.data.meshes.remove(me); me = new; me.name = self.name
        # Nothing hand-built is perfectly straight: a little wander in every wall, and the whole building a touch out of true.
        if wobble or lean != (0.0, 0.0):
            for v in me.vertices:
                x, y, z = v.co
                n = lambda k: math.sin(x * 1.7 + k * 2.1) * math.cos(y * 1.3 - k) * math.sin(z * 0.9 + k * 0.7)
                v.co = (x + wobble * n(1) + z * lean[0], y + wobble * n(2) + z * lean[1], z + wobble * 0.5 * n(3) * (1 if z > 0.3 else 0))
        # Real-scale UVs: each face is mapped flat along its dominant axis, so a texture repeat is the same size everywhere.
        uv = me.uv_layers.new(name='UVMap') if not me.uv_layers else me.uv_layers[0]
        smooth = me.attributes.get('smooth')
        for poly in me.polygons:
            size = TILE.get(me.materials[poly.material_index].name, 1.0)
            n = poly.normal
            ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
            for li in poly.loop_indices:
                v = me.vertices[me.loops[li].vertex_index].co
                if az >= ax and az >= ay: u_, v_ = v.x, v.y
                elif ax >= ay: u_, v_ = v.y, v.z
                else: u_, v_ = v.x, v.z
                uv.data[li].uv = (u_ / size, v_ / size)
            poly.use_smooth = bool(smooth.data[poly.index].value) if smooth else False
        if footprint:
            ob['hw'], ob['hd'] = footprint
        if shade:
            bake_shade(ob)
        return ob


def bake_shade(ob):
    """Bakes how much sky each corner sees into the mesh's colours (ambient occlusion), so recesses, eaves and the foot of
    a wall are darker without any light in the game having to work for it."""
    me = ob.data
    col = me.color_attributes.new('Col', 'BYTE_COLOR', 'CORNER')
    me.color_attributes.active_color = col; me.color_attributes.render_color_index = me.color_attributes.find('Col')
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'; sc.cycles.samples = 24; sc.cycles.device = 'CPU'
    for other in sc.objects:
        other.hide_render = other is not ob
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    try:
        bpy.ops.object.bake(type='AO', target='VERTEX_COLORS')
    except Exception as e:                                           # never let shading stop the build
        print('SHADE-FAILED', ob.name, e)
        for d in col.data: d.color = (1, 1, 1, 1)
    arr = np.empty(len(col.data) * 4, np.float32); col.data.foreach_get('color', arr)
    arr = arr.reshape(-1, 4)
    open_sky = max(0.05, float(np.percentile(arr[:, 0], 85)))        # what an open wall sees counts as fully lit
    arr[:, :3] = np.clip((arr[:, :3] / open_sky) ** 1.15, 0.5, 1.0); arr[:, 3] = 1
    col.data.foreach_set('color', arr.ravel())
    for other in sc.objects:
        other.hide_render = False


# ---------------------------------------------------------------------------------------------------------------------
# Tenements (insulae)

BAY, GROUND, FLOOR = 3.2, 4.0, 3.0


def insula(name, bays, floors, depth, seed):
    """A Roman apartment block: shops (tabernae) open to the street, some under brick arches and some under a timber
    lintel, a mezzanine over each, plastered flats above that may jut out over the street on beams, shuttered windows,
    timber balconies, cloth awnings, and a low roof of pan tiles with half-round covers."""
    rng = np.random.default_rng(seed)
    b = Builder(name)
    w = bays * BAY
    x0, yf, yb = -w / 2, -depth / 2, depth / 2
    jetty = 0.5 if seed % 3 == 1 else 0.0                      # the flats overhang the street
    arched = seed % 2 == 0                                     # brick arches over the shops, or timber lintels
    yu = yf - jetty                                            # the face of the upper floors
    plaster = M('plaster', PLASTERS[seed % len(PLASTERS)]); dado = M('dado', [0x8f3626, 0x3b332c, 0x8f3626][seed % 3])
    shutter = M('wood', SHUTTERS[seed % len(SHUTTERS)]); wood = M('wood'); brick = M('brick'); trav = M('travertine'); dark = M('dark'); clay = M('terracotta')
    top = GROUND + (floors - 1) * FLOOR
    # The body: side and back walls, and dark rooms behind the openings.
    b.box(x0, x0 + 0.4, yf, yb, 0, top, plaster); b.box(-x0 - 0.4, -x0, yf, yb, 0, top, plaster); b.box(x0, -x0, yb - 0.4, yb, 0, top, plaster)
    b.box(x0 + 0.4, -x0 - 0.4, yf + 1.9, yf + 2.0, 0, GROUND, dark)
    b.box(x0 + 0.4, -x0 - 0.4, yf + 0.5, yb - 0.4, GROUND - 0.25, GROUND, dark)
    b.box(x0 + 0.4, -x0 - 0.4, yu + 0.45, yu + 0.5, GROUND, top, dark)
    b.box(x0, -x0, yu, yb, top - 0.05, top, plaster)
    if jetty:                                                  # the overhang: its floor, its cheeks, and the beam ends that carry it
        b.box(x0, -x0, yu, yf + 0.1, GROUND - 0.14, GROUND, wood)
        b.box(x0, x0 + 0.4, yu, yf, GROUND, top, plaster); b.box(-x0 - 0.4, -x0, yu, yf, GROUND, top, plaster)
        for bx in np.arange(x0 + 0.25, -x0, 0.8):
            b.box(bx - 0.09, bx + 0.09, yu - 0.12, yf + 0.3, GROUND - 0.36, GROUND - 0.14, wood)
    # Street level, bay by bay.
    for k in range(bays):
        bx = x0 + k * BAY
        for px in (bx, bx + BAY - 0.42):                                                 # brick piers on stone feet
            b.box(px, px + 0.42, yf - 0.04, yf + 0.5, 0.25, 3.4 if not arched else 1.95, brick)
            b.box(px - 0.04, px + 0.46, yf - 0.08, yf + 0.5, 0, 0.25, trav)
        door = rng.random() < 0.22 and bays > 1
        if arched and not door:
            b.box(bx - 0.03, bx + 0.45, yf - 0.07, yf + 0.5, 1.95, 2.07, trav); b.box(bx + BAY - 0.45, bx + BAY + 0.03, yf - 0.07, yf + 0.5, 1.95, 2.07, trav)   # imposts
            b.arch(bx + BAY / 2, BAY / 2 - 0.42, yf, yf + 0.45, 2.07, 3.4, bx, bx + BAY, plaster, ring=brick)
        else:
            if arched:
                for px in (bx, bx + BAY - 0.42):
                    b.box(px, px + 0.42, yf - 0.04, yf + 0.5, 1.95, 3.05, brick)
            b.box(bx, bx + BAY, yf - 0.1, yf + 0.55, 3.05, 3.4, wood)                      # the timber lintel
        b.box(bx, bx + BAY, yf, yf + 0.45, 3.4, GROUND, plaster)                           # the mezzanine wall
        b.box(bx + BAY / 2 - 0.4, bx + BAY / 2 + 0.4, yf - 0.02, yf + 0.02, 3.5, 3.85, dark)
        b.box(bx + BAY / 2 - 0.46, bx + BAY / 2 + 0.46, yf - 0.05, yf + 0.03, 3.44, 3.5, trav)
        if door:                                                                          # the stair door to the flats
            b.box(bx + 0.42, bx + 0.95, yf, yf + 0.45, 0, 3.05, plaster); b.box(bx + BAY - 0.95, bx + BAY - 0.42, yf, yf + 0.45, 0, 3.05, plaster)
            b.box(bx + 0.95, bx + BAY - 0.95, yf + 0.12, yf + 0.2, 0, 2.6, wood)
            b.box(bx + BAY / 2 - 0.02, bx + BAY / 2 + 0.02, yf + 0.1, yf + 0.13, 0, 2.6, dark)
            b.box(bx + 0.95, bx + BAY - 0.95, yf, yf + 0.45, 2.6, 3.05, plaster)
            b.box(bx + 0.85, bx + BAY - 0.85, yf - 0.06, yf + 0.3, -0.02, 0.14, trav)
            b.box(bx, bx + BAY, yf - 0.03, yf + 0.01, 0, 0.9, dado)
        else:
            b.box(bx + 0.42, bx + BAY - 0.42, yf - 0.08, yf + 0.4, -0.02, 0.12, trav)      # the grooved threshold
            if rng.random() < 0.55:                                                       # a masonry counter with jars set in it
                b.box(bx + 0.42, bx + 1.9, yf + 0.05, yf + 0.6, 0.12, 1.0, dado); b.box(bx + 0.4, bx + 1.94, yf + 0.0, yf + 0.64, 1.0, 1.07, trav)
                for cx in (bx + 0.8, bx + 1.45):
                    b.lathe([(0.2, 1.07), (0.2, 1.09), (0.15, 1.09)], clay, cx, yf + 0.32, 12)
            for j in range(3):                                                            # the shutter boards, stacked aside
                b.box(bx + BAY - 0.52 - j * 0.07, bx + BAY - 0.47 - j * 0.07, yf + 0.1, yf + 0.42, 0.14, 1.9 if arched else 2.95, shutter)
            if not jetty and rng.random() < 0.4:                                          # a cloth awning, sagging between its poles
                colour = [0xa8482c, 0x3f6077, 0xb08a3a, 0x6a7a4a][int(rng.integers(4))]
                b.cloth((bx + 0.3, yf - 0.05, 3.25), (BAY - 0.6, 0, 0), (0, -1.7, -0.75), M('cloth', colour), sag=0.28)
                for px in (bx + 0.3, bx + BAY - 0.3):
                    b.tube([(px, yf - 1.75, 0), (px, yf - 1.75, 2.52)], 0.035, wood, 6)
    # A patch where the plaster has fallen and the brick shows.
    for _ in range(1 + bays // 2):
        px = x0 + rng.random() * (w - 1.4); pz = GROUND + rng.random() * max(0.5, top - GROUND - 1.6)
        for j in range(3):
            b.box(px + j * 0.12, px + 1.0 - j * 0.18 + rng.random() * 0.2, yu - 0.012, yu + 0.01, pz + j * 0.28, pz + (j + 1) * 0.28, brick)
    # The flats: wall between the windows, sills, shutters folded back, balconies on brackets.
    for f in range(1, floors):
        z0 = GROUND + (f - 1) * FLOOR
        b.box(x0 - 0.06, -x0 + 0.06, yu - 0.07, yu + 0.1, z0 - 0.08, z0 + 0.08, trav)      # the string course
        for k in range(bays):
            bx = x0 + k * BAY
            balcony = rng.random() < 0.22
            round_head = seed % 2 == 1 and not balcony
            wl, wr, wb, wt = bx + BAY / 2 - 0.5, bx + BAY / 2 + 0.5, z0 + (0.15 if balcony else 0.95), z0 + (2.05 if round_head else 2.45)
            b.box(bx, wl, yu, yu + 0.45, z0, z0 + FLOOR, plaster); b.box(wr, bx + BAY, yu, yu + 0.45, z0, z0 + FLOOR, plaster)
            b.box(wl, wr, yu, yu + 0.45, z0, wb, plaster)
            if round_head:
                b.arch(bx + BAY / 2, 0.5, yu, yu + 0.45, wt, z0 + FLOOR, wl, wr, plaster, ring=brick, seg=8)
            else:
                b.box(wl, wr, yu, yu + 0.45, wt, z0 + FLOOR, plaster)
                b.box(wl - 0.08, wr + 0.08, yu - 0.05, yu + 0.3, wt, wt + 0.12, wood)      # lintel
            if not balcony:
                b.box(wl - 0.1, wr + 0.1, yu - 0.1, yu + 0.3, wb - 0.1, wb, trav)          # sill
                if rng.random() < 0.25:                                                   # a pot of herbs on the sill
                    b.lathe([(0.0, wb), (0.09, wb), (0.12, wb + 0.16), (0.1, wb + 0.17)], clay, wl + 0.2, yu - 0.02, 8)
                    b.lathe([(0.0, wb + 0.16), (0.14, wb + 0.2), (0.16, wb + 0.34), (0.0, wb + 0.42)], M('leaf'), wl + 0.2, yu - 0.02, 7)
            for sx, away in ((wl, -1), (wr, 1)):                                          # shutters, open against the wall
                ajar = (rng.random() - 0.5) * 0.5
                b.box(sx, sx + away * 0.5, yu - 0.06, yu - 0.02, wb, wt, shutter, rot=ajar * away, about=(sx, yu - 0.04))
            if rng.random() < 0.3:
                b.box(wl, wr, yu + 0.4, yu + 0.44, wb, wt, M('lamp'))                     # someone is home
            if balcony:
                b.box(bx + 0.25, bx + BAY - 0.25, yu - 1.0, yu, z0 + 0.05, z0 + 0.15, wood)
                for px in (bx + 0.4, bx + BAY - 0.4):
                    b.tube([(px, yu - 0.9, z0 + 0.05), (px, yu, z0 - 0.75)], 0.05, wood, 6)
                for px in np.linspace(bx + 0.3, bx + BAY - 0.3, 8):
                    b.box(px - 0.025, px + 0.025, yu - 0.98, yu - 0.93, z0 + 0.15, z0 + 1.0, wood)
                b.box(bx + 0.25, bx + BAY - 0.25, yu - 1.0, yu - 0.92, z0 + 1.0, z0 + 1.07, wood)
                for py in (yu - 0.6, yu - 0.3):
                    b.box(bx + 0.25, bx + 0.3, py - 0.02, py + 0.02, z0 + 0.15, z0 + 1.0, wood); b.box(bx + BAY - 0.3, bx + BAY - 0.25, py - 0.02, py + 0.02, z0 + 0.15, z0 + 1.0, wood)
                if rng.random() < 0.6:                                                    # washing over the rail
                    b.cloth((bx + 0.5, yu - 1.02, z0 + 1.08), (0.9, 0, 0), (0, -0.02, -0.7), M('cloth', [0xe8e0d0, 0x8a5a6a, 0x6a7a8a][int(rng.integers(3))]), sag=0.03, nu=3, nv=3)
    # Side windows, small and high, as on a lane.
    for f in range(1, floors):
        for sx, face in ((x0 - 0.02, -1), (-x0 + 0.02, 1)):
            for py in np.linspace(yf + 1.5, yb - 1.5, max(1, int(depth // 3.2))):
                z = GROUND + (f - 1) * FLOOR + 1.2
                b.box(sx - 0.02, sx + 0.02, py - 0.35, py + 0.35, z, z + 0.9, dark)
                b.box(sx - 0.04 if face < 0 else sx, sx if face < 0 else sx + 0.04, py - 0.42, py + 0.42, z - 0.08, z, trav)
    # A rainwater pipe of clay sections down one corner.
    b.tube([(x0 + 0.2, yu - 0.09, top), (x0 + 0.2, yu - 0.09, GROUND + 0.3)], 0.07, clay, 8)
    # The roof: low pitch, pan tiles, a half-round cover tile over every joint, rafter ends under the eaves.
    over = 0.65
    rise = math.tan(math.radians(19)) * (depth / 2 + over)
    ridge = (yu + yb) / 2
    tile = M('rooftile')
    b.face([(x0 - over, yu - over, top), (-x0 + over, yu - over, top), (-x0 + over, ridge, top + rise), (x0 - over, ridge, top + rise)], tile)
    b.face([(-x0 + over, yb + over, top), (x0 - over, yb + over, top), (x0 - over, ridge, top + rise), (-x0 + over, ridge, top + rise)], tile)
    b.face([(x0 - over, yu - over, top - 0.03), (x0 - over, yb + over, top - 0.03), (-x0 + over, yb + over, top - 0.03), (-x0 + over, yu - over, top - 0.03)], wood)
    for tx in np.arange(x0 - over + 0.22, -x0 + over, 0.45):
        b.tube([(tx, yu - over - 0.04, top + 0.05), (tx, ridge, top + rise + 0.05)], 0.075, tile, 6)
        b.tube([(tx, yb + over + 0.04, top + 0.05), (tx, ridge, top + rise + 0.05)], 0.075, tile, 6)
    for tx in np.arange(x0 - over + 0.3, -x0 + over, 0.7):                                # rafter ends
        b.box(tx - 0.05, tx + 0.05, yu - over + 0.04, yu, top - 0.2, top - 0.04, wood); b.box(tx - 0.05, tx + 0.05, yb, yb + over - 0.04, top - 0.2, top - 0.04, wood)
    for sx in (x0 + 0.2, -x0 - 0.2):                                                      # gable ends
        b.face([(sx, yu, top), (sx, yb, top), (sx, ridge, top + rise - 0.08)], plaster)
        b.face([(sx, yb, top), (sx, yu, top), (sx, ridge, top + rise - 0.08)], plaster)
    b.tube([(x0 - over, ridge, top + rise + 0.06), (-x0 + over, ridge, top + rise + 0.06)], 0.12, tile, 8)
    lean = ((rng.random() - 0.5) * 0.012, (rng.random() - 0.5) * 0.008)
    return b.finish(0.0, footprint=(w / 2, depth / 2 + jetty / 2), wobble=0.022, lean=lean)


# ---------------------------------------------------------------------------------------------------------------------
# The orders

def column(b, x, y, z0, height, r, order='tuscan', mat=None):
    """A column standing at (x, y) from z0. Tuscan: plain shaft, simple capital. Corinthian: fluted, with a leafed bell."""
    mat = mat or M('marble')
    base_h, cap_h = (r * 0.9, r * 0.9) if order == 'tuscan' else (r * 1.0, r * 2.3)
    b.box(x - r * 1.45, x + r * 1.45, y - r * 1.45, y + r * 1.45, z0, z0 + r * 0.35, mat)                 # plinth
    b.lathe([(r * 1.4, z0 + r * 0.35), (r * 1.45, z0 + r * 0.5), (r * 1.3, z0 + r * 0.62), (r * 1.12, z0 + r * 0.7), (r * 1.25, z0 + r * 0.82), (r * 1.05, z0 + base_h)], mat, x, y, 24)
    s0, s1 = z0 + base_h, z0 + height - cap_h
    shaft = [(r * (1.0 - 0.16 * (t ** 1.6)) * (1 + 0.012 * math.sin(t * math.pi)), s0 + (s1 - s0) * t) for t in np.linspace(0, 1, 9)]   # entasis
    b.lathe(shaft, mat, x, y, 24, flutes=20 if order == 'corinthian' else 0, flute_depth=0.07)
    rt = r * 0.84
    if order == 'tuscan':
        b.lathe([(rt, s1), (rt * 1.1, s1 + cap_h * 0.2), (rt * 1.02, s1 + cap_h * 0.3), (rt * 1.38, s1 + cap_h * 0.62)], mat, x, y, 24)
        b.box(x - rt * 1.5, x + rt * 1.5, y - rt * 1.5, y + rt * 1.5, s1 + cap_h * 0.62, s1 + cap_h, mat)
    else:
        b.lathe([(rt * 1.06, s1), (rt * 1.0, s1 + cap_h * 0.06), (rt * 1.02, s1 + cap_h * 0.5), (rt * 1.25, s1 + cap_h * 0.82), (rt * 1.45, s1 + cap_h * 0.9)], mat, x, y, 24)
        for tier, (zz, out, n) in enumerate(((0.08, 1.12, 8), (0.36, 1.2, 8))):                       # two rings of acanthus
            for k in range(n):
                a = 2 * math.pi * (k + tier * 0.5) / n
                cx, cy = x + math.cos(a) * rt * out, y + math.sin(a) * rt * out
                b.box(cx - rt * 0.2, cx + rt * 0.2, cy - rt * 0.07, cy + rt * 0.07, s1 + cap_h * zz, s1 + cap_h * (zz + 0.3), mat, rot=a + math.pi / 2)
                tx, ty = x + math.cos(a) * rt * (out + 0.12), y + math.sin(a) * rt * (out + 0.12)
                b.box(tx - rt * 0.16, tx + rt * 0.16, ty - rt * 0.07, ty + rt * 0.07, s1 + cap_h * (zz + 0.24), s1 + cap_h * (zz + 0.34), mat, rot=a + math.pi / 2)
        for k in range(4):                                                                           # corner volutes
            a = math.pi / 4 + k * math.pi / 2
            b.lathe([(rt * 0.2, s1 + cap_h * 0.66), (rt * 0.24, s1 + cap_h * 0.76), (rt * 0.2, s1 + cap_h * 0.86)], mat, x + math.cos(a) * rt * 1.5, y + math.sin(a) * rt * 1.5, 8)
        b.box(x - rt * 1.55, x + rt * 1.55, y - rt * 1.55, y + rt * 1.55, s1 + cap_h * 0.88, s1 + cap_h, mat, rot=0)


def column_asset(name, order, height=6.0, r=0.42):
    b = Builder(name)
    column(b, 0, 0, 0, height, r, order)
    return b.finish(0.012, footprint=(r * 1.5, r * 1.5))


def temple(name):
    """A Roman temple: high podium with steps only at the front, a deep columned porch, a closed cella, a pediment."""
    b = Builder(name)
    marble, trav, tile, bronze, dark = M('marble'), M('travertine'), M('rooftile'), M('bronze'), M('dark')
    w, d, pod, col_h, r = 10.4, 18.0, 2.7, 8.4, 0.5
    yf, yb = -d / 2, d / 2
    b.box(-w / 2, w / 2, yf + 3.0, yb, 0, pod, trav)
    b.box(-w / 2 - 0.12, w / 2 + 0.12, yf + 2.9, yb + 0.12, 0, 0.35, trav); b.box(-w / 2 - 0.1, w / 2 + 0.1, yf + 2.92, yb + 0.1, pod - 0.22, pod, marble)
    steps = 9
    for k in range(steps):                                                               # the stair, between cheek walls
        b.box(-w / 2 + 1.0, w / 2 - 1.0, yf + k * 0.34, yf + 3.05, k * pod / steps, (k + 1) * pod / steps, marble)
    for sx in (-w / 2, w / 2 - 1.0):
        b.box(sx, sx + 1.0, yf + 0.2, yf + 3.05, 0, pod, trav); b.box(sx - 0.05, sx + 1.05, yf + 0.15, yf + 3.1, pod, pod + 0.18, marble)
    xs = [(-w / 2 + 0.85) + k * (w - 1.7) / 3 for k in range(4)]
    for x in xs:
        column(b, x, yf + 3.9, pod, col_h, r, 'corinthian')
    for y in (yf + 6.6,):
        for x in (xs[0], xs[-1]):
            column(b, x, y, pod, col_h, r, 'corinthian')
    cy0 = yf + 9.2                                                                       # the cella
    b.box(-w / 2 + 0.5, w / 2 - 0.5, cy0, yb - 0.5, pod, pod + col_h, M('plaster', 0xeee6d6))
    for x in (-w / 2 + 0.5, w / 2 - 0.85):
        b.box(x, x + 0.35, cy0 - 0.12, cy0 + 0.25, pod, pod + col_h, marble)
    b.box(-1.5, 1.5, cy0 - 0.14, cy0 + 0.1, pod, pod + 5.6, marble)
    for sx in (-1.3, 0.04):                                                              # bronze door leaves with studs
        b.box(sx, sx + 1.26, cy0 - 0.2, cy0 - 0.12, pod + 0.05, pod + 5.3, bronze)
        for i in range(2):
            for j in range(5):
                b.lathe([(0.0, 0), (0.06, 0.0), (0.0, 0.05)], bronze, 0, 0, 6) if False else None
                b.box(sx + 0.25 + i * 0.6, sx + 0.41 + i * 0.6, cy0 - 0.24, cy0 - 0.2, pod + 0.5 + j * 1.0, pod + 0.66 + j * 1.0, bronze)
    top = pod + col_h
    b.box(-w / 2 + 0.25, w / 2 - 0.25, yf + 3.3, yb - 0.3, top, top + 0.55, marble)          # architrave
    b.box(-w / 2 + 0.3, w / 2 - 0.3, yf + 3.35, yb - 0.35, top + 0.55, top + 1.05, M('plaster', 0xe9dcc4))   # frieze
    b.box(-w / 2 - 0.15, w / 2 + 0.15, yf + 2.9, yb + 0.1, top + 1.05, top + 1.35, marble)   # cornice
    for k in range(int(w / 0.5)):                                                        # dentils
        b.box(-w / 2 + 0.1 + k * 0.5, -w / 2 + 0.32 + k * 0.5, yf + 3.0, yf + 3.2, top + 0.9, top + 1.05, marble)
    rise = 2.5
    b.prism([(-w / 2 + 0.2, top + 1.35), (w / 2 - 0.2, top + 1.35), (0, top + 1.35 + rise - 0.2)], yf + 3.25, yf + 3.5, M('plaster', 0xe9dcc4))
    b.prism([(-w / 2 + 0.2, top + 1.35), (w / 2 - 0.2, top + 1.35), (0, top + 1.35 + rise - 0.2)], yb - 0.6, yb - 0.35, M('plaster', 0xe9dcc4))
    for sgn in (-1, 1):                                                                  # raking cornices and the tiled roof
        x_out, x_top = sgn * (w / 2 + 0.25), 0
        b.prism([(x_out, top + 1.3), (x_out, top + 1.55), (x_top, top + 1.55 + rise), (x_top, top + 1.3 + rise)] if sgn < 0 else
                [(x_top, top + 1.3 + rise), (x_top, top + 1.55 + rise), (x_out, top + 1.55), (x_out, top + 1.3)], yf + 2.85, yf + 3.25, marble)
        b.face([(x_out, yf + 3.0, top + 1.5), (x_out, yb + 0.15, top + 1.5), (x_top, yb + 0.15, top + 1.5 + rise), (x_top, yf + 3.0, top + 1.5 + rise)] if sgn > 0 else
               [(x_top, yf + 3.0, top + 1.5 + rise), (x_top, yb + 0.15, top + 1.5 + rise), (x_out, yb + 0.15, top + 1.5), (x_out, yf + 3.0, top + 1.5)], tile)
    b.tube([(0, yf + 3.0, top + 1.55 + rise), (0, yb + 0.15, top + 1.55 + rise)], 0.13, tile, 8)
    for x in (-w / 2 - 0.1, w / 2 + 0.1, 0):                                              # acroteria
        zz = top + 1.55 + (rise if x == 0 else 0)
        b.lathe([(0.0, zz), (0.22, zz + 0.1), (0.3, zz + 0.45), (0.12, zz + 0.8), (0.0, zz + 0.95)], bronze, x, yf + 3.05, 10)
    return b.finish(0.0, footprint=(w / 2 + 0.2, d / 2))


# ---------------------------------------------------------------------------------------------------------------------
# Street furniture

def street_fountain(name):
    """A public street fountain as at Pompeii: a trough of four upright stone slabs clamped with lead, a standing stone
    carved with a spout, the rim worn hollow where people leaned to drink."""
    b = Builder(name)
    basalt, trav, lead, water, bronze = M('basalt'), M('travertine'), M('lead'), M('water'), M('bronze')
    L, W, H, t = 1.9, 1.5, 0.82, 0.2
    b.box(-L / 2, L / 2, -W / 2, -W / 2 + t, 0, H, trav); b.box(-L / 2, L / 2, W / 2 - t, W / 2, 0, H, trav)
    b.box(-L / 2, -L / 2 + t, -W / 2 + t, W / 2 - t, 0, H, trav); b.box(L / 2 - t, L / 2, -W / 2 + t, W / 2 - t, 0, H, trav)
    b.box(-L / 2 + t, L / 2 - t, -W / 2 + t, W / 2 - t, 0, 0.12, trav)
    b.box(-L / 2 + t, L / 2 - t, -W / 2 + t, W / 2 - t, H - 0.2, H - 0.16, water)
    for cx, cy in ((-L / 2 + 0.1, -W / 2 + 0.1), (L / 2 - 0.1, -W / 2 + 0.1), (-L / 2 + 0.1, W / 2 - 0.1), (L / 2 - 0.1, W / 2 - 0.1)):
        b.box(cx - 0.09, cx + 0.09, cy - 0.035, cy + 0.035, H - 0.005, H + 0.02, lead, rot=math.pi / 4 if cx * cy > 0 else -math.pi / 4)
    b.box(-0.3, 0.3, W / 2 - 0.05, W / 2 + 0.28, 0, 1.55, trav)                          # the standing stone
    b.lathe([(0.2, 0), (0.24, 0.05), (0.2, 0.1)], trav, 0, 0, 14, squash=1.0) if False else None
    b.box(-0.2, 0.2, W / 2 - 0.1, W / 2 - 0.05, 1.05, 1.45, trav)                          # the carved face, in low relief
    b.box(-0.05, 0.05, W / 2 - 0.13, W / 2 - 0.1, 1.2, 1.32, trav); b.box(-0.14, -0.08, W / 2 - 0.12, W / 2 - 0.1, 1.32, 1.37, trav); b.box(0.08, 0.14, W / 2 - 0.12, W / 2 - 0.1, 1.32, 1.37, trav)
    b.tube([(0, W / 2 - 0.08, 1.14), (0, W / 2 - 0.3, 1.12), (0, W / 2 - 0.36, 1.07)], 0.03, bronze, 8)
    b.tube([(0, W / 2 - 0.36, 1.06), (0, W / 2 - 0.4, H - 0.16)], 0.014, water, 6)        # the falling water
    b.box(-0.55, 0.55, -W / 2 - 0.45, -W / 2 - 0.02, 0, 0.14, basalt)                     # a step, worn
    b.box(-L / 2 - 0.02, -L / 2 + 0.24, -0.28, 0.28, H - 0.07, H + 0.001, water) if False else None
    return b.finish(0.025, footprint=(L / 2 + 0.1, W / 2 + 0.3))


def amphora(name, kind):
    """Dressel 20: the round Spanish oil jar. Dressel 2-4: the tall wine jar with a spike foot and long handles."""
    b = Builder(name)
    clay = M('terracotta')
    if kind == 'oil':
        prof = [(0.0, 0.0), (0.04, 0.0), (0.05, 0.05), (0.2, 0.14), (0.29, 0.32), (0.3, 0.45), (0.25, 0.6), (0.12, 0.7), (0.075, 0.74), (0.075, 0.84), (0.1, 0.86), (0.1, 0.89), (0.07, 0.89)]
        for sx in (-1, 1):
            b.tube([(sx * 0.09, 0, 0.82), (sx * 0.2, 0, 0.82), (sx * 0.24, 0, 0.72), (sx * 0.22, 0, 0.62)], 0.028, clay, 8)
        fp = 0.3
    else:
        prof = [(0.0, 0.0), (0.03, 0.0), (0.035, 0.1), (0.1, 0.2), (0.16, 0.36), (0.165, 0.7), (0.13, 0.82), (0.06, 0.88), (0.055, 1.08), (0.075, 1.1), (0.075, 1.13), (0.05, 1.13)]
        for sx in (-1, 1):
            b.tube([(sx * 0.06, 0, 1.06), (sx * 0.15, 0, 1.07), (sx * 0.16, 0, 0.95), (sx * 0.15, 0, 0.82)], 0.022, clay, 8)
        fp = 0.2
    b.lathe(prof, clay, 0, 0, 20)
    return b.finish(0.0, footprint=(fp, fp))


def arc_shell(b, profile, a0, a1, mat, seg=10, grow=1.0):
    """Part of a turned shape: the profile swept only from angle a0 to a1 (a door, a panel)."""
    idx = b.slot(mat)
    rings = [[b.bm.verts.new((math.cos(a0 + (a1 - a0) * k / seg) * r * grow, math.sin(a0 + (a1 - a0) * k / seg) * r * grow, z)) for k in range(seg + 1)] for r, z in profile]
    for p, q in zip(rings, rings[1:]):
        for k in range(seg):
            f = b.bm.faces.new([p[k], p[k + 1], q[k + 1], q[k]]); f.material_index = idx; f[b.smooth] = 1
    return rings


def machine(name):
    """The time machine: a one-seat pod of dark steel on three legs, banded in brass, its hatch swung open on the street
    side, three gimbal rings round it, the core glowing inside. Nothing about it belongs in AD 155, which is the point."""
    b = Builder(name)
    steel, brass, scorch, glow, seat = M('steel'), M('bronze'), M('scorch'), M('coreglow'), M('seat')
    hull = [(0.0, 0.55), (0.45, 0.6), (0.85, 0.85), (1.08, 1.3), (1.15, 1.8), (1.08, 2.3), (0.85, 2.75), (0.5, 3.05), (0.18, 3.2), (0.0, 3.22)]
    front0, front1 = math.radians(-128), math.radians(-52)                       # the hatchway faces -Y (the street)
    arc_shell(b, hull, front1, front0 + 2 * math.pi, steel, seg=30)              # the hull, open at the hatch
    door_profile = [p for p in hull if 0.8 <= p[1] <= 2.8]
    arc_shell(b, [hull[0], hull[1], hull[2]], front0, front1, steel, seg=6); arc_shell(b, hull[6:], front0, front1, steel, seg=6)   # below and above the hatch
    # Inside: a dark shell, a seat, straps, and the core's light.
    arc_shell(b, [(r * 0.93, z) for r, z in hull], front1, front0 + 2 * math.pi, scorch, seg=24)   # open at the hatch too
    b.box(-0.32, 0.32, -0.25, 0.35, 0.9, 1.05, seat); b.box(-0.32, 0.32, 0.25, 0.4, 1.05, 2.0, seat); b.box(-0.2, 0.2, 0.2, 0.3, 2.0, 2.3, seat)
    for sx in (-0.2, 0.2):
        b.tube([(sx, 0.28, 1.95), (sx * 0.5, -0.05, 1.5), (0, -0.2, 1.1)], 0.025, M('wood', 0x7a2f26), 6)
    b.lathe([(0.0, 2.35), (0.3, 2.42), (0.42, 2.62), (0.3, 2.82), (0.0, 2.9)], glow, 0, 0.1, 12)
    b.box(-0.5, 0.5, -0.62, -0.5, 1.15, 1.45, scorch); b.box(-0.42, -0.1, -0.64, -0.6, 1.22, 1.38, glow)        # the dead panel, one lamp still lit
    # The hatch door, swung out to one side on its hinge.
    start = len(b.bm.verts)
    b.bm.verts.ensure_lookup_table()
    arc_shell(b, door_profile, front0, front1, steel, seg=8, grow=1.02); arc_shell(b, [(r * 0.97, z) for r, z in door_profile], front0, front1, scorch, seg=8)
    b.bm.verts.ensure_lookup_table()
    hinge = Vector((math.cos(front1) * 1.12, math.sin(front1) * 1.12, 0)); rot = Matrix.Rotation(math.radians(112), 4, 'Z')
    for v in list(b.bm.verts)[start:]:
        v.co = hinge + rot @ (v.co - hinge)
    # Brass bands, rivets, the three gimbal rings.
    for z in (0.86, 1.8, 2.74):
        r = [p[0] for p in hull if abs(p[1] - z) < 0.06]
        rr = (r[0] if r else 1.15) + 0.02
        b.tube([(math.cos(a) * rr, math.sin(a) * rr, z) for a in np.linspace(front1, front0 + 2 * math.pi, 40)], 0.035, brass, 6)
    for k, (tilt, r) in enumerate(((0.35, 1.55), (-0.5, 1.72), (1.25, 1.9))):
        ring = [Matrix.Rotation(tilt, 4, 'X' if k != 1 else 'Y') @ Vector((math.cos(a) * r, math.sin(a) * r, 0)) + Vector((0, 0, 1.8)) for a in np.linspace(0, 2 * math.pi, 49)]
        b.tube(ring, 0.04, brass, 6)
    # Three splayed legs with pads, a torn cable, scorch on the ground side.
    for k in range(3):
        a = math.radians(90 + k * 120)
        b.tube([(math.cos(a) * 0.55, math.sin(a) * 0.55, 0.8), (math.cos(a) * 1.25, math.sin(a) * 1.25, 0.08)], 0.07, steel, 8)
        b.lathe([(0.0, 0.0), (0.26, 0.0), (0.22, 0.08), (0.0, 0.1)], steel, math.cos(a) * 1.25, math.sin(a) * 1.25, 10)
    b.tube([(0.9, 0.5, 1.0), (1.4, 0.8, 0.5), (1.5, 1.3, 0.06), (1.1, 1.7, 0.05), (1.6, 2.0, 0.05)], 0.045, scorch, 6)
    b.lathe([(0.3, 0.52), (0.5, 0.5), (0.42, 0.36), (0.2, 0.3), (0.0, 0.3)], scorch, 0, 0, 12)                      # the field emitter underneath, burnt
    return b.finish(0.0, footprint=(1.9, 1.9))


def cart(name):
    """A two-wheeled Roman cart (plaustrum): plank bed, stake sides, spoked wheels with iron tyres, a pole for the mule."""
    b = Builder(name)
    wood, iron = M('wood'), M('steel')
    b.box(-1.3, 1.3, -0.7, 0.7, 0.78, 0.9, wood)
    for sy in (-0.7, 0.66):
        b.box(-1.3, 1.3, sy, sy + 0.04, 0.9, 1.3, wood)
        for sx in np.linspace(-1.2, 1.2, 6):
            b.box(sx - 0.03, sx + 0.03, sy - 0.02, sy + 0.06, 0.78, 1.42, wood)
    b.box(-1.34, -1.3, -0.7, 0.7, 0.9, 1.3, wood)
    for sy in (-0.86, 0.86):
        rim = [(math.cos(a) * 0.62, sy, 0.62 + math.sin(a) * 0.62) for a in np.linspace(0, 2 * math.pi, 25)]
        b.tube(rim, 0.055, wood, 6); b.tube([(x * 1.07, y, 0.62 + (z - 0.62) * 1.07) for x, y, z in rim], 0.022, iron, 5)
        for k in range(8):
            a = k * math.pi / 4
            b.tube([(0, sy, 0.62), (math.cos(a) * 0.6, sy, 0.62 + math.sin(a) * 0.6)], 0.03, wood, 5)
        b.lathe([(0.0, 0), (0.11, 0), (0.11, 0.16), (0.0, 0.16)], wood, 0, 0, 8) if False else None
    b.tube([(0, -0.95, 0.62), (0, 0.95, 0.62)], 0.05, wood, 6)
    b.tube([(1.3, 0, 0.82), (3.4, 0, 1.0)], 0.045, wood, 6); b.tube([(3.3, -0.5, 1.0), (3.3, 0.5, 1.0)], 0.035, wood, 6)
    for x, y in ((-0.6, 0.1), (0.2, -0.2), (0.7, 0.25)):                                                           # a load of jars
        b.lathe([(0.0, 0.9), (0.14, 0.95), (0.2, 1.15), (0.18, 1.4), (0.07, 1.52), (0.08, 1.6)], M('terracotta'), x, y, 10)
    return b.finish(0.0, footprint=(1.5, 1.0))


def shrine(name):
    """A crossroads shrine of the Lares: a small temple front on a masonry base, painted figures in the niche, a lamp."""
    b = Builder(name)
    marble, trav, red = M('marble'), M('travertine'), M('plaster', 0x9a3626)
    b.box(-0.75, 0.75, -0.5, 0.5, 0, 0.95, trav); b.box(-0.82, 0.82, -0.56, 0.56, 0.95, 1.05, marble); b.box(-0.8, 0.8, -0.54, 0.54, 0, 0.12, marble)
    b.box(-0.6, 0.6, -0.05, 0.42, 1.05, 2.25, red)
    b.arch(0, 0.36, -0.07, 0.0, 1.75, 2.25, -0.6, 0.6, M('plaster', 0xe8d8b0), seg=8); b.box(-0.6, -0.36, -0.07, 0.0, 1.05, 1.75, M('plaster', 0xe8d8b0)); b.box(0.36, 0.6, -0.07, 0.0, 1.05, 1.75, M('plaster', 0xe8d8b0))
    for sx in (-0.62, 0.62):
        column(b, sx, -0.32, 1.05, 1.25, 0.075, 'tuscan')
    b.box(-0.78, 0.78, -0.48, 0.48, 2.3, 2.42, marble)
    b.prism([(-0.8, 2.42), (0.8, 2.42), (0, 2.85)], -0.48, 0.48, marble)
    for sx in (-0.16, 0.16):                                                                                       # the two Lares, as small bronzes
        b.lathe([(0.05, 1.07), (0.05, 1.3), (0.035, 1.42), (0.045, 1.5), (0.0, 1.55)], M('bronze'), sx, 0.1, 8)
    b.lathe([(0.0, 1.07), (0.07, 1.07), (0.08, 1.11), (0.03, 1.13)], M('terracotta'), 0, -0.3, 8); b.lathe([(0.0, 1.13), (0.025, 1.15), (0.0, 1.22)], M('lamp'), 0, -0.3, 6)
    return b.finish(0.0, footprint=(0.85, 0.6))


# ---------------------------------------------------------------------------------------------------------------------
# Build, export, and a picture to judge by

ASSETS = [insula('insula_a', 3, 4, 8.0, 1), insula('insula_b', 2, 3, 7.0, 2), insula('insula_c', 4, 5, 9.0, 3), insula('insula_d', 3, 2, 7.0, 4),
          insula('insula_e', 2, 5, 7.0, 5), insula('insula_f', 4, 3, 8.0, 6),
          column_asset('column_tuscan', 'tuscan'), column_asset('column_corinthian', 'corinthian', 8.4, 0.5), temple('temple'),
          street_fountain('street_fountain'), amphora('amphora_oil', 'oil'), amphora('amphora_wine', 'wine'),
          machine('machine'), cart('cart'), shrine('shrine')]

bpy.ops.object.select_all(action='DESELECT')
for ob in ASSETS:
    ob.select_set(True)
bpy.ops.export_scene.gltf(filepath=OUT, export_format='GLB', use_selection=True, export_apply=True, export_extras=True, export_yup=True,
                          export_image_format='JPEG', export_jpeg_quality=86, export_tangents=False, export_animations=False, export_vertex_color='ACTIVE', export_all_vertex_colors=False)
tris = {ob.name: sum(len(p.vertices) - 2 for p in ob.data.polygons) for ob in ASSETS}
print('KIT', os.path.getsize(OUT) // 1024, 'KB', tris)

if PREVIEW:
    x = 0.0
    for ob in ASSETS:
        hw = ob.get('hw', 1.0)
        x += hw + 0.8; ob.location.x = x; x += hw + 0.8
    span = x
    ground = bpy.data.meshes.new('ground'); gb = bmesh.new(); bmesh.ops.create_grid(gb, x_segments=1, y_segments=1, size=span); gb.to_mesh(ground)
    g = bpy.data.objects.new('ground', ground); g.location = (span / 2, 0, 0); bpy.context.scene.collection.objects.link(g)
    gm = material('basalt', 'basalt'); ground.materials.append(gm)
    uv = ground.uv_layers.new(name='UVMap')
    for poly in ground.polygons:
        for li in poly.loop_indices:
            v = ground.vertices[ground.loops[li].vertex_index].co; uv.data[li].uv = (v.x / 3, v.y / 3)
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'; sc.cycles.samples = 40; sc.cycles.use_denoising = False; sc.render.resolution_x, sc.render.resolution_y = 1800, 760
    world = bpy.data.worlds.new('w'); sc.world = world; world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.55, 0.72, 0.95, 1); world.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.9
    sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); sun.data.energy = 4.0; sun.data.angle = 0.02; sun.rotation_euler = (math.radians(52), 0, math.radians(-38)); sc.collection.objects.link(sun)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.lens = 34
    def shot(path, pos, look):
        cam.location = pos
        d = Vector(look) - Vector(pos); cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = path; bpy.ops.render.render(write_still=True)
    shot(PREVIEW, (span * 0.2, -26, 9), (span * 0.22, 0, 6))
    shot(PREVIEW.replace('.png', '2.png'), (span * 0.74, -24, 7), (span * 0.76, 0, 4.5))
    shot(PREVIEW.replace('.png', '3.png'), (ASSETS[0].location.x - 2, -9, 2.2), (ASSETS[0].location.x, -3, 3.2))
