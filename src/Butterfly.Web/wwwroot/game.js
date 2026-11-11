// The walkable-district test (Corey, 2026-11-11). Presentation only: this page draws the models Butterfly.Presentation
// gives and sends back the commands it offers. It never decides, computes or stores anything about the game.
// What is the page's own: where the character stands, which district is drawn, which panel is open.
import * as THREE from './lib/three.module.min.js';

// ---------------------------------------------------------------------------------------------------------------------
// The engine (Butterfly.Core + Butterfly.Presentation, compiled to WebAssembly)

let state = null;
const call = async (method, ...args) => JSON.parse(await DotNet.invokeMethodAsync('Butterfly.Web', method, ...args));

async function boot() {
  await Blazor.start();
  const names = ['tuning.json', ...(await (await fetch('data/content/index.json')).json()).map(n => 'content/' + n)];
  const files = {};
  await Promise.all(names.map(async n => { files[n] = await (await fetch('data/' + n)).text(); }));
  const asked = new URLSearchParams(location.search).get('seed');
  const seed = asked && /^\d+$/.test(asked) ? asked : String(crypto.getRandomValues(new Uint32Array(1))[0] % 100000);
  boot.files = files;
  state = await call('Start', files, seed);
  document.getElementById('boot').remove();
  render();
}

async function act(command) {
  state = await call('Do', command);
  const last = state.last;
  if (last) {
    // A return place's words are read in its own panel; saying them again in the feed would say them twice.
    const inSite = (view.panel || '').startsWith('site:') && last.ok;
    if (last.message && !inSite) say(last.message, '', !last.ok);
    for (const f of last.feed) say(f.text, f.speaker);
  }
  render();
}

// ---------------------------------------------------------------------------------------------------------------------
// Small DOM helpers

const $ = id => document.getElementById(id);
function el(tag, text, cls) { const e = document.createElement(tag); if (text != null) e.textContent = text; if (cls) e.className = cls; return e; }
function button(label, onClick, primary, disabled) {
  const b = el('button', label, primary ? 'primary' : '');
  b.disabled = !!disabled;
  b.addEventListener('click', e => { e.stopPropagation(); onClick(); });
  return b;
}
function actions(items) {
  const box = el('div', null, 'acts');
  for (const a of items) box.append(button(a.label.charAt(0).toUpperCase() + a.label.slice(1), () => act(a.command)));
  return box;
}
function initials(name) { const p = (name || '').split(' '); return (p.length > 1 ? p[0][0] + p[1][0] : (name || '').slice(0, 2)).toUpperCase(); }

function say(text, speaker, refused) {
  const line = el('div', null, 'line' + (refused ? ' no' : ''));
  if (speaker) line.append(el('b', speaker + '  '));
  line.append(document.createTextNode(text));
  const feed = $('feed');
  feed.append(line);
  while (feed.children.length > 6) feed.firstChild.remove();
  feed.scrollTop = feed.scrollHeight;
  setTimeout(() => line.remove(), 45000);
}

// ---------------------------------------------------------------------------------------------------------------------
// What the page is showing (never game state)

const view = { district: 'subura', panel: null, dismissed: '', openingRead: false, beat: 0, site: 0 };

// Where each map place of the game stands in the walkable city. Lodging and the Subura share one district.
const DISTRICT_OF = { lodging: 'subura', subura: 'subura', curia: 'curia', market: 'market', changers: 'changers', forges: 'forges' };
// Where a known person can be found (the page's choice of where to draw them; the game decides whether they are known or away).
const HOME_OF = { Felix: 'market', Cassianus: 'market', Diodoros: 'market', Serenus: 'subura', Gaius: 'forges', Livia: 'forges', Marcus: 'forges',
                  Lucan: 'forges', Aulus: 'forges', Sextus: 'forges', Hermogenes: 'curia' };

const DISTRICTS = {
  subura:   { name: 'The Subura', ground: 0x9a8b74, kind: 'tenements',
              spots: [{ place: 'lodging', x: -13, z: -9 }, { place: 'subura', x: 9, z: 7 }] },
  curia:    { name: 'The Forum and the Curia', ground: 0xb9ad97, kind: 'forum', spots: [{ place: 'curia', x: 0, z: -6 }] },
  market:   { name: 'The market', ground: 0xa3916f, kind: 'market', spots: [{ place: 'market', x: 2, z: 2 }] },
  changers: { name: 'The money changers', ground: 0xa89c86, kind: 'arcade', spots: [{ place: 'changers', x: -3, z: -3 }] },
  forges:   { name: 'The forges', ground: 0x7f7465, kind: 'forges', spots: [{ place: 'forges', x: 4, z: -2 }] },
};
const TRAVEL_ORDER = ['subura', 'curia', 'market', 'changers', 'forges'];

// ---------------------------------------------------------------------------------------------------------------------
// The 3D city (placeholder blocks; cosmetic only)

const canvas = $('view');
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(55, 1, 0.1, 400);
const sun = new THREE.DirectionalLight(0xfff1d6, 2.2);
sun.position.set(30, 50, 20);
scene.add(sun, new THREE.HemisphereLight(0xcfe0ff, 0x6b5a45, 1.1));

let world = new THREE.Group();
scene.add(world);
let solids = [];      // {x, z, hw, hd}: boxes the character can't walk through
let spots = [];       // {kind:'place'|'person'|'gate'|'site', x, z, ...}
const HALF = 30;      // half the size of a district

// A fixed sequence for layout, so a district looks the same every time (cosmetic; never used for the game).
function sequence(seedText) {
  let a = 0; for (const c of seedText) a = (a * 31 + c.charCodeAt(0)) >>> 0;
  return () => { a |= 0; a = a + 0x6D2B79F5 | 0; let t = Math.imul(a ^ a >>> 15, 1 | a); t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; };
}
const mat = c => new THREE.MeshLambertMaterial({ color: c });
function box(w, h, d, color, x, y, z, solid) {
  const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat(color));
  m.position.set(x, y + h / 2, z);
  world.add(m);
  if (solid) solids.push({ x, z, hw: w / 2 + 0.5, hd: d / 2 + 0.5 });
  return m;
}
function cylinder(r, h, color, x, y, z, solid) {
  const m = new THREE.Mesh(new THREE.CylinderGeometry(r, r, h, 14), mat(color));
  m.position.set(x, y + h / 2, z);
  world.add(m);
  if (solid) solids.push({ x, z, hw: r + 0.4, hd: r + 0.4 });
  return m;
}
function insula(x, z, w, d, floors, rnd, later) {
  const walls = later ? [0xb7a58c, 0xa39480, 0xc2b49d] : [0xd9c3a0, 0xcfae86, 0xe0cdb0, 0xc99f7a];
  box(w, floors * 2.6, d, walls[Math.floor(rnd() * walls.length)], x, 0, z, true);
  box(w + 0.6, 0.35, d + 0.6, later ? 0x7d5a48 : 0xa9512f, x, floors * 2.6, z);
  for (let f = 0; f < floors; f++) for (let k = -1; k <= 1; k++) box(0.5, 0.8, 0.08, 0x3a2f28, x + k * (w / 3.4), f * 2.6 + 1.2, z + d / 2 + 0.02);
}
function temple(x, z, color) {
  box(13, 1, 9, 0xd8d0c0, x, 0, z, true);
  box(10.5, 5, 6.5, color, x, 1, z);
  for (let k = -2; k <= 2; k++) cylinder(0.4, 5, 0xeee8da, x + k * 2.4, 1, z + 4);
  box(13, 0.8, 9, 0xd8d0c0, x, 6, z);
  box(11, 1.4, 7.5, 0xb5553a, x, 6.8, z);
}
function stall(x, z, color) {
  box(3, 1, 1.6, 0x8a6a45, x, 0, z, true);
  box(3.4, 0.12, 2.4, color, x, 2.3, z);
  for (const sx of [-1.5, 1.5]) cylinder(0.07, 2.3, 0x5a4632, x + sx, 0, z + 1);
}
function fountain(x, z, clean) {
  cylinder(2.2, 0.7, 0xcfc6b4, x, 0, z, true);
  cylinder(1.9, 0.1, clean ? 0x5fa8c9 : 0x6f7a5a, x, 0.66, z);
  cylinder(0.3, 2.2, 0xcfc6b4, x, 0.7, z);
}
function machine(x, z, ready) {
  const g = new THREE.Group();
  const core = new THREE.Mesh(new THREE.IcosahedronGeometry(1.1, 0), new THREE.MeshStandardMaterial({ color: 0x2b3038, metalness: .7, roughness: .35, emissive: ready ? 0x2d6fb0 : 0x101820, emissiveIntensity: ready ? 1.1 : .4 }));
  core.position.y = 1.7;
  const ring = new THREE.Mesh(new THREE.TorusGeometry(1.7, 0.09, 8, 28), new THREE.MeshStandardMaterial({ color: 0x9a8a5a, metalness: .8, roughness: .3 }));
  ring.position.y = 1.7; ring.rotation.x = Math.PI / 2.4;
  const base = new THREE.Mesh(new THREE.CylinderGeometry(1.5, 1.8, 0.5, 8), mat(0x3b3f47));
  base.position.y = 0.25;
  g.add(core, ring, base);
  g.position.set(x, 0, z);
  g.userData.spin = ring;
  world.add(g);
  solids.push({ x, z, hw: 2, hd: 2 });
  return g;
}
function person(color, scale = 1) {
  const g = new THREE.Group();
  const body = new THREE.Mesh(new THREE.CylinderGeometry(0.34, 0.46, 1.25, 10), mat(color));
  body.position.y = 0.95;
  const head = new THREE.Mesh(new THREE.SphereGeometry(0.27, 12, 10), mat(0xd9a880));
  head.position.y = 1.85;
  const legs = new THREE.Mesh(new THREE.CylinderGeometry(0.22, 0.18, 0.4, 8), mat(0x6b5440));
  legs.position.y = 0.2;
  g.add(body, head, legs);
  g.scale.setScalar(scale);
  return g;
}

let spinning = [], walkers = [];
const hero = person(0x3d6b8e, 1.05);
scene.add(hero);
const pos = { x: 0, z: 12, yaw: 0, cam: 0 };

function buildDistrict(id) {
  scene.remove(world);
  world = new THREE.Group();
  scene.add(world);
  solids = []; spots = []; spinning = []; walkers = [];
  const d = DISTRICTS[id];
  const later = !!(state && state.hud.away);          // after a jump the same streets are older (a tint only)
  const rnd = sequence(id + (later ? ':later' : ''));
  scene.background = new THREE.Color(later ? 0xb9c2c8 : 0xbfd6e8);
  scene.fog = new THREE.Fog(scene.background, 45, 120);
  const ground = new THREE.Mesh(new THREE.PlaneGeometry(HALF * 2 + 60, HALF * 2 + 60), mat(later ? 0x8d8577 : d.ground));
  ground.rotation.x = -Math.PI / 2;
  world.add(ground);
  // A paved street across the district.
  box(HALF * 2, 0.04, 5, 0x857a6b, 0, 0, 12);
  box(5, 0.04, HALF * 2, 0x857a6b, 0, 0, 0);

  // The edge of the district: a wall of buildings.
  for (let k = -5; k <= 5; k++) {
    if (Math.abs(k) < 1) continue;
    insula(k * 6, -HALF + 2, 5.5, 5, 2 + Math.floor(rnd() * 3), rnd, later);
    insula(k * 6, HALF - 2, 5.5, 5, 2 + Math.floor(rnd() * 3), rnd, later);
    insula(-HALF + 2, k * 6, 5, 5.5, 2 + Math.floor(rnd() * 3), rnd, later);
    insula(HALF - 2, k * 6, 5, 5.5, 2 + Math.floor(rnd() * 3), rnd, later);
  }
  if (d.kind === 'tenements') {
    for (const [x, z] of [[-18, -16], [-8, -18], [14, -15], [20, -4], [-20, 2], [16, 18], [-12, 20], [22, 20]]) insula(x, z, 6, 6, 3 + Math.floor(rnd() * 3), rnd, later);
    insula(-13, -14, 7, 6, 4, rnd, later);                      // the lodging house
    box(1.3, 2.2, 0.2, 0x4a3524, -13, 0, -10.9);                // its door
    const m = machine(-9, -8, state && state.hud.machineReady);
    spinning.push(m.userData.spin);
    fountain(9, 9, true);
  } else if (d.kind === 'forum') {
    temple(0, -14, 0xe6dccb); temple(-17, 4, 0xdcd2bf); temple(17, 4, 0xdcd2bf);
    for (let k = -3; k <= 3; k++) cylinder(0.35, 4.5, 0xeee8da, k * 4, 0, 27, true);
    cylinder(0.9, 6, 0xd8d0c0, 10, 0, 12, true);
  } else if (d.kind === 'market') {
    const awn = [0xab5233, 0x3d6b8e, 0x8c6e38, 0x6b8e4e, 0xb98a3a];
    for (let k = 0; k < 14; k++) { const x = -20 + (k % 7) * 6.5, z = k < 7 ? -8 : 6; if (Math.abs(x) > 3) stall(x, z, awn[k % awn.length]); }
    insula(-18, 20, 6, 6, 3, rnd, later); insula(18, 20, 6, 6, 3, rnd, later);
  } else if (d.kind === 'arcade') {
    box(26, 4.2, 5, 0xd6cab5, 0, 0, -12, true);
    for (let k = -5; k <= 5; k++) cylinder(0.3, 4.2, 0xeee8da, k * 2.4, 0, -8.8);
    for (let k = -2; k <= 2; k++) box(1.8, 0.9, 1, 0x7a5c3c, k * 4.2, 0, -6.5, true);   // the changers' tables
    insula(-18, 10, 6, 6, 3, rnd, later); insula(18, 10, 6, 6, 4, rnd, later);
  } else if (d.kind === 'forges') {
    for (const [x, z] of [[-14, -10], [-2, -12], [12, -10], [-16, 8], [14, 10]]) {
      box(7, 3.4, 6, 0x6f5b4a, x, 0, z, true);
      box(1, 4.5, 1, 0x4a3d33, x + 2.4, 3.4, z - 1.5);
      const glow = new THREE.Mesh(new THREE.BoxGeometry(1.6, 1.2, 0.1), new THREE.MeshBasicMaterial({ color: 0xff8a3c }));
      glow.position.set(x, 1.1, z + 3.03);
      world.add(glow);
    }
    const wheel = new THREE.Mesh(new THREE.TorusGeometry(2.6, 0.25, 6, 16), mat(0x5a4632));
    wheel.position.set(22, 2.8, -18); wheel.rotation.y = Math.PI / 2;
    world.add(wheel); spinning.push(wheel);
    box(3, 0.1, 30, 0x5f8fa8, 24.5, 0, -8);                     // the mill race
  }

  // Passers-by: the streets are alive (cosmetic).
  for (let k = 0; k < 10; k++) {
    const w = person([0xb9a27c, 0x8a6f52, 0xa9b0a0, 0x9c7b6b, 0xc7b79a][k % 5], 0.95);
    const along = k % 2 === 0;
    w.userData = { along, lane: along ? 11 + rnd() * 2.5 : -1.5 + rnd() * 3, t: rnd() * 60, speed: (0.9 + rnd() * 0.8) * (rnd() < .5 ? 1 : -1) };
    world.add(w); walkers.push(w);
  }

  // What can be walked up to.
  for (const s of d.spots) addSpot({ kind: 'place', place: s.place, x: s.x, z: s.z });
  for (const p of (state && state.people) || []) {
    if (p.away || HOME_OF[p.id] !== id) continue;
    const n = spots.filter(s => s.kind === 'person').length;
    const base = d.spots[d.spots.length - 1];
    const fig = person(0xab5233);
    const x = base.x + 4 + (n % 3) * 2.4, z = base.z + 3 + Math.floor(n / 3) * 2.4;
    fig.position.set(x, 0, z);
    world.add(fig);
    addSpot({ kind: 'person', id: p.id, x, z });
  }
  for (const [x, z] of [[-HALF + 6, 12], [HALF - 6, 12]]) {
    box(0.6, 5, 0.6, 0xd8d0c0, x, 0, 9.2, true); box(0.6, 5, 0.6, 0xd8d0c0, x, 0, 14.8, true); box(0.9, 0.7, 6.4, 0xd8d0c0, x, 5, 12);
    addSpot({ kind: 'gate', x, z });
  }
  if (state && state.ret) {
    state.ret.sites.forEach((site, k) => {
      const a = (k / state.ret.sites.length) * Math.PI * 2, x = Math.cos(a) * 15, z = Math.sin(a) * 13 + 2;
      cylinder(0.25, 2.6, 0xd8d0c0, x, 0, z, true);
      addSpot({ kind: 'site', number: site.number, x: x + 1.4, z: z + 1.4 });
    });
  }
  view.district = id;
}

function addSpot(s) {
  const ring = new THREE.Mesh(new THREE.RingGeometry(1.2, 1.5, 28), new THREE.MeshBasicMaterial({ color: s.kind === 'person' ? 0xab5233 : s.kind === 'gate' ? 0xf2ebd9 : 0xf3c46b, side: THREE.DoubleSide, transparent: true, opacity: .85 }));
  ring.rotation.x = -Math.PI / 2;
  ring.position.set(s.x, 0.06, s.z);
  world.add(ring);
  s.label = el('div', null, 'label' + (s.kind === 'person' ? ' person' : ''));
  $('labels').append(s.label);
  spots.push(s);
}

function spotText(s) {
  if (s.kind === 'gate') return ['Leave the district', ''];
  if (s.kind === 'person') { const p = state.people.find(p => p.id === s.id); return [p ? p.name : s.id, '']; }
  if (s.kind === 'site') {
    const site = state.ret.sites.find(x => x.number === s.number);
    return [site.place, ['not yet visited', 'seen', 'looked closer'][site.state]];
  }
  const place = state.places.find(p => p.id === s.place);
  const n = (place.actions || []).reduce((sum, g) => sum + g.items.length, 0);
  return [place.name, n > 0 ? n + (n === 1 ? ' thing to do' : ' things to do') : ''];
}

// ---------------------------------------------------------------------------------------------------------------------
// Moving

const keys = {};
addEventListener('keydown', e => { keys[e.key.toLowerCase()] = true; if ((e.key === 'e' || e.key === 'Enter') && near && !blocked()) { e.preventDefault(); interact(); } if (e.key === 'Escape') closePanel(); });
addEventListener('keyup', e => { keys[e.key.toLowerCase()] = false; });
let drag = null, stick = { x: 0, y: 0, id: null };
canvas.addEventListener('pointerdown', e => {
  if (e.pointerType === 'touch' && e.clientX < innerWidth * 0.45) {
    stick.id = e.pointerId; stick.ox = e.clientX; stick.oy = e.clientY;
    const s = $('stick'); s.hidden = false; s.style.left = (e.clientX - 55) + 'px'; s.style.top = (e.clientY - 55) + 'px'; s.style.bottom = 'auto';
  } else drag = { id: e.pointerId, x: e.clientX };
});
addEventListener('pointermove', e => {
  if (e.pointerId === stick.id) {
    const dx = e.clientX - stick.ox, dy = e.clientY - stick.oy, m = Math.max(1, Math.hypot(dx, dy) / 45);
    stick.x = dx / 45 / m; stick.y = dy / 45 / m;
    $('knob').style.left = (35 + stick.x * 35) + 'px'; $('knob').style.top = (35 + stick.y * 35) + 'px';
  } else if (drag && e.pointerId === drag.id) { pos.cam -= (e.clientX - drag.x) * 0.006; drag.x = e.clientX; }
});
addEventListener('pointerup', e => {
  if (e.pointerId === stick.id) { stick = { x: 0, y: 0, id: null }; $('stick').hidden = true; }
  if (drag && e.pointerId === drag.id) drag = null;
});

let near = null;
const blocked = () => !$('card').hidden || !$('full').hidden;

function step(dt) {
  let fx = 0, fz = 0;
  if (!blocked()) {
    if (keys['w'] || keys['arrowup']) fz -= 1;
    if (keys['s'] || keys['arrowdown']) fz += 1;
    if (keys['a']) fx -= 1;
    if (keys['d']) fx += 1;
    if (keys['arrowleft'] || keys['q']) pos.cam += dt * 1.8;
    if (keys['arrowright']) pos.cam -= dt * 1.8;
    fx += stick.x; fz += stick.y;
  }
  const len = Math.hypot(fx, fz);
  if (len > 0.05) {
    const s = Math.sin(pos.cam), c = Math.cos(pos.cam);
    const dx = (fx * c + fz * s) / Math.max(1, len), dz = (-fx * s + fz * c) / Math.max(1, len);
    const speed = 7.5 * dt;
    let nx = pos.x + dx * speed, nz = pos.z + dz * speed;
    nx = Math.max(-HALF + 5, Math.min(HALF - 5, nx)); nz = Math.max(-HALF + 5, Math.min(HALF - 5, nz));
    const hit = (x, z) => solids.some(b => Math.abs(x - b.x) < b.hw && Math.abs(z - b.z) < b.hd);
    if (!hit(nx, pos.z)) pos.x = nx;
    if (!hit(pos.x, nz)) pos.z = nz;
    pos.yaw = Math.atan2(dx, dz);
    hero.position.y = Math.abs(Math.sin(performance.now() / 110)) * 0.08;
  } else hero.position.y = 0;
  hero.position.x = pos.x; hero.position.z = pos.z; hero.rotation.y = pos.yaw;
  camera.position.set(pos.x + Math.sin(pos.cam) * 11, 7.5, pos.z + Math.cos(pos.cam) * 11);
  camera.lookAt(pos.x, 1.6, pos.z);

  for (const w of walkers) {
    const u = w.userData; u.t += dt * u.speed;
    const p = ((u.t % 50) + 50) % 50 - 25;
    if (u.along) w.position.set(p, 0, u.lane); else w.position.set(u.lane, 0, p);
    w.rotation.y = u.along ? (u.speed > 0 ? Math.PI / 2 : -Math.PI / 2) : (u.speed > 0 ? 0 : Math.PI);
  }
  for (const s of spinning) s.rotation.z += dt * 0.6;

  // Labels over what can be walked up to, and the nearest one in reach.
  let best = null, bestD = 3.2;
  const v = new THREE.Vector3();
  for (const s of spots) {
    const d = Math.hypot(s.x - pos.x, s.z - pos.z);
    if (d < bestD) { best = s; bestD = d; }
    v.set(s.x, s.kind === 'person' ? 2.6 : 3.4, s.z).project(camera);
    const show = v.z < 1 && d < 34 && !blocked();
    s.label.style.display = show ? '' : 'none';
    if (show) {
      const [name, note] = spotText(s);
      s.label.style.left = ((v.x + 1) / 2 * innerWidth) + 'px'; s.label.style.top = ((1 - v.y) / 2 * innerHeight) + 'px';
      s.label.textContent = name;
      if (note) s.label.append(el('small', note));
    }
  }
  if (best !== near) {
    near = best;
    const b = $('interact');
    b.hidden = !near || blocked();
    if (near) b.textContent = near.kind === 'gate' ? 'Travel' : near.kind === 'person' ? 'Talk' : near.kind === 'site' ? 'Look' : 'Go in';
  }
  if (blocked()) $('interact').hidden = true;
}
$('interact').addEventListener('click', () => interact());

async function interact() {
  if (!near) return;
  if (near.kind === 'gate') return openPanel('travel');
  if (near.kind === 'person') return openPanel('person:' + near.id);
  if (near.kind === 'site') return openPanel('site:' + near.number);
  openPanel('place:' + near.place);
}

function travel(id) {
  $('labels').replaceChildren();
  buildDistrict(id);
  pos.x = 0; pos.z = 14; pos.cam = 0;
  closePanel();
  say(DISTRICTS[id].name + '.');
}

// ---------------------------------------------------------------------------------------------------------------------
// Screens (drawn from the models)

function openPanel(what) { view.panel = what; render(); }
function closePanel() { view.panel = null; $('panel').hidden = true; }
$('panelClose').addEventListener('click', closePanel);

async function render() {
  const s = state, phase = s.phase;
  const full = $('full'), fullBody = $('fullBody'), card = $('card'), cardBody = $('cardBody');
  full.hidden = true; card.hidden = true;
  const era = phase === 'Rome' || (phase === 'Opening' && view.openingRead);

  // The city follows the game: a jump or a new acquaintance redraws the district.
  const key = view.district + '|' + s.hud.away + '|' + s.hud.machineReady + '|' + (s.people || []).filter(p => !p.away).map(p => p.id).join(',') + '|' + (s.ret ? s.ret.sites.length : 0);
  if (render.key !== key) { render.key = key; $('labels').replaceChildren(); buildDistrict(s.hud.away ? 'subura' : view.district); }

  // HUD
  const hud = $('hud');
  hud.hidden = !(era || phase === 'Return' || phase === 'AfterReturn');
  const facts = $('facts'), nav = $('nav');
  facts.replaceChildren(el('b', s.hud.date));
  nav.replaceChildren();
  $('health').replaceChildren();
  if (era) {
    facts.append(el('span', s.hud.life, 'dim'));
    facts.append(el('span', 'Attention ' + '●'.repeat(Math.max(0, s.hud.attentionFree)) + '○'.repeat(Math.max(0, s.hud.attentionTotal - s.hud.attentionFree)) + '  ' + s.hud.attentionFree + ' free / ' + s.hud.attentionTotal));
    if (s.hud.reserved.length) facts.append(el('span', 'reserved: ' + s.hud.reserved.join(', '), 'dim'));
    facts.append(el('span', s.hud.money + ' · ' + s.hud.aurei));
    facts.append(el('span', 'Machine: ' + s.hud.machine, s.hud.machineReady ? 'ready' : ''));
    for (const line of s.hud.health) $('health').append(el('div', line));
    for (const [label, what] of [['People', 'people'], ['Work', 'work'], ['Machine', 'machine'], ['Journal', 'journal'], ['Everything', 'everything'], ['Travel', 'travel']])
      nav.append(button(label, () => openPanel(what), view.panel === what));
    if (s.decision && s.decision.key === view.dismissed) nav.append(button('Waiting: ' + s.decision.title, () => { view.dismissed = ''; render(); }, true));
    if (s.hud.machineReady) nav.append(button('Prepare to leave…', async () => { state = await call('OpenDeparture'); render(); }));
    nav.append(button('End Month', () => act('end'), true));
    nav.append(button('Fast-forward', () => act('wait')));
  } else if (phase === 'Return' || phase === 'AfterReturn') {
    facts.append(el('span', s.ret.yearsAway + ' years after you left'));
    if (phase === 'Return') facts.append(el('span', 'Places seen: ' + s.ret.seen + (s.ret.visitsRequired ? ' (at least ' + s.ret.visitsRequired + ' before you go on)' : ''), 'dim'));
    nav.append(button('Your journal', () => openPanel('journal')));
    if (phase === 'Return' && s.ret.canFinish) nav.append(button('Finish looking', () => act('done'), true));
  }
  document.documentElement.style.setProperty('--hudh', (hud.hidden ? 0 : hud.offsetHeight) + 'px');

  // Whole-screen moments
  if (phase === 'Ended') {
    full.hidden = false;
    fullBody.replaceChildren(el('h1', s.ending.title), ...s.ending.lines.map(l => el('p', l)), el('p', 'Seed ' + s.seed, 'dim'));
    fullBody.append(button('Start again with this seed', () => restart(s.seed), true));
    return closePanel();
  }
  if (phase === 'Opening' && !view.openingRead) {
    full.hidden = false;
    fullBody.replaceChildren(el('h1', s.opening.title), ...s.opening.paragraphs.map(p => el('p', p)), el('p', 'Seed ' + s.seed, 'dim'));
    fullBody.append(button('Continue', () => { view.openingRead = true; render(); }, true));
    return;
  }
  if (phase === 'Departure') {
    const d = s.departure;
    full.hidden = false;
    fullBody.replaceChildren(el('h1', 'Before you leave'), el('p', d.range), el('p', d.inHand), el('h3', 'What you leave'), ...d.briefing.map(l => el('p', l)));
    if (d.prepare.length) fullBody.append(el('h3', 'Your gold'), actions(d.prepare));
    const row = el('div', null, 'row');
    row.append(button('Stay in Rome', async () => { state = await call('CloseDeparture'); render(); }));
    row.append(button('Leave now', () => { view.beat = 0; act('jump'); }, true, !d.canLeave));
    fullBody.append(row);
    return closePanel();
  }
  if (phase === 'Arrival') {
    const a = s.arrival;
    full.hidden = false;
    fullBody.replaceChildren(el('h1', 'AD ' + a.arrivalYear), el('p', 'The machine carries you ' + a.jumpYears + ' years.', 'dim'));
    a.beats.slice(0, view.beat + 1).forEach(b => fullBody.append(el('p', b.text)));
    fullBody.append(view.beat < a.beats.length - 1
      ? button('Go on', () => { view.beat++; render(); }, true)
      : button('Walk into the city', async () => { state = await call('FinishArrival'); pos.x = 0; pos.z = 14; render(); }, true));
    return closePanel();
  }
  if (phase === 'AfterReturn' && view.panel !== 'journal') {
    full.hidden = false;
    fullBody.replaceChildren(el('h1', 'AD ' + s.ret.arrivalYear), el('p', 'You have seen what there was to see. This is as far as the test goes.'), el('p', s.learnMore, 'dim'), el('p', 'Seed ' + s.seed, 'dim'));
    fullBody.append(button('Start again', () => restart(null), true));
    return closePanel();
  }

  // The narrative view: one choice at a time; it can be set aside and waits in the HUD.
  if (era && s.decision && s.decision.key !== view.dismissed) {
    const c = s.decision;
    card.hidden = false;
    cardBody.replaceChildren();
    if (c.speaker) { cardBody.append(el('div', initials(c.speaker), 'portrait')); cardBody.append(el('div', c.speaker, 'muted')); }
    cardBody.append(el('h2', c.title), el('p', c.text), actions(c.choices));
    const row = el('div', null, 'row');
    row.append(button('Not now', () => { view.dismissed = c.key; render(); }));
    cardBody.append(row);
  }

  drawPanel();
}

async function drawPanel() {
  const s = state, panel = $('panel'), body = $('panelBody'), what = view.panel;
  if (!what) { panel.hidden = true; return; }
  panel.hidden = false;
  body.replaceChildren();
  const groups = gs => { for (const g of gs) if (g.items.length) body.append(el('h3', g.title), actions(g.items)); };
  if (what === 'travel') {
    body.append(el('h2', 'Where to?'));
    const list = el('div', null, 'acts');
    for (const id of TRAVEL_ORDER) list.append(button(DISTRICTS[id].name + (id === view.district ? '  (you are here)' : ''), () => travel(id), false, id === view.district || s.hud.away));
    body.append(list);
  } else if (what.startsWith('place:')) {
    const place = s.places.find(p => p.id === what.slice(6));
    body.append(el('h2', place.name), el('p', place.caption, 'muted'));
    const look = await call2('Look', place.id);
    if (look) body.append(el('p', look));
    if (place.id === 'lodging') { if (s.readyLine) body.append(el('p', s.readyLine)); for (const n of s.news || []) body.append(el('p', n, 'muted')); }
    groups(place.actions || []);
    if (!(place.actions || []).some(g => g.items.length)) body.append(el('p', 'Nothing to do here now.', 'muted'));
    if (place.id === 'lodging') { body.append(el('h3', 'The month')); body.append(actions([{ label: 'End Month', command: 'end' }])); }
  } else if (what.startsWith('person:')) {
    const p = s.people.find(p => p.id === what.slice(7));
    if (!p) return closePanel();
    body.append(el('div', initials(p.name), 'portrait'), el('h2', p.name), el('p', p.role, 'muted'), el('p', p.status));
    for (const [k, v] of [['Household', p.household], ['Cares about', p.caresAbout], ['Wants', p.wants]]) if (v) { body.append(el('h3', k), el('p', v)); }
  } else if (what === 'people') {
    body.append(el('h2', 'People'));
    if (!s.people.length) body.append(el('p', 'You know no one in Rome yet.', 'muted'));
    for (const p of s.people) {
      const it = el('div', null, 'item');
      it.append(el('b', p.name + (p.away ? '  (away)' : '')), el('span', p.role), el('span', p.status));
      if (!p.away && HOME_OF[p.id]) it.append(el('span', 'Usually found at: ' + DISTRICTS[HOME_OF[p.id]].name));
      body.append(it);
    }
  } else if (what === 'work') {
    body.append(el('h2', 'Work'));
    for (const [title, items] of [['For people', s.work.commissions], ['The larger questions', s.work.challenges], ['Under way', s.work.underWay]]) {
      if (!items.length) continue;
      body.append(el('h3', title));
      for (const i of items) { const it = el('div', null, 'item'); it.append(el('b', i.title), el('span', i.who), el('span', i.state)); body.append(it); }
    }
    groups(s.work.actions);
  } else if (what === 'machine') {
    const m = s.machine;
    body.append(el('h2', 'The machine'));
    for (const l of m.panel) body.append(el('p', l));
    for (const sys of m.systems) {
      body.append(el('b', sys.id.charAt(0).toUpperCase() + sys.id.slice(1) + '  ' + sys.done + '/' + sys.total));
      const bar = el('div', null, 'bar'); const fill = el('i'); fill.style.width = (sys.total ? sys.done / sys.total * 100 : 0) + '%'; bar.append(fill); body.append(bar);
      if (sys.underWay) body.append(el('p', 'Under way: ' + sys.underWay, 'muted')); else if (sys.next) body.append(el('p', 'Next: ' + sys.next, 'muted'));
    }
    if (m.assessed) body.append(el('p', 'Gold restored: ' + m.goldRestored + ' of ' + m.goldNeeded + ' aurei', 'muted'));
    if (m.actions.length) body.append(el('h3', 'At the machine'), actions(m.actions));
  } else if (what === 'journal') {
    const j = s.journal;
    body.append(el('h2', 'Your journal'));
    if (!j.entries.length) body.append(el('p', 'Nothing written yet.', 'muted'));
    for (const e of j.entries) body.append(el('p', e));
    if (j.recent.length) { body.append(el('h3', 'Lately')); for (const f of j.recent.slice(-14)) body.append(el('p', (f.speaker ? f.speaker + ': ' : '') + f.text, 'muted')); }
    if (j.ledger.length) { body.append(el('h3', 'Money')); for (const l of j.ledger) body.append(el('p', l, 'muted')); }
    body.append(el('p', 'Seed ' + s.seed, 'muted'));
  } else if (what === 'everything') {
    body.append(el('h2', 'Everything you could do now'));
    groups((s.everything || []).filter(g => !['look', 'month', 'leave', 'decide'].includes(g.key)));
  } else if (what.startsWith('site:')) {
    const site = s.ret && s.ret.sites.find(x => x.number === +what.slice(5));
    if (!site) return closePanel();
    body.append(el('h2', site.place), el('p', ['Not yet visited', 'Seen', 'Looked closer'][site.state], 'muted'));
    if (site.state === 0) body.append(actions([{ label: 'Go and look', command: site.visitCommand }]));
    else {
      body.append(el('p', site.recognition), el('p', site.contradiction));
      if (site.state === 1 && site.lead) body.append(actions([{ label: 'Look closer: ' + site.lead, command: site.lookCloserCommand }]));
      if (site.finding) body.append(el('h3', 'What you found'), el('p', site.finding));
    }
  }
}
// A read-only question to the engine that returns plain text.
const call2 = (method, ...args) => DotNet.invokeMethodAsync('Butterfly.Web', method, ...args);

async function restart(seed) {
  Object.assign(view, { district: 'subura', panel: null, dismissed: '', openingRead: false, beat: 0 });
  $('feed').replaceChildren();
  state = await call('Start', boot.files, seed || String(crypto.getRandomValues(new Uint32Array(1))[0] % 100000));
  render.key = null;
  render();
}

// ---------------------------------------------------------------------------------------------------------------------
// The frame

function resize() { renderer.setSize(innerWidth, innerHeight, false); camera.aspect = innerWidth / innerHeight; camera.updateProjectionMatrix(); }
addEventListener('resize', resize);
resize();
let before = performance.now();
function frame(now) {
  const dt = Math.min(0.05, (now - before) / 1000);
  before = now;
  if (state) step(dt);
  renderer.render(scene, camera);
  requestAnimationFrame(frame);
}
requestAnimationFrame(frame);
window.butterfly = { get state() { return state; }, act, view, pos, travel, openPanel };   // for the browser test only
boot().catch(e => { $('boot').textContent = 'Could not start: ' + e; console.error(e); });
