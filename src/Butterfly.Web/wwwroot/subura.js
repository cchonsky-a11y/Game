// The Subura: the benchmark district (2026-11-11). A real street plan, drawn from the art pack, that shows what the game
// says is true of it (the fountain as it is, the sickness, the years gone by) and has people in it with things to do.
// Presentation only. Nothing here decides anything: `facts` is Butterfly.Presentation's StreetFacts, read and drawn.
//
// The plan (x runs east, z runs south; the player comes in on the main street):
//   - The main street runs east-west under the great firewall of the Forum of Augustus, which closes the south side.
//   - Two lanes climb north from it. The lodging house stands at the corner of the first; its yard holds the machine.
//   - The second lane opens into a small square with the public fountain and the crossroads shrine.
// Historical basis and what is reconstruction: art/README.md.
import * as THREE from './lib/three.module.min.js';
import { figure, sequence, kitSize, textures } from './city.js';

const NAMES = ['insula_a', 'insula_b', 'insula_c', 'insula_d', 'insula_e', 'insula_f'];
const SOUTH = 0, NORTH = Math.PI, EAST = Math.PI / 2, WEST = -Math.PI / 2;      // which way a front faces

export const SUBURA = {
  bounds: { x0: -44.5, x1: 44.5, z0: -30.3, z1: 12.7 },
  spawn: { x: -36, z: 10, cam: -Math.PI / 2, yaw: Math.PI / 2 },
  spots: { lodging: { x: -26.3, z: 8.6 }, subura: { x: 21, z: -6.2 } },
  gates: [{ x: -42.5, z: 10 }, { x: 42.5, z: 10 }, { x: 30, z: 12.2 }],
  people: { x: 20, z: -13 },                                                    // where known people of the district stand
  sites: k => ({ x: -34 + k * 11.5, z: k % 2 ? 11.6 : 8.2 }),                    // where the return's places are marked
};

export function buildSubura(c, facts) {
  const later = facts.yearsAway > 0;
  const plan = sequence('subura-plan');                // the same streets before and after the jump
  const change = sequence('subura-years-' + facts.yearsAway);
  const life = [];

  c.ground(textures.earth(later), 240, 40);
  c.pave(92, 6.4, 0, 10);                              // the main street
  c.pave(4.9, 39, -18, -12.2); c.pave(4.9, 39, 14, -12.2);     // the two lanes
  c.pave(15, 14.4, 23.6, -10);                         // the fountain square

  // Raised footways along the shop fronts, broken where the lanes and the alley come in, and stepping stones across.
  const walks = [[-46, -20.3], [-15.7, -3.4], [-0.6, 11.7], [16.3, 46]], KERB = 8.45, UP = 0.18;
  for (const [a, b] of walks) c.pavement(a, b, 7.0, KERB, UP);
  for (const x of [-10.5, 21.5]) for (let k = 0; k < 3; k++) c.steppingStone(x, 9.3 + k * 1.3);
  const ground = (x, z) => z > 6 && z < KERB && walks.some(([a, b]) => x > a && x < b) ? UP : 0;

  // A row of tenements, shoulder to shoulder. After a jump some have been rebuilt taller, and some are gone.
  const lots = [];
  function row(from, to, place, skip) {
    let p = from;
    while (p < to - 5) {
      let name = NAMES[Math.floor(plan() * NAMES.length)];
      const w = kitSize(name).hw * 2, mid = p + w / 2;
      p += w + 0.05;
      if (skip && skip(mid, w)) continue;
      let fate = 'stands';
      if (later) {
        const r = change();
        const lost = 0.05 + Math.min(0.25, facts.plagueDeadShare * 1.5) + Math.min(0.1, facts.yearsAway / 600);
        fate = r < lost ? 'gone' : r < lost + 0.16 ? 'rebuilt' : 'stands';
      }
      const [x, z, turn] = place(mid, kitSize(name).hd);
      if (fate === 'gone') { ruin(x, z, turn, kitSize(name)); continue; }
      if (fate === 'rebuilt') name = kitSize(name).hw > 4 ? 'insula_c' : 'insula_e';   // the same plot, built higher
      c.kit(name, x, z, turn);
      lots.push({ x, z, turn, hw: kitSize(name).hw, hd: kitSize(name).hd });
    }
  }
  // A burnt or fallen plot: stumps of wall, charred beams, weeds. (Rome's tenements burned and fell often.)
  function ruin(x, z, turn, size) {
    const g = new THREE.Group(); g.position.set(x, 0, z); g.rotation.y = turn; c.group.add(g);
    const wall = new THREE.MeshStandardMaterial({ color: 0x8a7a68, roughness: 1 }), char = new THREE.MeshStandardMaterial({ color: 0x2a221c, roughness: 1 });
    for (const [bx, bz, w, h, d] of [[-size.hw + 0.3, 0, 0.5, 2.2 + change() * 2, size.hd * 2], [size.hw - 0.3, 0, 0.5, 1.2 + change() * 1.5, size.hd * 2], [0, -size.hd + 0.3, size.hw * 2, 1.5 + change() * 2.5, 0.5]]) {
      const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), wall); m.position.set(bx, h / 2, bz); m.castShadow = m.receiveShadow = true; g.add(m);
    }
    for (let k = 0; k < 5; k++) { const m = new THREE.Mesh(new THREE.BoxGeometry(0.22, 0.22, 3 + change() * 2), char); m.position.set((change() - 0.5) * size.hw * 1.4, 0.2 + change() * 0.5, (change() - 0.5) * size.hd); m.rotation.set(change() * 0.4, change() * 3, change() * 0.5); m.castShadow = true; g.add(m); }
    for (let k = 0; k < 7; k++) { const m = new THREE.Mesh(new THREE.ConeGeometry(0.25, 0.7 + change() * 0.6, 5), new THREE.MeshStandardMaterial({ color: 0x5a6a3a, roughness: 1 })); m.position.set((change() - 0.5) * size.hw * 1.6, 0.35, (change() - 0.5) * size.hd * 1.6); g.add(m); }
    const side = Math.abs(Math.sin(turn)) > 0.5;
    c.solid(x, z, side ? size.hd : size.hw, side ? size.hw : size.hd);
  }

  // North side of the main street (fronts on z = 7), broken by the two lanes and one alley.
  c.kit('insula_a', -26.3, 3, SOUTH);                                            // the lodging house, always standing
  row(-46.2, -31.1, (m, hd) => [m, 7 - hd, SOUTH]);
  row(-15.7, 11.7, (m, hd) => [m, 7 - hd, SOUTH], (m, w) => Math.abs(m + 2) < w / 2 + 1.4);   // an alley left open at x = -2
  row(16.3, 46.5, (m, hd) => [m, 7 - hd, SOUTH]);
  // The lanes: fronts on both sides. The lodging's yard is left open on the first lane.
  row(-31, -2, (m, hd) => [-20.3 - hd, m, EAST], (m, w) => m + w / 2 > -11.5);
  row(-31, -2.2, (m, hd) => [-15.7 + hd, m, WEST]);
  row(-31, -2.2, (m, hd) => [11.7 - hd, m, EAST]);
  row(-31, -17.3, (m, hd) => [16.3 + hd, m, WEST]);
  row(-17.2, -2.8, (m, hd) => [31.2 + hd, m, WEST]);                              // the far side of the square
  row(-46, 46.5, (m, hd) => [m, -31.2 - hd, SOUTH]);                             // the lanes end on a wall of houses

  // The firewall of the Forum of Augustus: a cliff of grey stone along the south side of the street, with one arch.
  c.firewall(-50, 50, 13.4, 30, later);
  c.wallArch(30, 13.4);
  c.kit('temple', 6, 34, NORTH, 1.9, false);                                     // the roof of Mars the Avenger shows above the wall
  for (const x of [-44.8, 44.8]) c.arch(x, 10, Math.PI / 2);

  // The lodging's yard: the machine under a lean-to, behind a low wall.
  c.shed(-25.5, -6.3); c.machine(-25.5, -6.3, facts.machineReady);
  c.lowWall(-30.5, -20.4, -11.4); c.lowWall(-30.6, -30.6, -11.4, -1.2, true);
  for (const [x, z] of [[-29.5, -2.2], [-29, -3], [-22, -10.4]]) c.amphora(x, z, 1);
  c.crate(-22.3, -2.6); c.brazier(-21.5, -7.5);

  // Street furniture.
  c.thermopolium(7.2, 7.9, NORTH);
  c.shrine(17.6, -4.2);
  for (const [x, col] of [[-33, '#a8482c'], [-9, '#3f6077'], [19, '#7a6a3f'], [38, '#5a7a4a']]) c.stall(x, 12.1, col, NORTH);
  c.cart(-11, 11.6, 0.2); c.cart(27.5, -15.2, 1.2);
  for (const [x, z] of [[-6.5, 7.6], [-6, 7.9], [24.5, 7.7], [-37.5, 7.6], [10.6, -20], [-14.6, -24]]) c.amphora(x, z, 0.9 + plan() * 0.4);
  for (const [x, z] of [[2.5, 7.7], [-19.8, -16], [36, 7.7], [16.9, -24.5]]) c.crate(x, z);
  c.laundry(-20.2, -18, -15.8, -18.5, 7.2); c.laundry(-20.2, -25, -15.8, -24.2, 6.4); c.laundry(11.8, -22, 16.2, -21.4, 7); c.laundry(11.8, -6, 16.2, -6.5, 6.6);
  c.brazier(-21.4, 7.7); c.brazier(16.9, 7.7);
  c.umbrellaPine(28.6, -15.4, 0.8);
  // The inner court behind the alley: a well, washing, somebody's jars.
  c.well(-2, -9); c.laundry(-8, -6, 3.5, -6.5, 3.2); c.amphora(-5, -11, 1.1); c.amphora(1.5, -12.5, 1); c.crate(2.6, -4.4);

  // The fountain, as the game says it is.
  fountain(c, facts, later, life, plan);

  // What the inventor's work left in the street (shown only when the game says Rome can now do it).
  if (facts.reproducible.includes('metrology')) c.bronzeFoot(19.2, -16.9);       // the shared foot, fixed to a wall of the square
  if (facts.reproducible.includes('valveseats')) { c.standpipe(-12.4, 7.6); if (later) c.standpipe(33.5, 7.6); }

  people(c, facts, later, life, plan);
  return { life, ground };
}

function fountain(c, facts, later, life, plan) {
  const X = 24, Z = -10, state = facts.fountain;
  c.fountain(X, Z, state === 'clean' ? 'clean' : state === 'worn' ? 'worn' : state === 'dry' ? 'dry' : 'foul');
  if (state === 'foul' || state === 'worn') {                                    // slime where it overflows
    const slick = new THREE.Mesh(new THREE.CircleGeometry(2.6, 20), new THREE.MeshStandardMaterial({ color: 0x3f4a2a, roughness: 0.4, transparent: true, opacity: 0.55 }));
    slick.rotation.x = -Math.PI / 2; slick.position.set(X - 0.6, 0.03, Z - 1.6); c.group.add(slick);
  }
  if (state === 'works') {                                                       // the repair under way: scaffold, pipe, two men
    const pole = new THREE.MeshStandardMaterial({ color: 0x7a5c3c, roughness: 1 }), lead = new THREE.MeshStandardMaterial({ color: 0x6f7478, metalness: 0.7, roughness: 0.5 });
    for (const [dx, dz] of [[-2.3, -1.9], [2.3, -1.9], [-2.3, 1.9], [2.3, 1.9]]) { const m = new THREE.Mesh(new THREE.CylinderGeometry(0.06, 0.07, 3.6, 6), pole); m.position.set(X + dx, 1.8, Z + dz); m.castShadow = true; c.group.add(m); }
    for (const dz of [1.9]) { const m = new THREE.Mesh(new THREE.BoxGeometry(4.8, 0.08, 0.5), pole); m.position.set(X, 2.6, Z + dz); m.castShadow = true; c.group.add(m); }
    for (let k = 0; k < 4; k++) { const m = new THREE.Mesh(new THREE.CylinderGeometry(0.07, 0.07, 2.6, 8), lead); m.rotation.z = Math.PI / 2; m.position.set(X - 4.2, 0.1 + (k > 1 ? 0.14 : 0), Z + 2.6 + (k % 2) * 0.16); c.group.add(m); }
    for (const [dx, dz, turn] of [[-2.6, 0.4, EAST], [1.2, -2.5, 0]]) { const f = figure(0x8a6f52, { rnd: plan }); f.position.set(X + dx, 0, Z + dz); f.rotation.y = turn; c.group.add(f); life.push({ fig: f, kind: 'work', t: plan() * 6 }); }
  }
  if (state === 'dry') for (let k = 0; k < 6; k++) { const m = new THREE.Mesh(new THREE.ConeGeometry(0.2, 0.6, 5), new THREE.MeshStandardMaterial({ color: 0x5a6a3a, roughness: 1 })); m.position.set(X - 2 + plan() * 4, 0.3, Z - 2.2 + plan() * 0.6); c.group.add(m); }
}

// People with somewhere to go. Cosmetic: who is there follows the street's facts (clean water draws a crowd, the
// sickness empties the street), but none of it is the game's own people or decides anything.
function people(c, facts, later, life, plan) {
  const sick = facts.plague === 'outbreak';
  const cloth = [0xb9a27c, 0x8a6f52, 0xa9b0a0, 0x9c7b6b, 0xc7b79a, 0x7a8a6a, 0x8a5a4a, 0xa88a5a];
  const dress = k => { const r = plan(); return r < 0.42 ? { rnd: plan } : r < 0.6 ? { rnd: plan, long: true, toga: 0xe8e0d0 } : r < 0.9 ? { rnd: plan, long: true, palla: [0x8a5a6a, 0x5a7a8a, 0xa8824a, 0x6a7a5a][k % 4] } : { rnd: plan, scale: 0.62 }; };
  const add = (path, opts = {}) => {
    const k = life.length, f = figure(cloth[k % cloth.length], opts.dress || dress(k));
    if (opts.carry) { const jar = c.kitPiece(opts.carry); jar.scale.setScalar(0.75); jar.position.set(0.28, 1.5, 0); jar.rotation.z = -0.5; f.add(jar); }
    f.position.set(path[0].x, 0, path[0].z); c.group.add(f);
    life.push({ fig: f, kind: 'walk', path, i: 1, dir: 1, speed: opts.speed || 1.1 + plan() * 0.7, pause: plan() * 3, loop: !!opts.loop });
  };
  // Shoppers along the main street, stopping at shop fronts.
  const n = sick ? 3 : later ? 8 : 9;
  for (let k = 0; k < n; k++) {
    const lane = 8.6 + plan() * 3, path = [];
    for (let x = -43; x <= 43; x += 7 + plan() * 5) path.push(plan() < 0.4 ? { x, z: 7.9, stop: 2 + plan() * 4, face: NORTH } : { x, z: lane });
    const start = Math.floor(plan() * path.length); add(path.slice(start).concat(path.slice(0, start).reverse()));
  }
  // People of the lanes.
  for (const lx of [-18, 14]) for (let k = 0; k < (sick ? 1 : 2); k++) add([{ x: lx + plan() - 0.5, z: 8 }, { x: lx + plan() - 0.5, z: -8, stop: plan() * 3, face: EAST }, { x: lx + plan() - 0.5, z: -29 }]);
  // Water carriers: to the fountain and home again when it runs clean; otherwise out of the district for their water.
  const clean = facts.fountain === 'clean';
  if (!sick || clean) for (let k = 0; k < (clean ? 4 : 2); k++) {
    const home = [{ x: -18, z: -20 - k * 2 }, { x: 14, z: -26 + k }, { x: -30 + k * 9, z: 9 }, { x: 36, z: 9.5 }][k];
    add(clean ? [home, { x: home.x, z: 9.5 }, { x: 14, z: 9.5 }, { x: 14, z: -8 }, { x: 21.6, z: -9.2, stop: 4, face: EAST }]
              : [home, { x: home.x, z: 10.5 }, { x: 43, z: 10.5, stop: 6 }], { carry: 'amphora_wine', dress: { rnd: plan, long: plan() < 0.6, palla: plan() < 0.5 ? 0x6a7a5a : undefined } });
  }
  // Children, where there is water to play in.
  if (clean && !sick) for (let k = 0; k < 3; k++) { const path = []; for (let a = 0; a < 8; a++) path.push({ x: 24 + Math.cos(a / 8 * 6.283 + k) * (3.6 + k * 0.5), z: -10 + Math.sin(a / 8 * 6.283 + k) * 3.4 }); add(path, { dress: { rnd: plan, scale: 0.6 }, speed: 2.6, loop: true }); }
  // Shopkeepers behind their counters, and neighbours talking.
  if (!sick) for (const [x, z, turn] of [[-37.8, 6.4, SOUTH], [-9.2, 6.4, SOUTH], [7.2, 6.6, SOUTH], [24, 6.4, SOUTH], [39, 6.4, SOUTH], [-33, 12.9, NORTH], [19, 12.9, NORTH]]) {
    const f = figure(cloth[Math.floor(plan() * cloth.length)], { rnd: plan }); f.position.set(x, 0, z); f.rotation.y = turn; c.group.add(f); life.push({ fig: f, kind: 'stand', home: turn });
  }
  for (const [x, z] of sick ? [] : [[-3.5, 11.4], [28.5, -5.2], [-18.4, -13]]) for (const s of [-1, 1]) {
    const f = figure(cloth[Math.floor(plan() * cloth.length)], dress(life.length)); f.position.set(x + s * 0.55, 0, z); f.rotation.y = s > 0 ? WEST : EAST; c.group.add(f); life.push({ fig: f, kind: 'talk', t: plan() * 6, home: f.rotation.y });
  }
}

// One frame of the street's life. `hero` is where the player stands, so people can look up as you pass.
export function liveSubura(life, dt, time, hero, ground) {
  for (const a of life) {
    if (ground) a.fig.userData.ground = ground(a.fig.position.x, a.fig.position.z);
    if (a.kind === 'walk') {
      if (a.pause > 0) { a.pause -= dt; a.fig.userData.animate(dt, 0); continue; }
      const t = a.path[a.i], dx = t.x - a.fig.position.x, dz = t.z - a.fig.position.z, d = Math.hypot(dx, dz);
      if (d < 0.15) {
        if (t.stop) { a.pause = t.stop; if (t.face !== undefined) a.fig.rotation.y = t.face; }
        a.i += a.dir;
        if (a.i >= a.path.length) { if (a.loop) a.i = 0; else { a.dir = -1; a.i = a.path.length - 2; } }
        else if (a.i < 0) { a.dir = 1; a.i = 1; }
        continue;
      }
      const step = Math.min(d, a.speed * dt);
      a.fig.position.x += dx / d * step; a.fig.position.z += dz / d * step;
      let turn = Math.atan2(dx, dz) - a.fig.rotation.y; turn = Math.atan2(Math.sin(turn), Math.cos(turn)); a.fig.rotation.y += turn * Math.min(1, dt * 8);
      a.fig.userData.animate(dt, a.speed / 1.6);
    } else {
      a.fig.userData.animate(dt, 0);
      const near = Math.hypot(hero.x - a.fig.position.x, hero.z - a.fig.position.z) < 4.5;
      let want = a.home ?? a.fig.rotation.y;
      if (near && a.kind === 'stand') want = Math.atan2(hero.x - a.fig.position.x, hero.z - a.fig.position.z);     // a shopkeeper looks up
      if (a.kind === 'talk') want += Math.sin(time * 1.3 + a.t) * 0.25;
      if (a.kind === 'work') a.fig.position.y = Math.max(0, Math.sin(time * 4 + a.t)) * 0.06;
      let turn = want - a.fig.rotation.y; turn = Math.atan2(Math.sin(turn), Math.cos(turn)); a.fig.rotation.y += turn * Math.min(1, dt * 3);
    }
  }
}
