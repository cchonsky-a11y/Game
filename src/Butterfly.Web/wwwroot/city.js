// The look of the walkable city (presentation only, all of it drawn by code: no downloaded art).
// Textures are painted on canvases; buildings are a textured block with a tiled roof; people are small jointed figures.
// Nothing here knows anything about the game: game.js tells it which district to build and what stands in it.
import * as THREE from './lib/three.module.min.js';

import { GLTFLoader } from './lib/GLTFLoader.js';

// The art pack (art/kit, modelled in Blender): loaded once, then copied wherever a piece stands.
const KIT = {};
export async function loadKit(url) {
  const gltf = await new GLTFLoader().loadAsync(url);
  gltf.scene.traverse(o => {
    if (!o.isMesh) return;
    o.castShadow = o.receiveShadow = true;
    const tint = /#([0-9a-f]{6})/.exec(o.material.name);            // a painted texture multiplied by its material's colour
    if (tint && o.material.map) o.material.color.set('#' + tint[1]);
    if (o.material.name.startsWith('lamplight')) o.material.emissiveIntensity = 1.6;
  });
  for (const o of [...gltf.scene.children]) KIT[o.name] = o;
  gltf.scene.traverse(o => { if (o.isMesh) MATS[o.material.name.split('#')[0]] = o.material; });
}
const MATS = {};
export const kitSize = name => ({ hw: KIT[name].userData.hw || 1, hd: KIT[name].userData.hd || 1 });
// One of the pack's painted materials, repeated to cover a surface of the given size in metres.
function kitMaterial(base, metres, w, h, color) {
  const m = MATS[base].clone(), map = m.map.clone(), nrm = m.normalMap.clone(), orm = m.roughnessMap.clone();
  for (const t of [map, nrm, orm]) { t.wrapS = t.wrapT = THREE.RepeatWrapping; t.repeat.set(w / metres, h / metres); t.needsUpdate = true; }
  m.map = map; m.normalMap = nrm; m.roughnessMap = orm; m.metalnessMap = orm; m.color.set(color ?? 0xffffff);
  return m;
}
const INSULAE = ['insula_a', 'insula_b', 'insula_c', 'insula_d', 'insula_e', 'insula_f'];

// A fixed sequence for layout and painting, so a street looks the same every time (cosmetic; never used for the game).
export function sequence(seedText) {
  let a = 0; for (const c of seedText) a = (a * 31 + c.charCodeAt(0)) >>> 0;
  return () => { a |= 0; a = a + 0x6D2B79F5 | 0; let t = Math.imul(a ^ a >>> 15, 1 | a); t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; };
}

// ---------------------------------------------------------------------------------------------------------------------
// Painted textures

function canvas(w, h, paint) {
  const c = document.createElement('canvas'); c.width = w; c.height = h;
  paint(c.getContext('2d'), w, h);
  const t = new THREE.CanvasTexture(c);
  t.colorSpace = THREE.SRGBColorSpace; t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = 4;
  return t;
}
function speckle(g, w, h, rnd, n, colors, max = 3) {
  for (let i = 0; i < n; i++) { g.fillStyle = colors[Math.floor(rnd() * colors.length)]; g.globalAlpha = 0.08 + rnd() * 0.18; const s = 1 + rnd() * max; g.fillRect(rnd() * w, rnd() * h, s, s); }
  g.globalAlpha = 1;
}
const cache = new Map();
const once = (key, make) => { if (!cache.has(key)) cache.set(key, make()); return cache.get(key); };

const cobble = later => once('cobble' + later, () => canvas(256, 256, (g, w, h) => {
  const rnd = sequence('cobble');
  g.fillStyle = later ? '#5d564d' : '#6a6157'; g.fillRect(0, 0, w, h);
  for (let y = 0; y < 8; y++) for (let x = 0; x < 8; x++) {
    const ox = (y % 2) * 16, v = 105 + Math.floor(rnd() * 45);
    g.fillStyle = `rgb(${v + 8},${v},${v - 10})`;
    g.beginPath(); g.roundRect(x * 32 + ox - 14 + rnd() * 3, y * 32 + 2 + rnd() * 3, 26, 26, 8); g.fill();
  }
  speckle(g, w, h, rnd, 500, ['#3a332c', '#a59a8a']);
}));
const earth = later => once('earth' + later, () => canvas(256, 256, (g, w, h) => {
  const rnd = sequence('earth');
  g.fillStyle = later ? '#8b8273' : '#a8946f'; g.fillRect(0, 0, w, h);
  speckle(g, w, h, rnd, 2600, ['#7d6a4c', '#c7b48c', '#6b5b43'], 4);
}));
const tiles = later => once('tiles' + later, () => canvas(128, 128, (g, w, h) => {
  const rnd = sequence('tiles');
  g.fillStyle = later ? '#7c4d3a' : '#a5502f'; g.fillRect(0, 0, w, h);
  for (let y = 0; y < 8; y++) for (let x = 0; x < 8; x++) {
    const v = rnd() * 30 - 15;
    g.fillStyle = later ? `rgb(${128 + v},${82 + v},${64 + v})` : `rgb(${178 + v},${88 + v},${52 + v})`;
    g.beginPath(); g.ellipse(x * 16 + 8, y * 16 + 4, 7.5, 11, 0, 0, Math.PI); g.fill();
  }
  g.fillStyle = 'rgba(40,20,10,.25)'; for (let y = 0; y < 8; y++) g.fillRect(0, y * 16, w, 2);
}));
const wood = () => once('wood', () => canvas(128, 128, (g, w, h) => {
  const rnd = sequence('wood');
  g.fillStyle = '#6f5134'; g.fillRect(0, 0, w, h);
  for (let x = 0; x < 8; x++) { g.fillStyle = `rgba(${40 + rnd() * 40},${25 + rnd() * 20},10,.35)`; g.fillRect(x * 16, 0, 15, h); }
  speckle(g, w, h, rnd, 300, ['#3a2a18', '#8c6a45'], 6);
}));
const marble = later => once('marble' + later, () => canvas(256, 256, (g, w, h) => {
  const rnd = sequence('marble');
  g.fillStyle = later ? '#c9c2b4' : '#e9e3d6'; g.fillRect(0, 0, w, h);
  g.strokeStyle = 'rgba(120,112,100,.25)';
  for (let i = 0; i < 26; i++) { g.lineWidth = 0.5 + rnd() * 1.5; g.beginPath(); let x = rnd() * w, y = rnd() * h; g.moveTo(x, y); for (let k = 0; k < 5; k++) { x += rnd() * 60 - 30; y += rnd() * 60 - 20; g.lineTo(x, y); } g.stroke(); }
  speckle(g, w, h, rnd, 700, ['#b8b0a0', '#ffffff']);
}));
const stripes = color => once('stripes' + color, () => canvas(64, 64, (g, w, h) => {
  g.fillStyle = '#efe6d2'; g.fillRect(0, 0, w, h); g.fillStyle = color; for (let x = 0; x < 4; x++) g.fillRect(x * 16, 0, 8, h);
}));
const water = () => once('water', () => canvas(128, 128, (g, w, h) => {
  const rnd = sequence('water');
  g.fillStyle = '#3f86a8'; g.fillRect(0, 0, w, h);
  g.strokeStyle = 'rgba(255,255,255,.35)';
  for (let i = 0; i < 40; i++) { g.lineWidth = 1 + rnd() * 1.5; g.beginPath(); const x = rnd() * w, y = rnd() * h; g.ellipse(x, y, 6 + rnd() * 14, 2 + rnd() * 3, 0, 0, Math.PI * 2); g.stroke(); }
}));

const WALLS = ['#d9c19c', '#cfa97f', '#e2d0b2', '#c99c75', '#d6b48f', '#bfa07c'];
const WALLS_LATER = ['#b3a48e', '#a39581', '#beb19c', '#99897a'];
// Words Romans really painted and scratched on their walls and thresholds (Pompeii).
const SIGNS = ['CAVE CANEM', 'SALVE LVCRVM', 'HIC HABITAT FELICITAS', 'SALVE', 'LVCRVM GAVDIVM', 'HAVE'];
const SHUTTERS = ['#5d7a5a', '#7a4a3a', '#3f6077', '#7a6a3f', '#5a4a3a'];

// One side of a tenement, painted: plaster, shops at street level, shuttered windows above, the odd balcony and stain.
function facade(style, bays, floors, later) {
  return once(`facade${style}:${bays}:${floors}:${later}`, () => canvas(bays * 96, floors * 112, (g, w, h) => {
    const rnd = sequence('facade' + style + bays + floors);
    const walls = later ? WALLS_LATER : WALLS;
    const wall = walls[style % walls.length], shutter = SHUTTERS[style % SHUTTERS.length];
    g.fillStyle = wall; g.fillRect(0, 0, w, h);
    speckle(g, w, h, rnd, bays * floors * 260, ['#8a7558', '#f3e6cc', '#6f5e48'], 4);
    // weather: streaks down from the roof line and damp at the foot
    for (let i = 0; i < bays * 3; i++) { const x = rnd() * w; const grd = g.createLinearGradient(0, 0, 0, 60 + rnd() * 90); grd.addColorStop(0, 'rgba(70,55,40,.28)'); grd.addColorStop(1, 'rgba(70,55,40,0)'); g.fillStyle = grd; g.fillRect(x, 0, 3 + rnd() * 7, 150); }
    const foot = g.createLinearGradient(0, h - 46, 0, h); foot.addColorStop(0, 'rgba(60,48,36,0)'); foot.addColorStop(1, 'rgba(60,48,36,.5)'); g.fillStyle = foot; g.fillRect(0, h - 46, w, 46);
    if (later) { g.fillStyle = 'rgba(50,60,45,.18)'; for (let i = 0; i < bays * 2; i++) g.fillRect(rnd() * w, h - rnd() * 120, 14 + rnd() * 30, 60); }
    // Street level is painted, as Roman walls were: a deep red dado under the plaster, and words on it.
    if (style % 3 !== 2) {
      g.fillStyle = later ? 'rgba(110,60,48,.7)' : (style % 2 ? 'rgba(138,48,36,.9)' : 'rgba(60,52,44,.85)'); g.fillRect(0, h - 40, w, 40);
      g.fillStyle = 'rgba(30,20,15,.5)'; g.fillRect(0, h - 42, w, 2.5);
    }
    if (!later && rnd() < 0.7) {
      const words = SIGNS[Math.floor(rnd() * SIGNS.length)];
      g.save(); g.translate(0, h - 112); g.font = 'bold 12px Georgia, serif'; g.fillStyle = '#7a2a1e'; g.textBaseline = 'top';
      g.fillText(words, 6 + rnd() * Math.max(4, w - 150), 6); g.restore();
    }
    for (let f = 0; f < floors; f++) {
      const y = h - (f + 1) * 112;
      for (let b = 0; b < bays; b++) {
        const x = b * 96;
        if (f === 0) {
          if (rnd() < 0.72) {                                     // a shop open to the street
            g.fillStyle = '#2a211a'; g.fillRect(x + 12, y + 28, 72, 84);
            g.fillStyle = '#6f5134'; g.fillRect(x + 8, y + 22, 80, 8);       // lintel
            g.fillStyle = '#8a6a45'; g.fillRect(x + 12, y + 84, 72, 10);     // counter
            g.fillStyle = 'rgba(255,190,110,.16)'; g.fillRect(x + 16, y + 34, 64, 46);
          } else {                                                // a door
            g.fillStyle = '#4a3524'; g.fillRect(x + 32, y + 36, 32, 76);
            g.fillStyle = '#2f2218'; g.fillRect(x + 47, y + 36, 2, 76);
            g.fillStyle = '#7a6248'; g.fillRect(x + 28, y + 30, 40, 7);
          }
        } else {
          const balcony = rnd() < 0.16;
          g.fillStyle = '#efe4cc'; g.fillRect(x + 30, y + 28, 36, 50);       // surround
          g.fillStyle = '#241c16'; g.fillRect(x + 34, y + 32, 28, 42);       // the dark room
          if (rnd() < 0.3) { g.fillStyle = 'rgba(255,200,120,.3)'; g.fillRect(x + 34, y + 32, 28, 42); }
          g.fillStyle = shutter; g.fillRect(x + 22, y + 32, 11, 42); g.fillRect(x + 63, y + 32, 11, 42);
          g.fillStyle = 'rgba(0,0,0,.25)'; for (let k = 0; k < 6; k++) { g.fillRect(x + 22, y + 34 + k * 7, 11, 1.5); g.fillRect(x + 63, y + 34 + k * 7, 11, 1.5); }
          if (balcony) { g.fillStyle = '#5a4632'; g.fillRect(x + 14, y + 78, 68, 6); for (let k = 0; k < 9; k++) g.fillRect(x + 16 + k * 8, y + 58, 2, 22); g.fillRect(x + 14, y + 56, 68, 3); }
          else if (rnd() < 0.2) { g.fillStyle = '#9c5a3a'; g.fillRect(x + 36, y + 76, 24, 7); g.fillStyle = '#4f7a45'; g.fillRect(x + 38, y + 68, 20, 9); }   // a window box
        }
      }
      g.fillStyle = 'rgba(60,45,30,.35)'; g.fillRect(0, y, w, 3);                 // a floor line
    }
    g.fillStyle = '#b8a587'; g.fillRect(0, 0, w, 7);                              // cornice
  }));
}

// ---------------------------------------------------------------------------------------------------------------------
// Pieces

const std = (opts) => new THREE.MeshStandardMaterial({ roughness: 0.92, metalness: 0, ...opts });
const plain = color => once('plain' + color, () => std({ color }));

export class City {
  constructor(id, later) {
    this.group = new THREE.Group();
    this.solids = [];
    this.animated = [];           // (dt, time) => void
    this.later = later;
    this.rnd = sequence(id + (later ? ':later' : ''));
  }
  add(mesh, shadow = true) { if (shadow) { mesh.castShadow = true; mesh.receiveShadow = true; } this.group.add(mesh); return mesh; }
  solid(x, z, hw, hd) { this.solids.push({ x, z, hw: hw + 0.45, hd: hd + 0.45 }); }
  box(w, h, d, material, x, y, z, solid) {
    const m = this.add(new THREE.Mesh(new THREE.BoxGeometry(w, h, d), material));
    m.position.set(x, y + h / 2, z);
    if (solid) this.solid(x, z, w / 2, d / 2);
    return m;
  }
  cyl(r1, r2, h, material, x, y, z, seg = 16, solid) {
    const m = this.add(new THREE.Mesh(new THREE.CylinderGeometry(r1, r2, h, seg), material));
    m.position.set(x, y + h / 2, z);
    if (solid) this.solid(x, z, Math.max(r1, r2), Math.max(r1, r2));
    return m;
  }

  ground(texture, size, repeat) {
    const t = texture.clone(); t.needsUpdate = true; t.repeat.set(repeat, repeat);
    const m = new THREE.Mesh(new THREE.PlaneGeometry(size, size), std({ map: t }));
    m.rotation.x = -Math.PI / 2; m.receiveShadow = true;
    this.group.add(m);
  }
  paving(w, d, x, z) {
    const t = cobble(this.later).clone(); t.needsUpdate = true; t.repeat.set(w / 6, d / 6);
    const m = new THREE.Mesh(new THREE.PlaneGeometry(w, d), std({ map: t }));
    m.rotation.x = -Math.PI / 2; m.position.set(x, 0.02, z); m.receiveShadow = true;
    this.group.add(m);
  }

  kitPiece(name) { return KIT[name].clone(); }
  // Street paving in the pack's dark basalt polygons.
  pave(w, d, x, z) {
    const m = new THREE.Mesh(new THREE.PlaneGeometry(w, d), kitMaterial('basalt', 3, w, d, this.later ? 0xd8d2c8 : 0xffffff));
    m.rotation.x = -Math.PI / 2; m.position.set(x, 0.03, z); m.receiveShadow = true; this.group.add(m);
  }
  // The firewall of the Forum of Augustus: a sheer face of grey stone blocks, far taller than the houses.
  firewall(x0, x1, z, height) {
    const w = x1 - x0;
    const m = new THREE.Mesh(new THREE.BoxGeometry(w, height, 3), kitMaterial('travertine', 4.8, w, height, this.later ? 0x8a857c : 0x9c9588));
    m.position.set((x0 + x1) / 2, height / 2, z + 1.5); m.castShadow = m.receiveShadow = true; this.group.add(m);
    const cap = new THREE.Mesh(new THREE.BoxGeometry(w + 0.6, 0.9, 3.6), kitMaterial('travertine', 4.8, w, 1, 0x8a8478)); cap.position.set((x0 + x1) / 2, height + 0.45, z + 1.5); this.group.add(cap);
    this.solid((x0 + x1) / 2, z + 1.5, w / 2, 1.5);
  }
  // The arch through the firewall (a dark opening framed in travertine).
  wallArch(x, z) {
    const stone = kitMaterial('travertine', 2.4, 2, 9, 0xe6dcc6), dark = new THREE.MeshStandardMaterial({ color: 0x14100c, roughness: 1 });
    const hole = new THREE.Mesh(new THREE.BoxGeometry(4.2, 6.4, 0.3), dark); hole.position.set(x, 3.2, z + 0.02); this.group.add(hole);
    const top = new THREE.Mesh(new THREE.CylinderGeometry(2.1, 2.1, 0.3, 20, 1, false, 0, Math.PI), dark); top.rotation.set(Math.PI / 2, 0, Math.PI / 2); top.position.set(x, 6.4, z + 0.02); this.group.add(top);
    for (const sx of [-2.6, 2.6]) { const j = new THREE.Mesh(new THREE.BoxGeometry(1, 6.6, 0.6), stone); j.position.set(x + sx, 3.3, z - 0.1); j.castShadow = true; this.group.add(j); }
    const ring = new THREE.Mesh(new THREE.TorusGeometry(2.6, 0.5, 6, 16, Math.PI), stone); ring.position.set(x, 6.5, z - 0.1); ring.castShadow = true; this.group.add(ring);
  }
  // A low yard wall of plastered rubble.
  lowWall(x0, x1, z0, z1, alongZ) {
    const len = alongZ ? Math.abs(z1 - z0) : Math.abs(x1 - x0), cx = alongZ ? x0 : (x0 + x1) / 2, cz = alongZ ? (z0 + z1) / 2 : z0;
    const m = new THREE.Mesh(new THREE.BoxGeometry(alongZ ? 0.45 : len, 1.5, alongZ ? len : 0.45), kitMaterial('plaster', 3, len, 1.5, 0xd9c7a8));
    m.position.set(cx, 0.75, cz); m.castShadow = m.receiveShadow = true; this.group.add(m);
    this.solid(cx, cz, alongZ ? 0.25 : len / 2, alongZ ? len / 2 : 0.25);
  }
  // A courtyard well: a stone ring, a timber frame, a bucket.
  well(x, z) {
    const ring = new THREE.Mesh(new THREE.CylinderGeometry(0.9, 0.95, 0.9, 14, 1, true), kitMaterial('travertine', 2.4, 5.6, 0.9)); ring.material.side = THREE.DoubleSide; ring.position.set(x, 0.45, z); ring.castShadow = true; this.group.add(ring);
    const water = new THREE.Mesh(new THREE.CircleGeometry(0.85, 14), new THREE.MeshStandardMaterial({ color: 0x1c2a30, roughness: 0.1 })); water.rotation.x = -Math.PI / 2; water.position.set(x, 0.3, z); this.group.add(water);
    const wood = kitMaterial('wood', 1.5, 0.3, 2.2);
    for (const sx of [-1, 1]) { const p = new THREE.Mesh(new THREE.BoxGeometry(0.12, 2.2, 0.12), wood); p.position.set(x + sx, 1.1, z); p.castShadow = true; this.group.add(p); }
    const beam = new THREE.Mesh(new THREE.CylinderGeometry(0.07, 0.07, 2.2, 8), wood); beam.rotation.z = Math.PI / 2; beam.position.set(x, 2.1, z); this.group.add(beam);
    const bucket = new THREE.Mesh(new THREE.CylinderGeometry(0.18, 0.14, 0.3, 10), wood); bucket.position.set(x + 0.2, 1.3, z); this.group.add(bucket);
    this.solid(x, z, 1, 1);
  }
  // The shared foot: a bronze measure let into a wall for anyone to check a rule against.
  bronzeFoot(x, z) {
    const bronze = MATS.bronze.clone();
    const plate = new THREE.Mesh(new THREE.BoxGeometry(0.5, 0.16, 0.05), bronze); plate.position.set(x, 1.35, z); this.group.add(plate);
    const slab = new THREE.Mesh(new THREE.BoxGeometry(0.9, 0.5, 0.08), kitMaterial('marble', 3, 0.9, 0.5)); slab.position.set(x, 1.35, z - 0.04); this.group.add(slab);
  }
  // A street standpipe with a bronze tap.
  standpipe(x, z) {
    const stone = new THREE.Mesh(new THREE.BoxGeometry(0.45, 1.1, 0.45), kitMaterial('travertine', 2.4, 0.45, 1.1)); stone.position.set(x, 0.55, z); stone.castShadow = true; this.group.add(stone);
    const tap = new THREE.Mesh(new THREE.CylinderGeometry(0.04, 0.04, 0.3, 8), MATS.bronze.clone()); tap.rotation.x = Math.PI / 2; tap.position.set(x, 0.85, z + 0.3); this.group.add(tap);
    const basin = new THREE.Mesh(new THREE.BoxGeometry(0.6, 0.25, 0.5), kitMaterial('travertine', 2.4, 0.6, 0.25)); basin.position.set(x, 0.12, z + 0.5); this.group.add(basin);
    this.solid(x, z, 0.4, 0.5);
  }
  // A piece of the art pack, set down facing `turn` (0 looks toward +z).
  kit(name, x, z, turn = 0, scale = 1, solid = true) {
    const o = KIT[name].clone();
    o.position.set(x, 0, z); o.rotation.y = turn; o.scale.setScalar(scale);
    this.group.add(o);
    const hw = (KIT[name].userData.hw || 1) * scale, hd = (KIT[name].userData.hd || 1) * scale, side = Math.abs(Math.sin(turn)) > 0.5;
    if (solid) this.solid(x, z, side ? hd : hw, side ? hw : hd);
    return o;
  }
  // A tenement from the art pack, its shops facing the middle of the district.
  insula(x, z, bays, depth, floors, turned, name) {
    name = name || INSULAE[Math.floor(this.rnd() * INSULAE.length)];
    const turn = turned ? (x < 0 ? Math.PI / 2 : -Math.PI / 2) : (z < 6 ? 0 : Math.PI);
    const g = new THREE.Group(); g.position.set(x, 0, z); g.rotation.y = turn; this.group.add(g);
    const o = KIT[name].clone(); g.add(o);
    g.userData.hd = KIT[name].userData.hd;
    const hw = KIT[name].userData.hw, hd = KIT[name].userData.hd;
    this.solid(x, z, turned ? hd : hw, turned ? hw : hd);
    return g;
  }
  // A row of tenements shoulder to shoulder along one edge. `skip(p)` leaves a gap (a gate).
  insulaRow(fixed, from, to, alongX, skip) {
    let p = from;
    while (p < to - 4) {
      const name = INSULAE[Math.floor(this.rnd() * INSULAE.length)], w = KIT[name].userData.hw * 2;
      const mid = p + w / 2;
      if (!skip || !skip(mid, w)) this.insula(alongX ? mid : fixed, alongX ? fixed : mid, 0, 0, 0, !alongX, name);
      p += w + 0.05;
    }
  }
  insulaCoded(x, z, bays, depth, floors, turned) {
    const style = Math.floor(this.rnd() * 6), w = bays * 2.6, h = floors * 2.9;
    const front = std({ map: facade(style, bays, floors, this.later) });
    const sideBays = Math.max(1, Math.round(depth / 2.6));
    const side = std({ map: facade(style + 1, sideBays, floors, this.later) });
    const top = plain(0x6f6253);
    const g = new THREE.Group();
    const body = new THREE.Mesh(new THREE.BoxGeometry(w, h, depth), [side, side, top, top, front, front]);
    body.position.y = h / 2; body.castShadow = body.receiveShadow = true;
    const roofTex = tiles(this.later).clone(); roofTex.needsUpdate = true; roofTex.repeat.set(3, 2);
    const roof = new THREE.Mesh(new THREE.ConeGeometry(0.7072, 1, 4, 1), std({ map: roofTex }));
    roof.rotation.y = Math.PI / 4; roof.scale.set(w + 1.1, 1.5 + this.rnd() * 0.6, depth + 1.1); roof.position.y = h + roof.scale.y / 2;
    roof.castShadow = true;
    const eaves = new THREE.Mesh(new THREE.BoxGeometry(w + 1, 0.16, depth + 1), plain(this.later ? 0x6d4a3b : 0x8f4a2f));
    eaves.position.y = h + 0.05; eaves.castShadow = true;
    g.add(body, roof, eaves);
    if (this.rnd() < 0.45) {                                       // an awning over a shop
      const awn = new THREE.Mesh(new THREE.PlaneGeometry(2.4, 1.5), new THREE.MeshStandardMaterial({ map: stripes(['#a8482c', '#3f6077', '#7a6a3f'][style % 3]), side: THREE.DoubleSide, roughness: 1 }));
      awn.position.set((Math.floor(this.rnd() * bays) - (bays - 1) / 2) * 2.6, 2.55, depth / 2 + 0.6); awn.rotation.x = -Math.PI / 2 + 0.5; awn.castShadow = true;
      g.add(awn);
    }
    g.position.set(x, 0, z);
    if (turned) g.rotation.y = Math.PI / 2;
    this.group.add(g);
    this.solid(x, z, (turned ? depth : w) / 2, (turned ? w : depth) / 2);
    return g;
  }

  column(x, z, h, r = 0.42, solid = true) {
    const o = KIT.column_tuscan.clone(); o.position.set(x, 0, z); o.scale.set(r / 0.42, h / 6, r / 0.42); this.group.add(o);
    if (solid) this.solid(x, z, r * 1.4, r * 1.4);
  }
  columnCoded(x, z, h, r = 0.42, solid = true) {
    const m = std({ map: marble(this.later) });
    this.cyl(r * 1.25, r * 1.35, 0.3, m, x, 0, z, 12, solid);
    this.cyl(r * 0.86, r, h - 0.7, m, x, 0.3, z, 14);
    this.box(r * 2.5, 0.4, r * 2.5, m, x, h - 0.4, z);
  }
  temple(x, z, cols = 6, turn = 0) { this.kit('temple', x, z, turn); }
  templeCoded(x, z, cols = 6, turn = 0) {
    const m = std({ map: marble(this.later) }), g0 = this.group, g = new THREE.Group();
    this.group = g;
    const w = cols * 2.3, d = 11, colH = 6;
    for (let k = 0; k < 3; k++) this.box(w + 3 - k, 0.35, d + 3 - k, m, 0, k * 0.35, 0);
    this.box(w - 2.4, colH, d - 4.4, std({ map: marble(this.later), color: 0xe4d8c2 }), 0, 1.05, -1);
    this.box(1.8, 3.6, 0.2, std({ map: wood(), color: 0x8a6a4a }), 0, 1.05, d / 2 - 3.1);
    for (let k = 0; k < cols; k++) this.column((k - (cols - 1) / 2) * 2.3, d / 2 - 0.9, colH + 1.05, 0.42, false);
    for (const sx of [-1, 1]) for (let k = 1; k < 4; k++) this.column(sx * (cols - 1) / 2 * 2.3, d / 2 - 0.9 - k * 2.6, colH + 1.05, 0.42, false);
    this.box(w + 0.6, 0.8, d - 0.4, m, 0, colH + 1.05, 0);
    const tri = new THREE.Shape(); tri.moveTo(-w / 2 - 0.5, 0); tri.lineTo(w / 2 + 0.5, 0); tri.lineTo(0, 2.4); tri.closePath();
    const ped = this.add(new THREE.Mesh(new THREE.ExtrudeGeometry(tri, { depth: d - 0.2, bevelEnabled: false }), std({ color: this.later ? 0x8a5a45 : 0xb5553a, map: null })));
    ped.position.set(0, colH + 1.85, -(d - 0.2) / 2);
    const face = this.add(new THREE.Mesh(new THREE.ExtrudeGeometry(tri, { depth: 0.12, bevelEnabled: false }), m));
    face.position.set(0, colH + 1.85, d / 2 - 0.16);
    this.group = g0;
    g.position.set(x, 0, z); g.rotation.y = turn;
    this.group.add(g);
    const side = Math.abs(Math.sin(turn)) > 0.5;
    this.solid(x, z, (side ? d : w) / 2 + 1.2, (side ? w : d) / 2 + 1.2);
  }
  stall(x, z, color, turn = 0) {
    const g0 = this.group, g = new THREE.Group(); this.group = g;
    this.box(3, 0.95, 1.4, std({ map: wood() }), 0, 0, 0);
    for (const [sx, sz] of [[-1.45, -0.7], [1.45, -0.7], [-1.45, 0.9], [1.45, 0.9]]) this.cyl(0.06, 0.06, 2.5, plain(0x5a4632), sx, 0, sz, 6);
    const awn = this.add(new THREE.Mesh(new THREE.PlaneGeometry(3.4, 2.2), new THREE.MeshStandardMaterial({ map: stripes(color), side: THREE.DoubleSide, roughness: 1 })));
    awn.position.set(0, 2.5, 0.1); awn.rotation.x = -Math.PI / 2 + 0.22;
    const goods = [0xc9542c, 0xe0b23c, 0x7a9a3f, 0x8a4a6a, 0xd8c8a0];
    for (let k = 0; k < 9; k++) { const s = this.add(new THREE.Mesh(new THREE.SphereGeometry(0.13 + this.rnd() * 0.07, 8, 6), plain(goods[Math.floor(this.rnd() * goods.length)]))); s.position.set(-1.2 + (k % 5) * 0.6, 1.08, -0.3 + Math.floor(k / 5) * 0.5); }
    this.group = g0;
    g.position.set(x, 0, z); g.rotation.y = turn; this.group.add(g);
    this.solid(x, z, 1.6, 1.2);
  }
  amphora(x, z, s = 1) { const o = this.kit(this.rnd() < 0.5 ? 'amphora_oil' : 'amphora_wine', x, z, this.rnd() * 6, s * 1.15, false); o.rotation.z = (this.rnd() - 0.5) * 0.5; if (Math.abs(o.rotation.z) > 0.15) o.rotation.z = (this.rnd() - 0.5) * 0.1; }
  amphoraCoded(x, z, s = 1) {
    const pts = [[0.05, 0], [0.16, 0.05], [0.3, 0.35], [0.33, 0.6], [0.24, 0.85], [0.1, 0.98], [0.1, 1.15], [0.14, 1.2]].map(p => new THREE.Vector2(p[0] * s, p[1] * s));
    const m = this.add(new THREE.Mesh(new THREE.LatheGeometry(pts, 12), plain(0xb0683f)));
    m.position.set(x, 0, z); m.rotation.z = (this.rnd() - 0.5) * 0.12;
  }
  crate(x, z) { const s = 0.7 + this.rnd() * 0.4; const m = this.box(s, s, s, std({ map: wood() }), x, 0, z, true); m.rotation.y = this.rnd(); }
  cart(x, z, turn = 0) {
    const g0 = this.group, g = new THREE.Group(); this.group = g;
    this.box(2.6, 0.25, 1.4, std({ map: wood() }), 0, 0.75, 0);
    for (const sz of [-0.68, 0.68]) this.box(2.6, 0.45, 0.08, std({ map: wood() }), 0, 1, sz);
    for (const sz of [-0.8, 0.8]) { const w = this.add(new THREE.Mesh(new THREE.TorusGeometry(0.62, 0.09, 6, 16), plain(0x4a3524))); w.position.set(0, 0.7, sz); }
    this.box(1.8, 0.08, 0.08, plain(0x4a3524), 2.1, 0.8, 0);
    this.group = g0; g.position.set(x, 0, z); g.rotation.y = turn; this.group.add(g); this.solid(x, z, 1.5, 1.1);
  }
  cypress(x, z) {
    this.cyl(0.14, 0.18, 1, plain(0x5a4632), x, 0, z, 6, true);
    const c = this.add(new THREE.Mesh(new THREE.ConeGeometry(0.75, 5 + this.rnd() * 2, 9), plain(this.later ? 0x3f5340 : 0x2f5a3a)));
    c.position.set(x, 3.6, z);
  }
  pine(x, z) {
    this.cyl(0.16, 0.22, 4.2, plain(0x6b5240), x, 0, z, 7, true);
    const c = this.add(new THREE.Mesh(new THREE.SphereGeometry(2.1, 10, 7), plain(0x3f6a3f))); c.scale.y = 0.45; c.position.set(x, 4.9, z);
  }
  brazier(x, z) {
    this.cyl(0.08, 0.2, 1, plain(0x3a3a3a), x, 0, z, 6);
    this.cyl(0.4, 0.2, 0.3, plain(0x3a3a3a), x, 1, z, 8);
    const fire = new THREE.Mesh(new THREE.ConeGeometry(0.25, 0.6, 6), new THREE.MeshBasicMaterial({ color: 0xff9a3c })); fire.position.set(x, 1.55, z); this.group.add(fire);
    const light = new THREE.PointLight(0xff9a4c, 6, 9, 2); light.position.set(x, 1.8, z); this.group.add(light);
    this.animated.push((dt, t) => { fire.scale.y = 1 + Math.sin(t * 11 + x) * 0.2; light.intensity = 5 + Math.sin(t * 13 + z) * 1.5; });
  }
  fountain(x, z, clean) {
    const how = clean === true ? 'clean' : clean === false ? 'foul' : clean;       // 'clean', 'foul', 'worn' or 'dry'
    const o = this.kit('street_fountain', x, z, 0, 1.5);
    o.traverse(m => { if (m.isMesh && m.material.name === 'water') {
      m.material = m.material.clone(); m.material.transparent = true; m.material.opacity = 0.85; m.material.roughness = how === 'clean' ? 0.08 : 0.5;
      m.material.color.set(how === 'clean' ? 0x4f96b8 : how === 'worn' ? 0x5f7a5a : 0x6b6a3a); m.visible = how !== 'dry';
    } });
  }
  fountainCoded(x, z, clean) {
    const stone = std({ map: marble(this.later) });
    this.cyl(2.5, 2.7, 0.75, stone, x, 0, z, 8, true);
    const wt = water().clone(); wt.needsUpdate = true;
    const pool = new THREE.Mesh(new THREE.CircleGeometry(2.2, 20), new THREE.MeshStandardMaterial({ map: wt, color: clean ? 0xffffff : 0x8a8f5a, roughness: 0.15, metalness: 0.1, transparent: true, opacity: 0.9 }));
    pool.rotation.x = -Math.PI / 2; pool.position.set(x, 0.72, z); this.group.add(pool);
    this.cyl(0.3, 0.4, 1.9, stone, x, 0.7, z, 10);
    this.cyl(0.95, 0.3, 0.3, stone, x, 2.6, z, 12);
    const jet = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.12, 0.7, 6), new THREE.MeshBasicMaterial({ color: 0xcfe9f5, transparent: true, opacity: 0.7 })); jet.position.set(x, 3.2, z); this.group.add(jet);
    this.animated.push((dt, t) => { wt.offset.x = t * 0.02; wt.offset.y = t * 0.015; jet.scale.y = 1 + Math.sin(t * 6) * 0.15; });
  }
  // Letters cut in stone.
  inscription(text, w, h, x, y, z, turn = 0) {
    const t = canvas(512, 64, (g, cw, ch) => { g.fillStyle = this.later ? '#c9c2b4' : '#e9e3d6'; g.fillRect(0, 0, cw, ch); g.fillStyle = '#6a4a2a'; g.font = 'bold 30px Georgia, serif'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(text, cw / 2, ch / 2 + 2, cw - 16); });
    t.wrapS = t.wrapT = THREE.ClampToEdgeWrapping;
    const m = new THREE.Mesh(new THREE.PlaneGeometry(w, h), std({ map: t }));
    m.position.set(x, y, z); m.rotation.y = turn; this.group.add(m);
    return m;
  }
  // A crossroads shrine to the Lares: a small painted niche on a pillar, with a lamp.
  shrine(x, z) {
    const m = std({ map: marble(this.later) });
    this.box(1.3, 1.5, 0.9, m, x, 0, z, true);
    this.box(1.1, 1.2, 0.7, std({ color: 0x8a2f26 }), x, 1.5, z);
    this.box(0.7, 0.8, 0.1, std({ color: 0xe8d8a8 }), x, 1.7, z + 0.36);
    const tri = new THREE.Shape(); tri.moveTo(-0.75, 0); tri.lineTo(0.75, 0); tri.lineTo(0, 0.5); tri.closePath();
    const ped = this.add(new THREE.Mesh(new THREE.ExtrudeGeometry(tri, { depth: 0.9, bevelEnabled: false }), m)); ped.position.set(x, 2.7, z - 0.45);
    const flame = new THREE.Mesh(new THREE.SphereGeometry(0.07, 6, 5), new THREE.MeshBasicMaterial({ color: 0xffc46a })); flame.position.set(x, 1.62, z + 0.5); this.group.add(flame);
    const light = new THREE.PointLight(0xffb45a, 2.5, 5, 2); light.position.set(x, 1.8, z + 0.8); this.group.add(light);
    this.animated.push((dt, t) => { light.intensity = 2.2 + Math.sin(t * 12) * 0.5; });
  }
  // A thermopolium: the street bar, an L of masonry with wine jars sunk in the counter.
  thermopolium(x, z, turn = 0) {
    const g0 = this.group, g = new THREE.Group(); this.group = g;
    const counter = std({ map: marble(this.later), color: 0xd8b8a0 });
    this.box(4, 1, 0.9, counter, 0, 0, 0); this.box(0.9, 1, 2.4, counter, -1.55, 0, 1.2);
    for (const [cx, cz] of [[-0.9, 0], [0.2, 0], [1.3, 0], [-1.55, 1.3]]) { const jar = this.add(new THREE.Mesh(new THREE.CylinderGeometry(0.28, 0.28, 0.06, 12), plain(0x3a2a20))); jar.position.set(cx, 1.01, cz); }
    for (let k = 0; k < 3; k++) this.box(1.1, 0.06, 0.3, std({ map: wood() }), 0.2, 1.6 + k * 0.45, -0.75);
    for (let k = 0; k < 7; k++) { const cup = this.add(new THREE.Mesh(new THREE.CylinderGeometry(0.07, 0.05, 0.13, 8), plain(0xb0683f))); cup.position.set(-0.25 + (k % 4) * 0.3, 1.7 + Math.floor(k / 4) * 0.45, -0.75); }
    this.group = g0; g.position.set(x, 0, z); g.rotation.y = turn; this.group.add(g); this.solid(x, z, 2.2, 1.8);
  }
  // Washing strung across the street.
  laundry(x1, z1, x2, z2, y) {
    const len = Math.hypot(x2 - x1, z2 - z1), a = Math.atan2(z2 - z1, x2 - x1);
    const rope = new THREE.Mesh(new THREE.CylinderGeometry(0.015, 0.015, len, 4), plain(0x5a4632)); rope.rotation.z = Math.PI / 2; rope.rotation.y = -a; rope.position.set((x1 + x2) / 2, y, (z1 + z2) / 2); this.group.add(rope);
    const cols = [0xe8e0d0, 0x9a5a4a, 0x6a7a8a, 0xd8c8a0, 0x7a8a6a];
    for (let k = 1; k < 7; k++) {
      const c = new THREE.Mesh(new THREE.PlaneGeometry(0.7 + this.rnd() * 0.5, 0.8 + this.rnd() * 0.5), new THREE.MeshStandardMaterial({ color: cols[k % cols.length], side: THREE.DoubleSide, roughness: 1 }));
      const f = k / 7; c.position.set(x1 + (x2 - x1) * f, y - 0.5, z1 + (z2 - z1) * f); c.rotation.y = -a; c.castShadow = true; this.group.add(c);
      const base = c.rotation.x, ph = this.rnd() * 6; this.animated.push((dt, t) => { c.rotation.x = base + Math.sin(t * 1.6 + ph) * 0.12; });
    }
  }
  // A triumphal column wound with a carved band, a bronze figure on top.
  spiralColumn(x, z) {
    const band = canvas(256, 512, (g, w, h) => { g.fillStyle = this.later ? '#c2bbad' : '#e6dfd0'; g.fillRect(0, 0, w, h); g.strokeStyle = 'rgba(110,98,84,.55)'; g.lineWidth = 3; for (let k = -2; k < 12; k++) { g.beginPath(); g.moveTo(0, k * 48); g.lineTo(w, k * 48 + 48); g.stroke(); } const rnd = sequence('band'); for (let i = 0; i < 520; i++) { g.fillStyle = 'rgba(95,84,70,.45)'; const y = rnd() * h, x0 = rnd() * w; g.fillRect(x0, y, 3, 9 + rnd() * 6); } });
    this.box(3.4, 2.6, 3.4, std({ map: marble(this.later) }), x, 0, z, true);
    const shaft = this.add(new THREE.Mesh(new THREE.CylinderGeometry(1.05, 1.2, 17, 20), std({ map: band }))); shaft.position.set(x, 11.1, z);
    this.box(2.8, 0.7, 2.8, std({ map: marble(this.later) }), x, 19.6, z);
    const man = figure(0x8a7a4a, { rnd: sequence('column'), scale: 1.5, toga: 0x8a7a4a, long: true, hair: 0x8a7a4a, skin: 0x8a7a4a }); man.position.set(x, 20.3, z); this.group.add(man);
  }
  // The Senate house: a tall brick hall with bronze doors.
  curia(x, z) {
    const brick = canvas(256, 256, (g, w, h) => { const rnd = sequence('brick'); g.fillStyle = this.later ? '#8f6a55' : '#b06a48'; g.fillRect(0, 0, w, h); for (let y = 0; y < 32; y++) for (let k = 0; k < 9; k++) { const v = rnd() * 26 - 13; g.fillStyle = `rgba(${150 + v},${86 + v},${58 + v},.9)`; g.fillRect(k * 30 + (y % 2) * 15 - 15, y * 8, 28, 6.5); } });
    brick.repeat.set(2, 2);
    this.box(11, 13, 9, std({ map: brick }), x, 0, z, true);
    for (let k = 0; k < 4; k++) this.box(13 - k * 0.6, 0.3, 3, std({ map: marble(this.later) }), x, k * 0.3, z + 5.6 - k * 0.45);
    this.box(3, 5.6, 0.25, new THREE.MeshStandardMaterial({ color: 0x8a6a2f, metalness: 0.8, roughness: 0.35 }), x, 1.2, z + 4.55);
    for (const sx of [-2.6, 0, 2.6]) this.box(1.5, 2.4, 0.1, plain(0x241c16), x + sx, 8.4, z + 4.52);
    const tri = new THREE.Shape(); tri.moveTo(-6, 0); tri.lineTo(6, 0); tri.lineTo(0, 2.6); tri.closePath();
    const ped = this.add(new THREE.Mesh(new THREE.ExtrudeGeometry(tri, { depth: 9.6, bevelEnabled: false }), std({ color: this.later ? 0x8a5a45 : 0xb5553a }))); ped.position.set(x, 13, z - 4.8);
  }
  // The speakers' platform, hung with the beaks of ships.
  rostra(x, z) {
    this.box(12, 1.6, 4, std({ map: marble(this.later) }), x, 0, z, true);
    for (let k = -2; k <= 2; k++) { const beak = this.add(new THREE.Mesh(new THREE.ConeGeometry(0.22, 0.9, 6), new THREE.MeshStandardMaterial({ color: 0x8a6a2f, metalness: 0.8, roughness: 0.4 }))); beak.rotation.x = Math.PI / 2; beak.position.set(x + k * 2.2, 0.95, z + 2.4); }
  }
  // The round pavilion in the middle of a Roman market.
  tholos(x, z) {
    const m = std({ map: marble(this.later) });
    this.cyl(4.2, 4.4, 0.5, m, x, 0, z, 20, false);
    for (let k = 0; k < 10; k++) { const a = k / 10 * Math.PI * 2; this.column(x + Math.cos(a) * 3.6, z + Math.sin(a) * 3.6, 4.4, 0.24, false); }
    const t = tiles(this.later).clone(); t.needsUpdate = true; t.repeat.set(6, 2);
    const roof = this.add(new THREE.Mesh(new THREE.ConeGeometry(4.6, 2.2, 20), std({ map: t }))); roof.position.set(x, 6, z);
    this.cyl(1.1, 1.2, 0.9, m, x, 0.5, z, 12); this.solid(x, z, 4.2, 4.2);
  }
  // The umbrella pine of the Roman skyline.
  umbrellaPine(x, z, s = 1) {
    this.cyl(0.16 * s, 0.26 * s, 6 * s, plain(0x6b5240), x, 0, z, 7, s < 2);
    for (const [ox, oz, r] of [[0, 0, 2.6], [1.3, 0.6, 1.7], [-1.2, -0.7, 1.8]]) { const c = this.add(new THREE.Mesh(new THREE.SphereGeometry(r * s, 10, 7), plain(this.later ? 0x4a5f45 : 0x3d6a3a))); c.scale.y = 0.38; c.position.set(x + ox * s, 6.4 * s, z + oz * s); }
  }
  // What stands beyond the district: the city's skyline. Far off, hazed, never walked to.
  skyline() {
    const hazy = c => new THREE.MeshStandardMaterial({ color: c, roughness: 1 });
    const far = (m) => { m.castShadow = false; m.receiveShadow = false; this.group.add(m); return m; };
    // The amphitheatre: three tiers of arches and an attic.
    const arcade = canvas(128, 256, (g, w, h) => {
      g.fillStyle = this.later ? '#b8ae9c' : '#d9cdb6'; g.fillRect(0, 0, w, h);
      for (let tier = 0; tier < 3; tier++) { const y = h - (tier + 1) * 66; g.fillStyle = '#3a2f28'; g.beginPath(); g.moveTo(34, y + 62); g.lineTo(34, y + 30); g.arc(64, y + 30, 30, Math.PI, 0); g.lineTo(94, y + 62); g.closePath(); g.fill(); g.fillStyle = 'rgba(90,78,64,.5)'; g.fillRect(0, y + 62, w, 4); g.fillRect(8, y, 10, 62); g.fillRect(110, y, 10, 62); }
      g.fillStyle = '#3a2f28'; g.fillRect(52, 20, 24, 22);
    });
    arcade.repeat.set(28, 1);
    const col = far(new THREE.Mesh(new THREE.CylinderGeometry(34, 34, 40, 56, 1, true), new THREE.MeshStandardMaterial({ map: arcade, side: THREE.DoubleSide, roughness: 1 })));
    col.scale.z = 0.8; col.position.set(78, 20, -74);
    // The aqueduct, striding in on two tiers of arches.
    const arches = canvas(128, 128, (g, w, h) => { g.clearRect(0, 0, w, h); g.fillStyle = this.later ? '#a89c8a' : '#c8b89c'; g.fillRect(0, 0, w, h); for (const [y, r] of [[64, 44], [8, 20]]) { g.globalCompositeOperation = 'destination-out'; if (r === 44) { g.beginPath(); g.moveTo(20, h); g.lineTo(20, y + 20); g.arc(64, y + 20, 44, Math.PI, 0); g.lineTo(108, h); g.closePath(); g.fill(); } else { for (const cx of [32, 96]) { g.beginPath(); g.moveTo(cx - 20, 58); g.lineTo(cx - 20, y + 22); g.arc(cx, y + 22, 20, Math.PI, 0); g.lineTo(cx + 20, 58); g.closePath(); g.fill(); } } g.globalCompositeOperation = 'source-over'; } });
    arches.repeat.set(22, 1);
    const aq = far(new THREE.Mesh(new THREE.PlaneGeometry(260, 24), new THREE.MeshStandardMaterial({ map: arches, transparent: true, alphaTest: 0.5, side: THREE.DoubleSide, roughness: 1 })));
    aq.position.set(-30, 12, -96); aq.rotation.y = 0.18;
    // The hills, with their pines and a temple on the height.
    for (const [x, z, r, h] of [[-95, -40, 46, 26], [-70, 80, 50, 22], [95, 60, 44, 24], [10, 118, 60, 20], [120, -10, 40, 18]]) {
      const hill = far(new THREE.Mesh(new THREE.SphereGeometry(r, 18, 10, 0, Math.PI * 2, 0, Math.PI / 2), hazy(this.later ? 0x8a8a70 : 0xa39a68))); hill.scale.y = h / r; hill.position.set(x, -1, z);
      for (let k = 0; k < 7; k++) { const a = this.rnd() * Math.PI * 2, d = this.rnd() * r * 0.55; const px = x + Math.cos(a) * d, pz = z + Math.sin(a) * d, py = h * Math.sqrt(Math.max(0, 1 - (d / r) ** 2)) - 1;
        const trunk = far(new THREE.Mesh(new THREE.CylinderGeometry(0.4, 0.6, 9, 5), hazy(0x6b5240))); trunk.position.set(px, py + 4.5, pz);
        const crown = far(new THREE.Mesh(new THREE.SphereGeometry(5, 8, 6), hazy(this.later ? 0x4a5f45 : 0x3d6a3a))); crown.scale.y = 0.36; crown.position.set(px, py + 9.5, pz); }
    }
    const g0 = this.group, g = new THREE.Group(); this.group = g;
    this.kit('temple', 0, 0, 0, 1, false); this.group = g0; g.scale.setScalar(1.6); g.position.set(-95, 24.5, -40); g.rotation.y = 0.9; g.traverse(o => { o.castShadow = false; }); this.group.add(g);
    const g2 = new THREE.Group(); this.group = g2; this.kit('insula_f', 0, 0, 0, 1, false); this.group = g0; g2.scale.setScalar(2.2); g2.position.set(95, 22, 60); g2.traverse(o => { o.castShadow = false; }); this.group.add(g2);
  }
  arch(x, z, turn = 0) {
    const m = std({ map: marble(this.later) }), g0 = this.group, g = new THREE.Group(); this.group = g;
    this.box(1.4, 6, 1.6, m, -3.2, 0, 0); this.box(1.4, 6, 1.6, m, 3.2, 0, 0); this.box(8.4, 1.6, 1.8, m, 0, 6, 0); this.box(7.4, 0.5, 1.4, m, 0, 5.5, 0);
    this.group = g0; g.position.set(x, 0, z); g.rotation.y = turn; this.group.add(g);
    const side = Math.abs(Math.sin(turn)) > 0.5;
    for (const s of [-3.2, 3.2]) this.solid(x + (side ? 0 : s), z + (side ? s : 0), 0.8, 0.8);
  }
  shed(x, z) {                                                     // a lean-to over the machine
    for (const [sx, sz] of [[-2.6, -2.2], [2.6, -2.2], [-2.6, 2.2], [2.6, 2.2]]) this.cyl(0.1, 0.1, 3.4, plain(0x5a4632), x + sx, 0, z + sz, 6);
    const t = tiles(this.later).clone(); t.needsUpdate = true; t.repeat.set(2, 2);
    const roof = this.add(new THREE.Mesh(new THREE.BoxGeometry(6.2, 0.14, 5.4), std({ map: t }))); roof.position.set(x, 3.55, z); roof.rotation.z = 0.1;
  }
  machine(x, z, ready) {
    const g = new THREE.Group();
    const dark = new THREE.MeshStandardMaterial({ color: 0x2b3038, metalness: 0.75, roughness: 0.35 });
    const core = new THREE.Mesh(new THREE.IcosahedronGeometry(0.95, 1), new THREE.MeshStandardMaterial({ color: 0x1c2733, metalness: 0.6, roughness: 0.25, emissive: ready ? 0x3d8fe0 : 0x16324a, emissiveIntensity: ready ? 1.6 : 0.5 }));
    core.position.y = 1.9;
    const brass = new THREE.MeshStandardMaterial({ color: 0xa8905a, metalness: 0.85, roughness: 0.3 });
    const rings = [0, 1, 2].map(k => { const r = new THREE.Mesh(new THREE.TorusGeometry(1.45 + k * 0.18, 0.06, 8, 36), brass); r.position.y = 1.9; r.rotation.set(Math.PI / 2 + k * 0.5, k * 0.7, 0); return r; });
    const base = new THREE.Mesh(new THREE.CylinderGeometry(1.3, 1.6, 0.6, 10), dark); base.position.y = 0.3;
    const seat = new THREE.Mesh(new THREE.BoxGeometry(0.9, 1.1, 0.9), dark); seat.position.set(0, 1.0, 0);
    g.add(core, base, seat, ...rings);
    for (let k = 0; k < 5; k++) { const p = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.05, 1.6, 6), brass); const a = k / 5 * Math.PI * 2; p.position.set(Math.cos(a) * 1.15, 1.1, Math.sin(a) * 1.15); p.rotation.z = Math.cos(a) * 0.3; p.rotation.x = Math.sin(a) * 0.3; g.add(p); }
    g.traverse(o => { if (o.isMesh) o.castShadow = true; });
    const light = new THREE.PointLight(ready ? 0x6fb6ff : 0x3d6f9a, ready ? 14 : 4, 12, 2); light.position.y = 2; g.add(light);
    g.position.set(x, 0, z); this.group.add(g); this.solid(x, z, 1.7, 1.7);
    this.animated.push((dt, t) => { rings.forEach((r, k) => { r.rotation.z += dt * (ready ? 1.4 : 0.25) * (k % 2 ? -1 : 1); }); core.rotation.y += dt * 0.4; light.intensity = (ready ? 14 : 4) * (0.8 + Math.sin(t * 3) * 0.2); });
  }
  forge(x, z, turned) {
    const g = this.insula(x, z, 3, 6.5, 1, turned, 'insula_d');
    const front = g.userData.hd;
    const ch = new THREE.Mesh(new THREE.BoxGeometry(1, 3.6, 1), plain(0x4a3d33)); ch.position.set(2.2, 7.6, -1.5); ch.castShadow = true; g.add(ch);
    const glow = new THREE.Mesh(new THREE.PlaneGeometry(7.5, 2.4), new THREE.MeshBasicMaterial({ color: 0xff7a2c, transparent: true, opacity: 0.85 })); glow.position.set(0, 1.4, front - 1.45); g.add(glow);
    const light = new THREE.PointLight(0xff7a3c, 8, 10, 2); light.position.set(0, 1.6, front + 0.6); g.add(light);
    this.animated.push((dt, t) => { light.intensity = 7 + Math.sin(t * 9 + x) * 2; glow.material.opacity = 0.7 + Math.sin(t * 9 + x) * 0.15; });
    const anvil = new THREE.Mesh(new THREE.BoxGeometry(0.7, 0.5, 0.35), new THREE.MeshStandardMaterial({ color: 0x33363a, metalness: 0.8, roughness: 0.4 })); anvil.position.set(1.6, 0.75, front + 1.2); anvil.castShadow = true; g.add(anvil);
    const block = new THREE.Mesh(new THREE.CylinderGeometry(0.3, 0.35, 0.5, 8), plain(0x6b5240)); block.position.set(1.6, 0.25, front + 1.2); g.add(block);
  }
  wheel(x, z) {
    const g = new THREE.Group(), m = std({ map: wood() });
    g.add(new THREE.Mesh(new THREE.TorusGeometry(2.6, 0.2, 6, 20), m));
    for (let k = 0; k < 8; k++) { const s = new THREE.Mesh(new THREE.BoxGeometry(5.2, 0.14, 0.14), m); s.rotation.z = k / 8 * Math.PI; g.add(s); const p = new THREE.Mesh(new THREE.BoxGeometry(0.9, 0.08, 0.9), m); const a = k / 8 * Math.PI * 2; p.position.set(Math.cos(a) * 2.6, Math.sin(a) * 2.6, 0); p.rotation.z = a; g.add(p); }
    g.traverse(o => { if (o.isMesh) o.castShadow = true; });
    g.position.set(x, 2.5, z); g.rotation.y = Math.PI / 2; this.group.add(g);
    this.animated.push(dt => { g.rotation.x += dt * 0.5; });
    const wt = water().clone(); wt.needsUpdate = true; wt.repeat.set(1, 8);
    const race = new THREE.Mesh(new THREE.PlaneGeometry(2.6, 44), new THREE.MeshStandardMaterial({ map: wt, roughness: 0.2 })); race.rotation.x = -Math.PI / 2; race.position.set(x + 0.6, 0.05, z + 10); this.group.add(race);
    this.animated.push((dt, t) => { wt.offset.y = -t * 0.25; });
    this.solid(x, z, 1.2, 3);
  }
}

// ---------------------------------------------------------------------------------------------------------------------
// People: a small jointed figure that can walk

const SKIN = [0xd9a880, 0xc79068, 0xb07a55, 0xe0b590];
const HAIR = [0x2a2018, 0x4a3524, 0x6b5240, 0x9a9488, 0x1c1814];
export function figure(tunic, opts = {}) {
  const rnd = opts.rnd || Math.random, g = new THREE.Group();
  const cloth = new THREE.MeshStandardMaterial({ color: tunic, roughness: 1 });
  const skin = new THREE.MeshStandardMaterial({ color: opts.skin ?? SKIN[Math.floor(rnd() * SKIN.length)], roughness: 0.9 });
  const limb = (len, r, mat) => { const pivot = new THREE.Group(); const m = new THREE.Mesh(new THREE.CylinderGeometry(r, r * 0.85, len, 7), mat); m.position.y = -len / 2; pivot.add(m); return pivot; };
  const torso = new THREE.Mesh(new THREE.CylinderGeometry(0.24, 0.36, 0.95, 10), cloth); torso.position.y = 1.2;
  const skirt = new THREE.Mesh(new THREE.CylinderGeometry(0.36, 0.42, opts.long ? 0.75 : 0.3, 10), cloth); skirt.position.y = opts.long ? 0.42 : 0.62;
  const belt = new THREE.Mesh(new THREE.CylinderGeometry(0.3, 0.31, 0.07, 10), new THREE.MeshStandardMaterial({ color: 0x4a3524 })); belt.position.y = 0.98;
  const head = new THREE.Mesh(new THREE.SphereGeometry(0.2, 12, 10), skin); head.position.y = 1.9;
  const hair = new THREE.Mesh(new THREE.SphereGeometry(0.21, 12, 8, 0, Math.PI * 2, 0, Math.PI * 0.55), new THREE.MeshStandardMaterial({ color: opts.hair ?? HAIR[Math.floor(rnd() * HAIR.length)], roughness: 1 })); hair.position.y = 1.92; hair.rotation.x = -0.25;
  const neck = new THREE.Mesh(new THREE.CylinderGeometry(0.07, 0.08, 0.12, 6), skin); neck.position.y = 1.72;
  const armL = limb(0.72, 0.075, skin), armR = limb(0.72, 0.075, skin); armL.position.set(-0.3, 1.6, 0); armR.position.set(0.3, 1.6, 0);
  const legL = limb(0.72, 0.09, skin), legR = limb(0.72, 0.09, skin); legL.position.set(-0.13, 0.72, 0); legR.position.set(0.13, 0.72, 0);
  g.add(torso, skirt, belt, head, hair, neck, armL, armR, legL, legR);
  if (opts.toga) {                                                // a draped band over one shoulder
    const drape = new THREE.Mesh(new THREE.TorusGeometry(0.33, 0.09, 6, 14, Math.PI * 1.2), new THREE.MeshStandardMaterial({ color: opts.toga, roughness: 1 }));
    drape.position.set(0, 1.3, 0); drape.rotation.set(Math.PI / 2, 0.5, 0.9); g.add(drape);
  }
  if (opts.palla) {                                               // a woman's mantle drawn over the head
    const veil = new THREE.Mesh(new THREE.SphereGeometry(0.25, 12, 8, 0, Math.PI * 2, 0, Math.PI * 0.62), new THREE.MeshStandardMaterial({ color: opts.palla, roughness: 1 })); veil.position.y = 1.92; veil.rotation.x = -0.35;
    const wrap = new THREE.Mesh(new THREE.CylinderGeometry(0.3, 0.4, 0.7, 10, 1, true), new THREE.MeshStandardMaterial({ color: opts.palla, roughness: 1, side: THREE.DoubleSide })); wrap.position.y = 1.38;
    g.add(veil, wrap);
  }
  g.traverse(o => { if (o.isMesh) o.castShadow = true; });
  const s = opts.scale ?? (0.95 + rnd() * 0.12); g.scale.setScalar(s);
  let phase = rnd() * 6;
  // Walks when `moving` is above zero; stands and breathes otherwise.
  g.userData.animate = (dt, moving) => {
    phase += dt * (moving > 0 ? 9 * moving : 1.6);
    const swing = moving > 0 ? Math.sin(phase) * 0.7 * Math.min(1, moving) : 0;
    legL.rotation.x = swing; legR.rotation.x = -swing; armL.rotation.x = -swing * 0.8; armR.rotation.x = swing * 0.8;
    if (moving > 0) { g.position.y = Math.abs(Math.sin(phase)) * 0.05; torso.rotation.z = 0; }
    else { g.position.y = 0; armL.rotation.z = 0.06; armR.rotation.z = -0.06; torso.scale.x = 1 + Math.sin(phase) * 0.012; }
  };
  return g;
}

// A marker that floats over something that needs the player.
export function beacon(color) {
  const m = new THREE.Mesh(new THREE.OctahedronGeometry(0.28, 0), new THREE.MeshBasicMaterial({ color }));
  m.userData.spin = (dt, t) => { m.rotation.y += dt * 2; m.position.y = m.userData.base + Math.sin(t * 3) * 0.15; };
  return m;
}

// The sky: a painted dome.
export function sky(later) {
  const t = canvas(16, 256, (g, w, h) => {
    const grd = g.createLinearGradient(0, 0, 0, h);
    if (later) { grd.addColorStop(0, '#6f8194'); grd.addColorStop(0.5, '#b4b9b8'); grd.addColorStop(0.55, '#c9c2b0'); grd.addColorStop(1, '#c9c2b0'); }
    else { grd.addColorStop(0, '#3f78b8'); grd.addColorStop(0.45, '#a9cbe6'); grd.addColorStop(0.55, '#f1dfbf'); grd.addColorStop(1, '#f1dfbf'); }
    g.fillStyle = grd; g.fillRect(0, 0, w, h);
  });
  t.wrapS = t.wrapT = THREE.ClampToEdgeWrapping;
  return new THREE.Mesh(new THREE.SphereGeometry(300, 24, 16), new THREE.MeshBasicMaterial({ map: t, side: THREE.BackSide, fog: false }));
}

export const textures = { cobble, earth };
