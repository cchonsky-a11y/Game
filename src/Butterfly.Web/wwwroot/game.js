// The walkable city (Corey, 2026-11-11). Presentation only: this page draws the models Butterfly.Presentation gives and
// sends back the commands it offers. It never decides, computes or stores anything about the game.
// What is the page's own: where the character stands, which district is drawn, which panel is open.
import * as THREE from './lib/three.module.min.js';
import { City, figure, beacon, sky, textures, sequence, loadKit } from './city.js';

// ---------------------------------------------------------------------------------------------------------------------
// The engine (Butterfly.Core + Butterfly.Presentation, compiled to WebAssembly)

let state = null;
const call = async (method, ...args) => JSON.parse(await DotNet.invokeMethodAsync('Butterfly.Web', method, ...args));
const ask = (method, ...args) => DotNet.invokeMethodAsync('Butterfly.Web', method, ...args);   // a read-only question, plain text

async function boot() {
  // The runtime's files are served from framework/ (the build renames _framework: some hosts reserve names starting with "_").
  await Blazor.start({ loadBootResource: (type, name) => new URL('framework/' + name, document.baseURI).href });
  const names = ['tuning.json', ...(await (await fetch('data/content/index.json')).json()).map(n => 'content/' + n)];
  const files = {};
  await Promise.all(names.map(async n => { files[n] = await (await fetch('data/' + n)).text(); }));
  const asked = new URLSearchParams(location.search).get('seed');
  const seed = asked && /^\d+$/.test(asked) ? asked : String(crypto.getRandomValues(new Uint32Array(1))[0] % 100000);
  boot.files = files;
  await loadKit('kit.glb');
  state = await call('Start', files, seed);
  document.getElementById('boot').remove();
  render();
}

async function act(command) {
  const dateBefore = state.hud.date;
  state = await call('Do', command);
  const last = state.last;
  if (last) {
    // A return place's words are read in its own panel; saying them again in the feed would say them twice.
    const inSite = (view.panel || '').startsWith('site:') && last.ok;
    if (last.message && !inSite) say(last.message, '', !last.ok);
    for (const f of last.feed) say(f.text, f.speaker);
  }
  if (state.hud.date !== dateBefore && (state.phase === 'Rome' || state.phase === 'Opening')) turnThePage(state.hud.date);
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
const cap = s => s.charAt(0).toUpperCase() + s.slice(1);
function actions(items) {
  const box = el('div', null, 'acts');
  for (const a of items) box.append(button(cap(a.label), () => act(a.command)));
  return box;
}
function initials(name) { const p = (name || '').split(' '); return (p.length > 1 ? p[0][0] + p[1][0] : (name || '').slice(0, 2)).toUpperCase(); }

function say(text, speaker, refused) {
  const line = el('div', null, 'line' + (refused ? ' no' : ''));
  if (speaker) line.append(el('b', speaker + '  '));
  line.append(document.createTextNode(text));
  const feed = $('feed');
  feed.append(line);
  while (feed.children.length > 5) feed.firstChild.remove();
  feed.scrollTop = feed.scrollHeight;
  setTimeout(() => line.remove(), 30000);
}
function turnThePage(date) {
  const f = $('fade');
  f.textContent = date;
  f.classList.remove('show'); void f.offsetWidth; f.classList.add('show');
}

// ---------------------------------------------------------------------------------------------------------------------
// What the page is showing (never game state)

const view = { district: 'subura', panel: null, dismissed: '', openingRead: false, beat: 0 };

// Where a known person can be found, and how they dress (the page's choice of how to draw them; the game decides whether
// they are known or away).
const PEOPLE = {
  Felix: { home: 'market', tunic: 0x8a5a3a }, Cassianus: { home: 'market', tunic: 0xe8e0d0, toga: 0x8a2f3a, long: true }, Diodoros: { home: 'market', tunic: 0x7a6a4a },
  Serenus: { home: 'subura', tunic: 0xd8d0b8, toga: 0x5a7a8a, long: true }, Gaius: { home: 'forges', tunic: 0x6a4a3a }, Livia: { home: 'forges', tunic: 0x9a5a6a, long: true, palla: 0x6a4a5a },
  Marcus: { home: 'forges', tunic: 0x5a6a4a }, Lucan: { home: 'forges', tunic: 0x5a5048, hair: 0xb8b2a6 }, Aulus: { home: 'forges', tunic: 0x7a5a4a },
  Sextus: { home: 'forges', tunic: 0xe0d8c8, toga: 0x6a5a8a, long: true }, Hermogenes: { home: 'curia', tunic: 0xece6d8, toga: 0x9a7a3a, long: true },
};
const DISTRICTS = {
  subura:   { name: 'The Subura', spots: [{ place: 'lodging', x: -11.5, z: -8.5 }, { place: 'subura', x: 9, z: 4.6 }] },
  curia:    { name: 'The Forum and the Curia', spots: [{ place: 'curia', x: 0, z: -11 }] },
  market:   { name: 'The market', spots: [{ place: 'market', x: 0, z: 5.6 }] },
  changers: { name: 'The money changers', spots: [{ place: 'changers', x: 0, z: -4.5 }] },
  forges:   { name: 'The forges', spots: [{ place: 'forges', x: 3, z: 1 }] },
};
const TRAVEL_ORDER = ['subura', 'curia', 'market', 'changers', 'forges'];
const HALF = 30;

// ---------------------------------------------------------------------------------------------------------------------
// The scene

const canvas = $('view');
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, powerPreference: 'high-performance' });
renderer.setPixelRatio(Math.min(devicePixelRatio, 1.75));
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.toneMappingExposure = 1.05;
const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(58, 1, 0.3, 500);
const sun = new THREE.DirectionalLight(0xffe2b4, 3.1);
sun.castShadow = true;
sun.shadow.mapSize.set(2048, 2048);
Object.assign(sun.shadow.camera, { left: -34, right: 34, top: 34, bottom: -34, near: 1, far: 140 });
sun.shadow.bias = -0.0006; sun.shadow.normalBias = 0.04;
const fill = new THREE.HemisphereLight(0xbcd4f0, 0x8a6f52, 1.25);
scene.add(sun, sun.target, fill);
let dome = null;
const pmrem = new THREE.PMREMGenerator(renderer);

let city = null, spots = [], walkers = [], beacons = [];
const hero = figure(0x3d6b8e, { scale: 1.06, rnd: sequence('hero'), hair: 0x2a2018 });
scene.add(hero);
const pos = { x: 0, z: 14, yaw: Math.PI, cam: 0, dist: 9.5, target: null, then: null, speed: 0 };
const camNow = new THREE.Vector3(0, 8, 26), lookNow = new THREE.Vector3(0, 1.6, 14);

function buildDistrict(id) {
  if (city) scene.remove(city.group);
  if (dome) scene.remove(dome);
  for (const s of spots) s.label.remove();
  spots = []; walkers = []; beacons = [];
  const later = !!(state && state.hud.away);          // after a jump the same streets are older
  const c = city = new City(id, later), rnd = c.rnd;
  dome = sky(later); scene.add(dome);
  { const env = new THREE.Scene(); env.add(sky(later)); if (scene.environment) scene.environment.dispose(); scene.environment = pmrem.fromScene(env, 0, 1, 400).texture; scene.environmentIntensity = 0.45; }
  scene.fog = new THREE.Fog(later ? 0xc4c0b2 : 0xeeddc0, 60, 230);
  sun.color.set(later ? 0xdfe0e6 : 0xffe2b4); sun.intensity = later ? 2.3 : 3.1;
  c.ground(textures.earth(later), HALF * 2 + 90, 26);
  c.paving(HALF * 2, 6, 0, 12);
  c.paving(6, HALF * 2, 0, 0);

  // The edge of the district: a wall of tenements, with the two gates left open.
  const street = (mid, w) => Math.abs(mid) < w / 2 + 3.4, gate = (mid, w) => Math.abs(mid - 12) < w / 2 + 4.5;
  c.insulaRow(-HALF - 1.5, -HALF - 5, HALF + 5, true, street); c.insulaRow(HALF + 1.5, -HALF - 5, HALF + 5, true, street);
  c.insulaRow(-HALF - 1.5, -HALF + 4, HALF - 4, false, gate); c.insulaRow(HALF + 1.5, -HALF + 4, HALF - 4, false, gate);
  c.arch(-HALF + 7, 12, Math.PI / 2); c.arch(HALF - 7, 12, Math.PI / 2);
  c.skyline();

  if (id === 'subura') {
    c.insula(-13, -15.5, 3, 7, 4, false, 'insula_a');                  // the lodging house
    c.shed(-7.2, -8.6); c.machine(-7.2, -8.6, state && state.hud.machineReady);
    for (const [x, z, t, n] of [[12, -16.5, false, 'insula_b'], [20, -2, true, 'insula_e'], [-21, 3, true, 'insula_b'], [-13, 22, false, 'insula_d'], [14, 22, false, 'insula_f']]) c.insula(x, z, 0, 0, 0, t, n);
    c.fountain(9, 8.5, true);
    c.cart(-4, 16.5, 0.3); c.brazier(-10, -10.2); c.brazier(4.2, 9);
    for (const [x, z] of [[-15.5, -10.4], [-15, -9.8], [4.5, -13], [5.2, -13.4], [16, 14]]) c.amphora(x, z, 0.9 + rnd() * 0.4);
    for (const [x, z] of [[-17, 9], [-16, 9.6], [15, -7]]) c.crate(x, z);
    c.umbrellaPine(16.5, 8.5); c.cypress(-17.5, 15.5);
    c.shrine(4.6, 16.2); c.thermopolium(-9.5, 17.6, Math.PI);
    c.laundry(-17.5, -6, -9.5, -11.5, 6.5); c.laundry(5.5, -14, 16, -10.5, 7.5); c.laundry(-17, 6, -8.5, 18.5, 7);
  } else if (id === 'curia') {
    c.paving(46, 40, 0, -2);
    c.curia(0, -19.5); c.inscription('SENATVS POPVLVSQVE ROMANVS', 9, 1.1, 0, 11.6, -14.92);
    c.temple(-18, -1, 6, Math.PI / 2); c.temple(18, -1, 6, -Math.PI / 2); c.rostra(0, 22.5); c.spiralColumn(-9, 5);
    for (let k = -3; k <= 3; k++) if (k) { c.column(k * 4.4, 24.5, 5.5); }
    c.column(9, 5, 6, 0.6); const statue = figure(0x8a7a4a, { rnd, scale: 1.5, toga: 0x8a7a4a, long: true, hair: 0x8a7a4a, skin: 0x8a7a4a }); statue.position.set(9, 6, 5); c.group.add(statue);   // a bronze
    c.umbrellaPine(-15, 17); c.umbrellaPine(15, 17); c.brazier(-3, -12.6); c.brazier(3, -12.6);
  } else if (id === 'market') {
    c.paving(48, 30, 0, 0);
    const awn = ['#a8482c', '#3f6077', '#7a6a3f', '#5a7a4a', '#8a4a6a'];
    for (let k = 0; k < 7; k++) { const x = -21 + k * 7; if (Math.abs(x) > 3.5) { c.stall(x, -8, awn[k % 5], 0); c.stall(x, 7, awn[(k + 2) % 5], Math.PI); } }
    c.insula(-17, -20, 0, 0, 0, false, 'insula_d'); c.insula(16, -20, 0, 0, 0, false, 'insula_b'); c.insula(-17, 22, 0, 0, 0, false, 'insula_b'); c.insula(17, 22, 0, 0, 0, false, 'insula_d');
    c.cart(9, 16.5, -0.4); c.cart(-12, -2, 1.2);
    for (let k = 0; k < 9; k++) c.amphora(-6 + rnd() * 2.4, -13 + rnd() * 2, 0.9 + rnd() * 0.5);
    for (let k = 0; k < 6; k++) c.crate(5 + rnd() * 3, -14 + rnd() * 2.5);
    c.umbrellaPine(22, 2); c.tholos(0, -0.5);
  } else if (id === 'changers') {
    c.paving(40, 24, 0, -4);
    c.insula(-6.5, -17, 0, 0, 0, false, 'insula_f'); c.insula(6.4, -17, 0, 0, 0, false, 'insula_f');
    for (let k = -5; k <= 5; k++) c.column(k * 2.6, -11.2, 4.6, 0.3);
    c.box(31, 0.5, 2.6, stoneMat(), 0, 4.6, -11.9);
    for (let k = -2; k <= 2; k++) { c.box(1.9, 0.9, 1, woodMat(), k * 4.4, 0, -7.5, true); c.box(0.5, 0.25, 0.4, goldMat(), k * 4.4 + 0.3, 0.9, -7.5); }
    c.insula(-21, 0, 0, 0, 0, true, 'insula_b'); c.insula(21, 0, 0, 0, 0, true, 'insula_b'); c.insula(-11, 22, 0, 0, 0, false, 'insula_d'); c.insula(12, 22, 0, 0, 0, false, 'insula_a');
    c.brazier(-12, -9.5); c.brazier(12, -9.5); c.cypress(-22, -8); c.cypress(22, -8);
  } else if (id === 'forges') {
    c.forge(-15, -15, false); c.forge(-4.5, -15, false); c.forge(12, -15, false); c.forge(-21, 2, true); c.forge(13, 22, false);
    c.wheel(23, -16);
    c.cart(-6, 16.5, 0.2);
    for (let k = 0; k < 7; k++) c.crate(-9 + rnd() * 3, 4 + rnd() * 3);
    for (const [x, z] of [[7, -6], [7.7, -6.5], [-10, -6]]) c.amphora(x, z, 1.1);
    c.insula(-12, 22, 0, 0, 0, false, 'insula_b');
  }

  // Passers-by: the streets are alive (cosmetic).
  const cloth = [0xb9a27c, 0x8a6f52, 0xa9b0a0, 0x9c7b6b, 0xc7b79a, 0x7a8a6a, 0x8a5a4a];
  for (let k = 0; k < 12; k++) {
    const kind = rnd();                                              // a tunic, a citizen's toga, a woman's mantle, a child
    const w = figure(cloth[k % cloth.length], kind < 0.4 ? { rnd } : kind < 0.62 ? { rnd, long: true, toga: 0xe8e0d0 } : kind < 0.9 ? { rnd, long: true, palla: [0x8a5a6a, 0x5a7a8a, 0xa8824a, 0x6a7a5a][k % 4] } : { rnd, scale: 0.62 });
    const along = k % 2 === 0;
    w.userData.walk = { along, lane: along ? 10.2 + rnd() * 3.6 : -1.8 + rnd() * 3.6, t: rnd() * 60, speed: (0.9 + rnd() * 0.9) * (rnd() < .5 ? 1 : -1) };
    c.group.add(w); walkers.push(w);
  }

  // What can be walked up to.
  const d = DISTRICTS[id];
  for (const s of d.spots) addSpot({ kind: 'place', place: s.place, x: s.x, z: s.z });
  let n = 0;
  for (const p of (state && state.people) || []) {
    const look = PEOPLE[p.id];
    if (p.away || !look || look.home !== id) continue;
    const base = d.spots[d.spots.length - 1];
    const a = -0.6 + n * 0.7, x = base.x + Math.cos(a) * 4.6, z = base.z + 3.4 + Math.sin(a) * 2.6;
    const fig = figure(look.tunic, { rnd: sequence(p.id), toga: look.toga, long: look.long, hair: look.hair, palla: look.palla });
    fig.position.set(x, 0, z);
    c.group.add(fig);
    addSpot({ kind: 'person', id: p.id, x, z, fig });
    n++;
  }
  addSpot({ kind: 'gate', x: -HALF + 9.5, z: 12 }); addSpot({ kind: 'gate', x: HALF - 9.5, z: 12 });
  if (state && state.ret) {
    state.ret.sites.forEach((site, k) => {
      const a = (k / state.ret.sites.length) * Math.PI * 2 + 0.4, x = Math.cos(a) * 14, z = Math.sin(a) * 11 + 3;
      c.column(x, z, 2.4, 0.32);
      addSpot({ kind: 'site', number: site.number, x: x + 1.5, z: z + 1.5 });
    });
  }
  scene.add(c.group);
  view.district = id;
}
// Small materials used once above.
const woodMat = () => new THREE.MeshStandardMaterial({ color: 0x7a5c3c, roughness: 1 });
const goldMat = () => new THREE.MeshStandardMaterial({ color: 0xd8b24a, metalness: 0.9, roughness: 0.3 });
const stoneMat = () => new THREE.MeshStandardMaterial({ color: 0xd9d0bf, roughness: 1 });

function addSpot(s) {
  const color = s.kind === 'person' ? 0xc9643c : s.kind === 'gate' ? 0xf2ebd9 : 0xf3c46b;
  s.ring = new THREE.Mesh(new THREE.RingGeometry(1.15, 1.42, 32), new THREE.MeshBasicMaterial({ color, side: THREE.DoubleSide, transparent: true, opacity: 0.8 }));
  s.ring.rotation.x = -Math.PI / 2;
  s.ring.position.set(s.x, 0.07, s.z);
  city.group.add(s.ring);
  s.mark = beacon(s.kind === 'person' ? 0xffd27a : 0xf3c46b);
  s.mark.userData.base = s.kind === 'person' ? 2.75 : 3.9;
  s.mark.position.set(s.x, s.mark.userData.base, s.z);
  s.mark.visible = false;
  city.group.add(s.mark);
  s.label = el('div', null, 'label' + (s.kind === 'person' ? ' person' : ''));
  s.label.addEventListener('click', () => walkTo(s.x, s.z + (s.kind === 'person' ? 1.6 : 0), s));
  $('labels').append(s.label);
  spots.push(s);
}

// Actions the game offers that concern one person (found by their name in the action's own words): a way to reach them by
// walking up to the person. Every one of them stays reachable from its place and from "Everything" as well.
function concerning(p) {
  const out = [];
  for (const g of state.everything || []) for (const a of g.items) if (a.label.includes(p.id) || a.command.toLowerCase().includes(p.id.toLowerCase())) out.push(a);
  return out;
}
const speaks = p => state.decision && state.decision.speaker && state.decision.speaker.includes(p.id);

function spotInfo(s) {
  if (s.kind === 'gate') return { name: 'Leave the district', note: '', wants: false };
  if (s.kind === 'person') {
    const p = state.people.find(p => p.id === s.id);
    if (!p) return { name: s.id, note: '', wants: false };
    return { name: p.name, note: p.role.split(',')[0], wants: speaks(p) || concerning(p).length > 0 };
  }
  if (s.kind === 'site') {
    const site = state.ret.sites.find(x => x.number === s.number);
    return { name: site.place, note: ['not yet visited', 'seen', 'looked closer'][site.state], wants: site.state === 0 };
  }
  const place = state.places.find(p => p.id === s.place);
  const n = (place.actions || []).reduce((sum, g) => sum + g.items.length, 0);
  return { name: place.name, note: n > 0 ? n + (n === 1 ? ' thing to do' : ' things to do') : '', wants: n > 0 };
}

// ---------------------------------------------------------------------------------------------------------------------
// Moving

const keys = {};
addEventListener('keydown', e => {
  if (e.target.tagName === 'INPUT') return;
  keys[e.key.toLowerCase()] = true;
  if ((e.key === 'e' || e.key === 'Enter') && near && !blocked()) { e.preventDefault(); interact(); }
  if (e.key === 'Escape') closePanel();
});
addEventListener('keyup', e => { keys[e.key.toLowerCase()] = false; });
let drag = null, stick = { x: 0, y: 0, id: null };
const ray = new THREE.Raycaster(), floor = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);
canvas.addEventListener('pointerdown', e => {
  if (e.pointerType === 'touch' && e.clientX < innerWidth * 0.4 && e.clientY > innerHeight * 0.45) {
    stick.id = e.pointerId; stick.ox = e.clientX; stick.oy = e.clientY;
    const s = $('stick'); s.hidden = false; s.style.left = (e.clientX - 55) + 'px'; s.style.top = (e.clientY - 55) + 'px';
  } else drag = { id: e.pointerId, x: e.clientX, sx: e.clientX, sy: e.clientY, moved: false };
});
addEventListener('pointermove', e => {
  if (e.pointerId === stick.id) {
    const dx = e.clientX - stick.ox, dy = e.clientY - stick.oy, m = Math.max(1, Math.hypot(dx, dy) / 45);
    stick.x = dx / 45 / m; stick.y = dy / 45 / m;
    $('knob').style.left = (35 + stick.x * 35) + 'px'; $('knob').style.top = (35 + stick.y * 35) + 'px';
  } else if (drag && e.pointerId === drag.id) {
    if (Math.hypot(e.clientX - drag.sx, e.clientY - drag.sy) > 8) drag.moved = true;
    if (drag.moved) { pos.cam -= (e.clientX - drag.x) * 0.006; drag.x = e.clientX; }
  }
});
addEventListener('pointerup', e => {
  if (e.pointerId === stick.id) { stick = { x: 0, y: 0, id: null }; $('stick').hidden = true; }
  if (drag && e.pointerId === drag.id) {
    if (!drag.moved && e.target === canvas && !blocked()) {          // a tap or click on the street: walk there
      ray.setFromCamera(new THREE.Vector2(e.clientX / innerWidth * 2 - 1, -(e.clientY / innerHeight) * 2 + 1), camera);
      const hit = new THREE.Vector3();
      if (ray.ray.intersectPlane(floor, hit)) {
        const s = spots.reduce((best, s) => { const d = Math.hypot(s.x - hit.x, s.z - hit.z); return d < 2.2 && (!best || d < best.d) ? { s, d } : best; }, null);
        walkTo(hit.x, hit.z, s ? s.s : null);
      }
    }
    drag = null;
  }
});
canvas.addEventListener('wheel', e => { pos.dist = Math.max(6, Math.min(20, pos.dist + e.deltaY * 0.01)); e.preventDefault(); }, { passive: false });

function walkTo(x, z, then) { pos.target = { x, z }; pos.then = then || null; }

let near = null, clock = 0;
const blocked = () => !$('card').hidden || !$('full').hidden;
const hitSolid = (x, z) => city.solids.some(b => Math.abs(x - b.x) < b.hw && Math.abs(z - b.z) < b.hd);

function step(dt) {
  clock += dt;
  let fx = 0, fz = 0;
  if (!blocked()) {
    if (keys['w'] || keys['arrowup']) fz -= 1;
    if (keys['s'] || keys['arrowdown']) fz += 1;
    if (keys['a']) fx -= 1;
    if (keys['d']) fx += 1;
    if (keys['arrowleft'] || keys['q']) pos.cam += dt * 1.8;
    if (keys['arrowright']) pos.cam -= dt * 1.8;
    fx += stick.x; fz += stick.y;
  } else pos.target = null;
  let dx = 0, dz = 0;
  const len = Math.hypot(fx, fz);
  if (len > 0.05) {
    pos.target = null;
    const s = Math.sin(pos.cam), c = Math.cos(pos.cam);
    dx = (fx * c + fz * s) / Math.max(1, len); dz = (-fx * s + fz * c) / Math.max(1, len);
  } else if (pos.target) {
    const tx = pos.target.x - pos.x, tz = pos.target.z - pos.z, d = Math.hypot(tx, tz);
    if (d < (pos.then ? 1.4 : 0.35)) { const then = pos.then; pos.target = null; pos.then = null; if (then) { near = then; interact(); } }
    else { dx = tx / d; dz = tz / d; }
  }
  const moving = Math.hypot(dx, dz);
  if (moving > 0.05) {
    const speed = 7 * dt;
    let nx = Math.max(-HALF + 6, Math.min(HALF - 6, pos.x + dx * speed)), nz = Math.max(-HALF + 6, Math.min(HALF - 6, pos.z + dz * speed));
    const beforeX = pos.x, beforeZ = pos.z;
    if (!hitSolid(nx, pos.z)) pos.x = nx;
    if (!hitSolid(pos.x, nz)) pos.z = nz;
    if (pos.target && Math.hypot(pos.x - beforeX, pos.z - beforeZ) < speed * 0.2) { pos.target = null; pos.then = null; }   // walked into a wall
    let turn = Math.atan2(dx, dz) - pos.yaw; turn = Math.atan2(Math.sin(turn), Math.cos(turn)); pos.yaw += turn * Math.min(1, dt * 12);
  }
  pos.speed += ((moving > 0.05 ? 1 : 0) - pos.speed) * Math.min(1, dt * 10);
  hero.userData.animate(dt, pos.speed > 0.1 ? pos.speed : 0);
  hero.position.x = pos.x; hero.position.z = pos.z; hero.rotation.y = pos.yaw;

  // The camera follows from behind and above, easing; the sun's shadow follows the character.
  const want = new THREE.Vector3(pos.x + Math.sin(pos.cam) * pos.dist, 2.2 + pos.dist * 0.26, pos.z + Math.cos(pos.cam) * pos.dist);
  camNow.lerp(want, Math.min(1, dt * 6)); lookNow.lerp(new THREE.Vector3(pos.x, 2.5, pos.z), Math.min(1, dt * 9));
  camera.position.copy(camNow); camera.lookAt(lookNow);
  sun.position.set(pos.x + 26, 44, pos.z + 18); sun.target.position.set(pos.x, 0, pos.z);
  if (dome) dome.position.set(pos.x, 0, pos.z);

  for (const w of walkers) {
    const u = w.userData.walk; u.t += dt * u.speed;
    const p = ((u.t % 46) + 46) % 46 - 23;
    w.position.x = u.along ? p : u.lane; w.position.z = u.along ? u.lane : p;
    w.rotation.y = u.along ? (u.speed > 0 ? Math.PI / 2 : -Math.PI / 2) : (u.speed > 0 ? 0 : Math.PI);
    w.userData.animate(dt, Math.abs(u.speed) / 1.6);
  }
  for (const f of city.animated) f(dt, clock);

  // What can be walked up to: labels, markers over what wants you, and the nearest one in reach.
  let best = null, bestD = 3.3;
  const v = new THREE.Vector3(), quiet = blocked();
  for (const s of spots) {
    const d = Math.hypot(s.x - pos.x, s.z - pos.z), info = spotInfo(s);
    if (d < bestD) { best = s; bestD = d; }
    s.ring.material.opacity = 0.55 + Math.sin(clock * 3) * 0.2 + (s === near ? 0.25 : 0);
    s.ring.scale.setScalar(s === near ? 1.12 : 1);
    s.mark.visible = info.wants; if (info.wants) s.mark.userData.spin(dt, clock);
    if (s.fig) { s.fig.userData.animate(dt, 0); if (d < 9) { let t = Math.atan2(pos.x - s.x, pos.z - s.z) - s.fig.rotation.y; t = Math.atan2(Math.sin(t), Math.cos(t)); s.fig.rotation.y += t * Math.min(1, dt * 4); } }
    v.set(s.x, s.kind === 'person' ? 2.5 : 3.2, s.z).project(camera);
    const show = v.z < 1 && d < 38 && !quiet;
    s.label.style.display = show ? '' : 'none';
    if (show) {
      s.label.style.left = ((v.x + 1) / 2 * innerWidth) + 'px'; s.label.style.top = ((1 - v.y) / 2 * innerHeight) + 'px';
      const text = info.name + '|' + info.note;
      if (s.label.dataset.t !== text) { s.label.dataset.t = text; s.label.textContent = info.name; if (info.note) s.label.append(el('small', info.note)); }
      s.label.classList.toggle('near', s === near);
    }
  }
  if (best !== near) {
    near = best;
    if (near) $('interact').textContent = (near.kind === 'gate' ? 'Travel' : near.kind === 'person' ? 'Talk' : near.kind === 'site' ? 'Look' : 'Go in') + '  ·  E';
  }
  $('interact').hidden = !near || quiet || !$('panel').hidden;
}
$('interact').addEventListener('click', () => interact());

function interact() {
  if (!near) return;
  pos.target = null;
  if (near.kind === 'gate') return openPanel('travel');
  if (near.kind === 'person') {
    const p = state.people.find(p => p.id === near.id);
    if (p && speaks(p) && view.dismissed === state.decision.key) { view.dismissed = ''; return render(); }   // they were waiting for your answer
    return openPanel('person:' + near.id);
  }
  if (near.kind === 'site') return openPanel('site:' + near.number);
  openPanel('place:' + near.place);
}

function travel(id) {
  buildDistrict(id);
  pos.x = 0; pos.z = 14; pos.cam = 0; pos.yaw = Math.PI; pos.target = null;
  camNow.set(0, 9, 30); lookNow.set(0, 1.7, 14);
  closePanel();
  render.key = districtKey();
  say(DISTRICTS[id].name + '.');
  render();
}

// ---------------------------------------------------------------------------------------------------------------------
// Screens (drawn from the models)

function openPanel(what) { view.panel = what; render(); }
function closePanel() { view.panel = null; $('panel').hidden = true; }
$('panelClose').addEventListener('click', closePanel);
const districtKey = () => view.district + '|' + state.hud.away + '|' + state.hud.machineReady + '|' + (state.people || []).filter(p => !p.away).map(p => p.id).join(',') + '|' + (state.ret ? state.ret.sites.length : 0);

async function render() {
  const s = state, phase = s.phase;
  const full = $('full'), fullBody = $('fullBody'), card = $('card'), cardBody = $('cardBody');
  full.hidden = true; card.hidden = true;
  const era = phase === 'Rome' || (phase === 'Opening' && view.openingRead);

  // The city follows the game: a jump or a new acquaintance redraws the district.
  if (render.key !== districtKey()) { if (s.hud.away) view.district = 'subura'; buildDistrict(view.district); render.key = districtKey(); }

  // HUD
  const hud = $('hud');
  hud.hidden = !(era || phase === 'Return' || phase === 'AfterReturn');
  const facts = $('facts'), nav = $('nav');
  facts.replaceChildren(el('b', s.hud.date));
  nav.replaceChildren();
  $('health').replaceChildren();
  if (era) {
    facts.append(el('span', DISTRICTS[view.district].name, 'where'));
    facts.append(el('span', s.hud.life, 'dim'));
    const att = el('span', null, 'att'); att.title = 'Attention';
    for (let k = 0; k < s.hud.attentionTotal; k++) att.append(el('i', null, k < s.hud.attentionFree ? 'on' : ''));
    att.append(document.createTextNode(' ' + s.hud.attentionFree + ' free of ' + s.hud.attentionTotal));
    facts.append(att);
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
  $('hint').hidden = !(era || phase === 'Return') || !!render.hinted;

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

  // The narrative view: one choice at a time; it can be set aside and waits in the HUD (or with the person who asked).
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
  render.hinted = true; $('hint').hidden = true;
  body.replaceChildren();
  const groups = gs => { for (const g of gs) if (g.items.length) body.append(el('h3', g.title), actions(g.items)); };
  if (what === 'travel') {
    body.append(el('h2', 'Where to?'));
    const list = el('div', null, 'acts');
    for (const id of TRAVEL_ORDER) {
      const here = id === view.district;
      const n = DISTRICTS[id].spots.reduce((sum, sp) => sum + ((s.places.find(p => p.id === sp.place).actions || []).reduce((a, g) => a + g.items.length, 0)), 0);
      list.append(button(DISTRICTS[id].name + (here ? '  (you are here)' : n ? '  ·  ' + n + ' to do' : ''), () => travel(id), false, here || s.hud.away));
    }
    body.append(list);
  } else if (what.startsWith('place:')) {
    const place = s.places.find(p => p.id === what.slice(6));
    body.append(el('h2', place.name), el('p', place.caption, 'muted'));
    const look = await ask('Look', place.id);
    if (view.panel !== what) return;
    if (look) body.append(el('p', look));
    if (place.id === 'lodging') { if (s.readyLine) body.append(el('p', s.readyLine)); for (const n of s.news || []) body.append(el('p', n, 'muted')); }
    groups(place.actions || []);
    if (!(place.actions || []).some(g => g.items.length)) body.append(el('p', 'Nothing to do here now.', 'muted'));
    if (place.id === 'lodging' && s.phase !== 'Return') { body.append(el('h3', 'The month')); body.append(actions([{ label: 'Sleep: end the month', command: 'end' }])); }
  } else if (what.startsWith('person:')) {
    const p = s.people.find(p => p.id === what.slice(7));
    if (!p) return closePanel();
    body.append(el('div', initials(p.name), 'portrait'), el('h2', p.name), el('p', p.role, 'muted'), el('p', p.status));
    const theirs = concerning(p);
    if (theirs.length) body.append(el('h3', 'Between you'), actions(theirs));
    for (const w of [...s.work.commissions, ...s.work.underWay].filter(w => (w.who || '').includes(p.id))) { const it = el('div', null, 'item'); it.append(el('b', w.title), el('span', w.state)); body.append(it); }
    for (const [k, v] of [['Household', p.household], ['Cares about', p.caresAbout], ['Wants', p.wants]]) if (v) { body.append(el('h3', k), el('p', v)); }
  } else if (what === 'people') {
    body.append(el('h2', 'People'));
    if (!s.people.length) body.append(el('p', 'You know no one in Rome yet.', 'muted'));
    for (const p of s.people) {
      const it = el('div', null, 'item');
      it.append(el('b', p.name + (p.away ? '  (away)' : '')), el('span', p.role), el('span', p.status));
      if (!p.away && PEOPLE[p.id]) it.append(el('span', 'Usually found at: ' + DISTRICTS[PEOPLE[p.id].home].name));
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
      body.append(el('b', cap(sys.id) + '  ' + sys.done + '/' + sys.total));
      const bar = el('div', null, 'bar'); const fillBar = el('i'); fillBar.style.width = (sys.total ? sys.done / sys.total * 100 : 0) + '%'; bar.append(fillBar); body.append(bar);
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

async function restart(seed) {
  Object.assign(view, { district: 'subura', panel: null, dismissed: '', openingRead: false, beat: 0 });
  $('feed').replaceChildren();
  state = await call('Start', boot.files, seed || String(crypto.getRandomValues(new Uint32Array(1))[0] % 100000));
  render.key = null; pos.x = 0; pos.z = 14;
  render();
}

// ---------------------------------------------------------------------------------------------------------------------
// The frame

function resize() { renderer.setSize(innerWidth, innerHeight, false); camera.aspect = innerWidth / innerHeight; camera.updateProjectionMatrix(); }
addEventListener('resize', resize);
resize();
let before = performance.now();
// The page watches its own frame rate and gives up detail before it gives up smoothness (a slow phone, or no graphics card).
const asked = new URLSearchParams(location.search).get('q');
const quality = { level: asked === 'low' ? 2 : 0, slow: 0, frames: 0, off: asked === 'off' };   // q=off: play without drawing (scripted tests of the game's flow)
function setQuality(level) {
  quality.level = level;
  renderer.setPixelRatio(level === 0 ? Math.min(devicePixelRatio, 1.75) : 1);
  renderer.shadowMap.enabled = level < 2; sun.castShadow = level < 2;
  sun.shadow.mapSize.set(level === 0 ? 2048 : 1024, level === 0 ? 2048 : 1024); if (sun.shadow.map) { sun.shadow.map.dispose(); sun.shadow.map = null; }
  scene.traverse(o => { if (o.material) [].concat(o.material).forEach(m => { m.needsUpdate = true; }); });
  resize();
}
function frame(now) {
  const raw = (now - before) / 1000;
  if (state && city && quality.level < 2 && ++quality.frames > 30) { quality.slow = raw > 0.045 ? quality.slow + 1 : Math.max(0, quality.slow - 1); if (quality.slow > 40) { quality.slow = 0; setQuality(quality.level + 1); } }
  const dt = Math.min(0.05, raw);
  before = now;
  if (state && city) step(dt);
  if (!quality.off) renderer.render(scene, camera);
  requestAnimationFrame(frame);
}
if (quality.level) setQuality(quality.level);
requestAnimationFrame(frame);
window.butterfly = { get state() { return state; }, act, view, pos, travel, openPanel, walkTo, renderer, quality };   // for the browser test only
boot().catch(e => { $('boot').textContent = 'Could not start: ' + e; console.error(e); });
