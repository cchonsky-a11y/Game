"""Renders the opening picture from the art pack: the machine standing in a Roman street at the end of the day.
Run with Blender's Python:  python key_art.py <kit.glb> <out.jpg> [size] [samples]"""
import math
import sys
import bpy
from mathutils import Vector

KIT, OUT = sys.argv[1], sys.argv[2]
SIZE = int(sys.argv[3]) if len(sys.argv) > 3 else 1280
SAMPLES = int(sys.argv[4]) if len(sys.argv) > 4 else 96
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=KIT)
sc = bpy.context.scene
kit = {o.name: o for o in sc.objects if o.parent is None}

# A painted texture is multiplied by the colour in its material's name (the game does the same when it loads the pack).
for m in bpy.data.materials:
    if '#' not in m.name or not m.use_nodes:
        continue
    hexc = m.name.split('#')[1][:6]
    rgb = [(int(hexc[i:i + 2], 16) / 255) ** 2.2 for i in (0, 2, 4)]
    bsdf = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if not bsdf or not bsdf.inputs['Base Color'].is_linked:
        continue
    src = bsdf.inputs['Base Color'].links[0].from_socket
    mix = m.node_tree.nodes.new('ShaderNodeMix'); mix.data_type = 'RGBA'; mix.blend_type = 'MULTIPLY'; mix.inputs[0].default_value = 1.0
    m.node_tree.links.new(src, mix.inputs[6]); mix.inputs[7].default_value = (*rgb, 1)
    m.node_tree.links.new(mix.outputs[2], bsdf.inputs['Base Color'])
for name, strength in (('lamplight', 6.0), ('coreglow', 3.5)):
    m = bpy.data.materials.get(name)
    if m:
        next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED').inputs['Emission Strength'].default_value = strength


def place(name, x, y, turn=0.0, scale=1.0):
    src = kit[name]
    def copy(o, parent=None):
        c = o.copy(); sc.collection.objects.link(c)
        if parent: c.parent = parent
        for ch in o.children: copy(ch, c)
        return c
    o = copy(src)
    o.location = (x, y, 0); o.rotation_mode = 'XYZ'; o.rotation_euler = (o.rotation_euler[0], o.rotation_euler[1], turn); o.scale = (scale,) * 3
    return o


def size(name):
    return kit[name].get('hw', 1.0), kit[name].get('hd', 1.0)


# The street: tenements on both sides, running away from the camera, a temple closing the view.
order = ['insula_b', 'insula_a', 'insula_e', 'insula_c', 'insula_d', 'insula_f', 'insula_a', 'insula_c']
for side, turn in ((-1, math.pi / 2), (1, -math.pi / 2)):
    y = -14.0
    for k in range(8):
        name = order[(k + (3 if side > 0 else 0)) % len(order)]
        hw, hd = size(name)
        place(name, side * (4.6 + hd), y + hw, turn)
        y += hw * 2 + 0.05
place('temple', 0.5, 92, 0, 1.5)
place("machine", -1.5, 7.2, math.radians(14))
place('cart', 2.4, 15.5, math.radians(100))
place('shrine', 3.9, 9.2, -math.pi / 2)
place('street_fountain', -3.2, 21, math.pi / 2, 1.3)
for x, y, n, t in ((3.6, 4.6, 'amphora_wine', 0.2), (3.2, 4.2, 'amphora_oil', 1.0), (-3.9, 12.4, 'amphora_oil', 2.0), (-3.7, 13.1, 'amphora_wine', 0.4)):
    place(n, x, y, t)
for o in kit.values():
    o.location = (0, -500, -50)                      # the originals wait out of sight

# The paving: the pack's basalt, at its real scale.
import bmesh
me = bpy.data.meshes.new('street'); bm = bmesh.new(); bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=160); bm.to_mesh(me)
ground = bpy.data.objects.new('street', me); sc.collection.objects.link(ground)
uv = me.uv_layers.new(name='UVMap')
for poly in me.polygons:
    for li in poly.loop_indices:
        v = me.vertices[me.loops[li].vertex_index].co; uv.data[li].uv = (v.x / 3, v.y / 3)
me.materials.append(bpy.data.materials['basalt'])

# The end of the day: a low sun down the street, a warm sky.
world = bpy.data.worlds.new('sky'); sc.world = world; world.use_nodes = True
nt = world.node_tree; bg = nt.nodes['Background']
sky = nt.nodes.new('ShaderNodeTexSky')
try:
    sky.sun_elevation = math.radians(3.5); sky.sun_rotation = math.radians(-14); sky.sun_intensity = 1.0
    for attr, val in (('air_density', 1.6), ('dust_density', 3.0), ('aerosol_density', 3.0)):
        if hasattr(sky, attr): setattr(sky, attr, val)
except Exception as e:
    print('SKY', e)
nt.links.new(sky.outputs['Color'], bg.inputs['Color']); bg.inputs['Strength'].default_value = 0.3
fill = bpy.data.objects.new('fill', bpy.data.lights.new('fill', 'AREA')); fill.data.energy = 260; fill.data.size = 6; fill.data.color = (0.75, 0.85, 1.0)
fill.location = (1.5, -4, 5); fill.rotation_euler = (math.radians(65), 0, math.radians(8)); sc.collection.objects.link(fill)
core = bpy.data.objects.new('core', bpy.data.lights.new('core', 'POINT')); core.data.energy = 120; core.data.color = (0.4, 0.7, 1.0); core.data.shadow_soft_size = 0.2
core.location = (-1.5, 6.9, 2.0); sc.collection.objects.link(core)   # inside the pod, so the light spills out of the hatch
spill = bpy.data.objects.new('spill', bpy.data.lights.new('spill', 'POINT')); spill.data.energy = 160; spill.data.color = (0.35, 0.65, 1.0); spill.data.shadow_soft_size = 0.6
spill.location = (-1.0, 5.2, 0.6); sc.collection.objects.link(spill)  # its light on the paving in front of the hatch
bounce = bpy.data.objects.new('bounce', bpy.data.lights.new('bounce', 'AREA')); bounce.data.energy = 1400; bounce.data.size = 8; bounce.data.color = (1.0, 0.55, 0.28)
bounce.location = (0, 40, 6); bounce.rotation_euler = Vector((0, -1, -0.12)).to_track_quat('-Z', 'Y').to_euler(); sc.collection.objects.link(bounce)  # warm light thrown back down the street
# The sun itself, low at the far end of the street, raking the right-hand fronts and the temple gold.
sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); sun.data.energy = 6.0; sun.data.color = (1.0, 0.52, 0.24); sun.data.angle = math.radians(1.5)
sun.rotation_euler = Vector((-0.62, -0.62, -0.42)).to_track_quat('-Z', 'Y').to_euler(); sc.collection.objects.link(sun)

cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.lens = 22; cam.location = (1.4, 0.4, 1.05)
cam.rotation_euler = (Vector((-1.2, 10.5, 2.4)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
cam.data.dof.use_dof = True; cam.data.dof.focus_distance = 7.2; cam.data.dof.aperture_fstop = 4.0

sc.render.engine = 'CYCLES'; sc.cycles.device = 'CPU'; sc.cycles.samples = SAMPLES
try:
    sc.cycles.use_denoising = True
except Exception:
    pass
sc.render.resolution_x = SIZE; sc.render.resolution_y = int(SIZE * 0.625)
sc.view_settings.exposure = 0.0
try:
    sc.view_settings.look = 'AgX - Medium High Contrast'
except Exception as e:
    print('LOOK', e)
sc.render.image_settings.file_format = 'JPEG'; sc.render.image_settings.quality = 88
sc.render.filepath = OUT
bpy.ops.render.render(write_still=True)
print('KEYART', OUT)
