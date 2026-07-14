// TRUNK! — pure game logic. No DOM, no Date, no Math.random: fully deterministic.

export interface Input {
  left: boolean;
  right: boolean;
  aimX: number; // world coords
  aimY: number;
  shoot: boolean;    // edge-triggered by caller (true for one step per click)
  shootHeld: boolean;
  interact: boolean; // E, edge-triggered
  interactHeld: boolean;
}

export const IDLE_INPUT: Input = {
  left: false, right: false, aimX: 0, aimY: 0,
  shoot: false, shootHeld: false, interact: false, interactHeld: false,
};

export interface Rect { x: number; y: number; w: number; h: number }
export interface Vec { x: number; y: number }

export interface Platform extends Rect {
  oneWay?: boolean;
  deco?: "grass" | "stone" | "metal" | "cloud";
}

export interface MovingPlatform extends Rect {
  x0: number; x1: number; y0: number; y1: number; // travel endpoints (lerp)
  speed: number; t: number; dir: 1 | -1;
  ricochet: boolean; // green = bullets bounce; red = bullets die
  flashing?: { interval: number; timer: number }; // toggles ricochet
}

export interface SpikeBall { x: number; y: number; r: number }
export interface SpikeStrip extends Rect {}

export interface Lever {
  x: number; y: number;
  on: boolean;
  targets: string[]; // door ids
}

export interface Door extends Rect { id: string; open: boolean; openAmount: number }

export interface PushBox extends Rect { vx: number; onPlate: boolean }
export interface Plate extends Rect { pressed: boolean; targets: string[] }

export interface WindZone extends Rect { force: number }

export interface Rail { points: Vec[]; } // polyline, param by arc length
export interface Minecart {
  rail: Rail;
  dist: number;        // distance along rail
  speed: number;
  riding: boolean;
  started: boolean;
  finished: boolean;
  x: number; y: number;
}

export interface Bullet {
  x: number; y: number;
  dx: number; dy: number; // unit dir
  speed: number;
  bounces: number;
  dead: boolean;
  age: number;
}

export type BossPhase = "waiting" | "intro" | "chase" | "windup" | "leap" | "dead";

export interface Boss {
  x: number; y: number; vx: number; vy: number;
  w: number; h: number;
  hp: number; maxHp: number;
  phase: BossPhase;
  timer: number;
  attackCooldown: number;
  leapCooldown: number;
  hitFlash: number;
  faceDir: 1 | -1;
  dodgeCooldown: number;
}

export interface Particle {
  x: number; y: number; vx: number; vy: number;
  life: number; maxLife: number;
  kind: "spark" | "puff" | "hit" | "confetti";
  hue: number;
}

export interface Sign { x: number; y: number; text: string }

export interface Dodger {
  x: number; y: number; homeX: number; homeY: number;
  hp: number; dead: boolean;
  phase: number; dodgeVx: number; hitFlash: number;
}

export interface GrabRing { x: number; y: number; r: number }
export interface Spring { x: number; y: number; w: number; revealed: boolean; compress: number }
export interface Heart { x: number; y: number; taken: boolean }

export interface Player {
  x: number; y: number;   // center
  w: number; h: number;
  vx: number; vy: number;
  grounded: boolean;
  coyote: number;
  face: 1 | -1;
  hp: number; maxHp: number;
  iframes: number;
  shootCooldown: number;
  aimX: number; aimY: number;
  aimActive: number; // seconds remaining of "deliberate aim"
  dead: boolean; deathTimer: number;
  walkPhase: number;
  landedImpact: number; // for squash-stretch, set on landing
  inCart: boolean;
}

export interface State {
  t: number;
  player: Player;
  bullets: Bullet[];
  particles: Particle[];
  platforms: Platform[];
  movers: MovingPlatform[];
  spikeBalls: SpikeBall[];
  spikeStrips: SpikeStrip[];
  levers: Lever[];
  doors: Door[];
  boxes: PushBox[];
  plates: Plate[];
  winds: WindZone[];
  cart: Minecart;
  boss: Boss;
  signs: Sign[];
  dodgers: Dodger[];
  rings: GrabRing[];
  springs: Spring[];
  hearts: Heart[];
  tether: { ringIndex: number; len: number } | null;
  zoom: number; zoomTarget: number;
  paused: boolean;
  checkpoints: Vec[];
  checkpoint: Vec;
  deathY: number;       // below this = death
  camX: number; camY: number;
  shake: number;
  rngState: number;
  won: boolean;
  wonTimer: number;
  bossArenaX: number;   // boss activates when player passes this
  stats: { shots: number; bounces: number; launches: number; deaths: number; damageTaken: number };
  toast: { text: string; timer: number };
}

// ---------- deterministic rng ----------
function rng(s: State): number {
  s.rngState |= 0;
  s.rngState = (s.rngState + 0x6d2b79f5) | 0;
  let t = Math.imul(s.rngState ^ (s.rngState >>> 15), 1 | s.rngState);
  t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
  return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
}

// ---------- level ----------
// Units: 1u ≈ 40px. Player is 0.9w x 1.3h. World ~205u wide.
export function makeLevel(): State {
  const P: Platform[] = [];
  const ground = (x: number, w: number, y = 0, h = 3, deco: Platform["deco"] = "grass") =>
    P.push({ x, y: y - h, w, h, deco });
  const plat = (x: number, y: number, w: number, oneWay = false, deco: Platform["deco"] = "stone") =>
    P.push({ x, y, w, h: oneWay ? 0.4 : 1, oneWay, deco });
  const wall = (x: number, y: number, h: number, w = 1) => P.push({ x, y, w, h, deco: "stone" });

  // 1 LEDGE (tutorial) x 0..22
  ground(-2, 24);
  wall(-3, -3, 14); // left boundary

  // 2 GAP FIELD x 22..52 (islands over death pit)
  ground(28, 6);
  ground(40, 5);
  ground(48, 8);

  // 3 SPIKE ALLEY + MINECART x 56..88
  ground(56, 34, 0, 3, "stone");
  // spike floor is decorated by strip (damage), cart rides above it

  // 4 TOWER FIELD x 90..126 — climb via one-way platforms
  ground(90, 8);
  wall(101, -3, 9, 3);   // tower A y -3..6
  wall(110, -3, 13, 3);  // tower B y -3..10
  wall(119, -3, 7, 3);   // tower C
  plat(98.5, 2.5, 3, true, "metal");
  plat(103.5, 5, 3, true, "metal");
  plat(107, 8, 3, true, "metal");
  plat(113.5, 11.5, 3, true, "metal");
  plat(119.5, 6.5, 3, true, "metal");
  ground(126, 12, 0, 3);

  // 5 PUZZLE ROOM x 138..160 (door blocks exit)
  ground(138, 24, 0, 3, "stone");
  wall(138, 0, 8);        // room left wall above entrance? no—entrance at floor: raise wall with gap
  P.pop();                // rethink: entrance opening — low ceiling instead
  wall(138, 5, 6);        // decorative upper wall
  wall(159, 0, 2.2);      // door frame base right — door sits at x 154

  // 6 WIND SHAFT x 160..176, climbs y 0..44
  wall(160, 0, 46, 1.5);
  wall(174.5, 0, 46, 1.5);
  ground(160, 16, 0, 3, "stone"); // shaft floor (entered through door)

  // 7 BOSS ARENA y 44 top, x 152..184
  P.push({ x: 152, y: 44, w: 34, h: 1.5, deco: "cloud" }); // arena floor
  wall(151, 44, 10);  // arena left wall
  wall(185, 44, 10);  // arena right wall

  const movers: MovingPlatform[] = [
    // gap field mover (carries across widest gap)
    { x: 34.5, y: 1.2, w: 3, h: 0.5, x0: 34.5, x1: 34.5, y0: 1.2, y1: 1.2, speed: 0, t: 0, dir: 1, ricochet: true },
    // tower field: flashing ricochet gate (times your bounce shots)
    { x: 111, y: 14.5, w: 3.5, h: 0.5, x0: 108, x1: 114, y0: 14.5, y1: 14.5, speed: 1.6, t: 0, dir: 1, ricochet: false, flashing: { interval: 1.4, timer: 0 } },
    // wind shaft moving covers
    { x: 163, y: 14, w: 3.5, h: 0.5, x0: 161.8, x1: 169.5, y0: 14, y1: 14, speed: 2.2, t: 0, dir: 1, ricochet: true },
    { x: 168, y: 26, w: 3.5, h: 0.5, x0: 161.8, x1: 169.5, y0: 26, y1: 26, speed: 2.8, t: 0, dir: -1, ricochet: false, flashing: { interval: 1.1, timer: 0.5 } },
    { x: 164, y: 38, w: 3.5, h: 0.5, x0: 161.8, x1: 169.5, y0: 38, y1: 38, speed: 3.2, t: 0, dir: 1, ricochet: true },
  ];

  const spikeBalls: SpikeBall[] = [
    { x: 106, y: 7.2, r: 0.75 },   // between towers
    { x: 116.6, y: 13.4, r: 0.75 },
    { x: 122.5, y: 9.0, r: 0.75 },
    { x: 167.5, y: 20, r: 0.9 },   // wind shaft
    { x: 165.5, y: 32, r: 0.9 },
  ];

  const spikeStrips: SpikeStrip[] = [
    { x: 60, y: 0, w: 26, h: 0.55 },      // spike alley floor (cart route)
  ];

  const levers: Lever[] = [
    { x: 148.5, y: 0.85, on: false, targets: ["exitdoor"] },
  ];

  const doors: Door[] = [
    { id: "exitdoor", x: 154, y: 0, w: 1.2, h: 4.4, open: false, openAmount: 0 },
  ];

  const boxes: PushBox[] = [
    { x: 141.5, y: 0, w: 1.5, h: 1.5, vx: 0, onPlate: false },
  ];

  const plates: Plate[] = [
    { x: 145.6, y: 0.12, w: 2.2, h: 0.12, pressed: false, targets: ["leverguard"] },
  ];
  // leverguard: a small door covering the lever until plate pressed
  doors.push({ id: "leverguard", x: 147.9, y: 0, w: 1.4, h: 2.2, open: false, openAmount: 0 });

  const winds: WindZone[] = [
    { x: 161.5, y: 0, w: 13, h: 44, force: 30 },
  ];

  const rail: Rail = {
    points: [
      { x: 57, y: 1.4 }, { x: 62, y: 1.4 }, { x: 66, y: 2.6 }, { x: 70, y: 1.6 },
      { x: 75, y: 3.0 }, { x: 80, y: 1.6 }, { x: 84, y: 2.4 }, { x: 88.5, y: 1.4 },
    ],
  };

  const cart: Minecart = { rail, dist: 0, speed: 0, riding: false, started: false, finished: false, x: 57, y: 1.4 };

  const boss: Boss = {
    x: 179, y: 45.4 + 1.1, vx: 0, vy: 0, w: 2.6, h: 2.2,
    hp: 100, maxHp: 100, phase: "waiting", timer: 0,
    attackCooldown: 0, leapCooldown: 4, hitFlash: 0, faceDir: -1, dodgeCooldown: 0,
  };

  const signs: Sign[] = [
    { x: 2.5, y: 1.6, text: "A / D — walk" },
    { x: 7.5, y: 1.6, text: "mouse — trunk aims" },
    { x: 12.5, y: 1.6, text: "click — shoot!" },
    { x: 17.5, y: 1.6, text: "shoot the GROUND to launch ↑" },
    { x: 24, y: 1.6, text: "gaps ahead — launch across!" },
    { x: 57.5, y: 4.2, text: "press E — ride the cart" },
    { x: 92, y: 1.6, text: "shots BOUNCE off green" },
    { x: 139.7, y: 4.6, text: "box → plate → lever → door" },
    { x: 161.7, y: 4.5, text: "the wind carries you — steer!" },
  ];

  const checkpoints: Vec[] = [
    { x: 29, y: 1.5 },     // gap field start
    { x: 51, y: 1.5 },     // before cart
    { x: 92, y: 1.5 },     // tower field
    { x: 139.5, y: 1.5 },  // puzzle room
    { x: 161.7, y: 1.5 },  // wind shaft floor
    { x: 155, y: 46.5 },   // boss arena edge
  ];

  const dodgers: Dodger[] = [
    { x: 121, y: 9.5, homeX: 121, homeY: 9.5, hp: 2, dead: false, phase: 0, dodgeVx: 0, hitFlash: 0 },
    { x: 131, y: 4.5, homeX: 131, homeY: 4.5, hp: 2, dead: false, phase: 2, dodgeVx: 0, hitFlash: 0 },
    { x: 170, y: 33, homeX: 170, homeY: 33, hp: 2, dead: false, phase: 4, dodgeVx: 0, hitFlash: 0 },
  ];

  const rings: GrabRing[] = [
    { x: 96.5, y: 7.5, r: 0.45 },
    { x: 100.8, y: 10, r: 0.45 },
    { x: 128, y: 8.5, r: 0.45 },
  ];

  const springs: Spring[] = [
    { x: 143.2, y: 0, w: 1.8, revealed: false, compress: 0 },
  ];

  const hearts: Heart[] = [
    { x: 144.1, y: 6.4, taken: false },  // spring shelf reward
    { x: 154, y: 46.6, taken: false },   // before boss
    { x: 92.5, y: 1.2, taken: false },   // after cart
  ];
  // heart shelf platform (reach via spring)
  P.push({ x: 142.6, y: 5.2, w: 3, h: 0.4, oneWay: true, deco: "metal" });

  const player: Player = {
    x: 2.5, y: 1.5, w: 0.9, h: 1.3, vx: 0, vy: 0,
    grounded: false, coyote: 0, face: 1,
    hp: 100, maxHp: 100, iframes: 0, shootCooldown: 0,
    aimX: 6, aimY: 2, aimActive: 0,
    dead: false, deathTimer: 0, walkPhase: 0, landedImpact: 0, inCart: false,
  };

  return {
    t: 0, player, bullets: [], particles: [],
    platforms: P, movers, spikeBalls, spikeStrips, levers, doors, boxes, plates, winds,
    cart, boss, signs, dodgers, rings, springs, hearts, tether: null,
    zoom: 1, zoomTarget: 1, paused: false, checkpoints, checkpoint: { x: 2.5, y: 1.5 },
    deathY: -14, camX: player.x, camY: player.y + 1.5, shake: 0,
    rngState: 1337, won: false, wonTimer: 0, bossArenaX: 156,
    stats: { shots: 0, bounces: 0, launches: 0, deaths: 0, damageTaken: 0 },
    toast: { text: "", timer: 0 },
  };
}

// ---------- physics helpers ----------
function overlaps(a: Rect, b: Rect): boolean {
  return a.x < b.x + b.w && a.x + a.w > b.x && a.y < b.y + b.h && a.y + a.h > b.y;
}

function playerRect(p: Player): Rect {
  return { x: p.x - p.w / 2, y: p.y - p.h / 2, w: p.w, h: p.h };
}

interface SolidHit { rect: Rect; mover?: MovingPlatform }

function solids(s: State, forBullet = false): SolidHit[] {
  const out: SolidHit[] = [];
  for (const p of s.platforms) if (!p.oneWay || !forBullet) out.push({ rect: p });
  for (const m of s.movers) out.push({ rect: m, mover: m });
  for (const d of s.doors) if (d.openAmount < 0.95) {
    out.push({ rect: { x: d.x, y: d.y + d.openAmount * d.h, w: d.w, h: d.h * (1 - d.openAmount) } });
  }
  for (const b of s.boxes) out.push({ rect: b });
  return out;
}

// Move an AABB with collision resolution; returns grounded + what we stand on.
function moveBody(
  s: State, body: Rect, vel: Vec, dt: number,
  opts: { oneWays?: boolean } = {},
): { grounded: boolean; hitHead: boolean; hitWall: boolean; standingOn: MovingPlatform | null } {
  const sol = solids(s);
  let grounded = false, hitHead = false, hitWall = false;
  let standingOn: MovingPlatform | null = null;

  // X axis
  body.x += vel.x * dt;
  for (const { rect } of sol) {
    if (isOneWay(s, rect)) continue;
    if (overlaps(body, rect)) {
      if (vel.x > 0) body.x = rect.x - body.w;
      else if (vel.x < 0) body.x = rect.x + rect.w;
      vel.x = 0; hitWall = true;
    }
  }
  // Y axis
  const prevBottom = body.y;
  body.y += vel.y * dt;
  for (const { rect, mover } of sol) {
    if (!overlaps(body, rect)) continue;
    const oneWay = isOneWay(s, rect);
    if (vel.y <= 0) {
      // falling: land only if we were above (always for solids, conditionally for one-ways)
      if (!oneWay || prevBottom >= rect.y + rect.h - 0.02) {
        body.y = rect.y + rect.h;
        vel.y = 0; grounded = true;
        if (mover) standingOn = mover;
      }
    } else if (!oneWay) {
      body.y = rect.y - body.h;
      vel.y = 0; hitHead = true;
    }
  }
  return { grounded, hitHead, hitWall, standingOn };
}

const oneWaySet = new WeakSet<Rect>();
function isOneWay(s: State, r: Rect): boolean {
  // platforms carry their own flag; cache identity for movers/doors (never one-way)
  return (r as Platform).oneWay === true;
}

function railPoint(rail: Rail, dist: number): { p: Vec; done: boolean } {
  let d = dist;
  for (let i = 0; i < rail.points.length - 1; i++) {
    const a = rail.points[i], b = rail.points[i + 1];
    const seg = Math.hypot(b.x - a.x, b.y - a.y);
    if (d <= seg) {
      const t = d / seg;
      return { p: { x: a.x + (b.x - a.x) * t, y: a.y + (b.y - a.y) * t }, done: false };
    }
    d -= seg;
  }
  return { p: { ...rail.points[rail.points.length - 1] }, done: true };
}

function spawnParticles(s: State, x: number, y: number, n: number, kind: Particle["kind"], hue = 40) {
  for (let i = 0; i < n; i++) {
    const a = rng(s) * Math.PI * 2, sp = 2 + rng(s) * 5;
    s.particles.push({ x, y, vx: Math.cos(a) * sp, vy: Math.sin(a) * sp + 2, life: 0.5 + rng(s) * 0.4, maxLife: 0.9, kind, hue });
  }
}

function damagePlayer(s: State, dmg: number, knockX = 0, knockY = 0) {
  const p = s.player;
  if (p.iframes > 0 || p.dead || s.won) return;
  p.hp = Math.max(0, p.hp - dmg);
  p.iframes = 1.2;
  p.vx += knockX; p.vy += knockY;
  s.shake = Math.max(s.shake, 0.5);
  s.stats.damageTaken += dmg;
  spawnParticles(s, p.x, p.y, 10, "hit", 0);
  if (p.hp <= 0) killPlayer(s);
}

function killPlayer(s: State) {
  const p = s.player;
  if (p.dead) return;
  p.dead = true; p.deathTimer = 1.2;
  s.stats.deaths++;
  s.shake = 1;
  spawnParticles(s, p.x, p.y, 24, "hit", 0);
}

function respawn(s: State) {
  const p = s.player;
  p.dead = false; p.hp = p.maxHp;
  p.x = s.checkpoint.x; p.y = s.checkpoint.y;
  p.vx = 0; p.vy = 0; p.iframes = 1.5; p.inCart = false;
  s.cart.riding = false;
  if (s.cart.finished) { /* keep */ } else { s.cart.dist = 0; s.cart.speed = 0; s.cart.started = false; }
}

// ---------- main step ----------
export const DT = 1 / 120;

export function step(s: State, input: Input, dt: number): void {
  if (s.paused) return;
  s.t += dt;
  const p = s.player;

  if (s.won) {
    s.wonTimer += dt;
    if (s.particles.length < 80 && rng(s) < 0.3) {
      s.particles.push({
        x: s.camX + (rng(s) - 0.5) * 20, y: s.camY + 8, vx: (rng(s) - 0.5) * 2, vy: -2 - rng(s) * 2,
        life: 2, maxLife: 2, kind: "confetti", hue: rng(s) * 360,
      });
    }
  }

  // toast decay
  if (s.toast.timer > 0) s.toast.timer -= dt;

  // ---- death / respawn ----
  if (p.dead) {
    p.deathTimer -= dt;
    if (p.deathTimer <= 0) respawn(s);
    updateParticles(s, dt);
    updateCamera(s, dt);
    return;
  }

  // ---- movers ----
  for (const m of s.movers) {
    if (m.flashing) {
      m.flashing.timer += dt;
      if (m.flashing.timer >= m.flashing.interval) { m.flashing.timer = 0; m.ricochet = !m.ricochet; }
    }
    if (m.speed > 0) {
      const span = Math.hypot(m.x1 - m.x0, m.y1 - m.y0);
      if (span > 0.001) {
        m.t += (m.speed / span) * m.dir * dt;
        if (m.t > 1) { m.t = 1; m.dir = -1; }
        if (m.t < 0) { m.t = 0; m.dir = 1; }
        m.x = m.x0 + (m.x1 - m.x0) * m.t;
        m.y = m.y0 + (m.y1 - m.y0) * m.t;
      }
    }
  }

  // ---- doors ----
  for (const d of s.doors) {
    const target = d.open ? 1 : 0;
    d.openAmount += Math.sign(target - d.openAmount) * dt * 1.6;
    d.openAmount = Math.max(0, Math.min(1, d.openAmount));
  }

  // ---- plates ----
  for (const pl of s.plates) {
    const was = pl.pressed;
    pl.pressed = s.boxes.some(b => overlaps(pl, { x: b.x, y: b.y - 0.1, w: b.w, h: b.h })) ||
                 overlaps(pl, playerRect(p));
    if (pl.pressed !== was) {
      for (const id of pl.targets) {
        const d = s.doors.find(dd => dd.id === id);
        if (d) d.open = pl.pressed;
      }
      if (pl.pressed) { s.shake = Math.max(s.shake, 0.15); spawnParticles(s, pl.x + pl.w / 2, pl.y + 0.2, 6, "puff", 100); }
    }
  }

  // ---- boxes (slide when shot, friction) ----
  for (const b of s.boxes) {
    b.vx *= Math.pow(0.02, dt); // strong friction
    if (Math.abs(b.vx) > 0.05) {
      const vel = { x: b.vx, y: 0 };
      moveBody(s, b, vel, dt);
      b.vx = vel.x;
    }
    // gravity-lite: keep on floor (boxes live on flat ground in this level)
  }

  // ---- minecart ----
  const cart = s.cart;
  if (!cart.finished) {
    const cp = railPoint(cart.rail, cart.dist);
    cart.x = cp.p.x; cart.y = cp.p.y;
    if (cart.riding) {
      cart.speed = Math.min(cart.speed + 8 * dt, 9);
      cart.dist += cart.speed * dt;
      const np = railPoint(cart.rail, cart.dist);
      cart.x = np.p.x; cart.y = np.p.y;
      p.x = cart.x; p.y = cart.y + 0.95;
      p.vx = 0; p.vy = 0;
      if (np.done) {
        cart.finished = true; cart.riding = false; p.inCart = false;
        p.vx = 7; p.vy = 6; // launched off the end
        s.toast = { text: "weeee!", timer: 1.5 };
      }
    } else if (input.interact && !cart.started) {
      const d = Math.hypot(p.x - cart.x, p.y - cart.y);
      if (d < 2.2) {
        cart.riding = true; cart.started = true; p.inCart = true;
        s.toast = { text: "hold on!", timer: 1.2 };
      }
    }
  }

  // ---- player movement (skip while carted) ----
  if (!cart.riding) {
    const accel = p.grounded ? 60 : 38;
    const maxRun = 7;
    let ax = 0;
    if (input.left) ax -= 1;
    if (input.right) ax += 1;
    if (ax !== 0) {
      p.vx += ax * accel * dt;
      if (Math.abs(p.vx) > maxRun && Math.sign(p.vx) === ax) p.vx = ax * maxRun;
    } else {
      const fr = p.grounded ? 40 : 4;
      const dv = fr * dt;
      if (Math.abs(p.vx) <= dv) p.vx = 0; else p.vx -= Math.sign(p.vx) * dv;
    }

    // wind
    for (const w of s.winds) {
      if (overlaps(playerRect(p), w)) {
        p.vy += w.force * dt;
        p.vy = Math.min(p.vy, 7.5); // terminal updraft
        if (rng(s) < 0.25) s.particles.push({ x: p.x + (rng(s) - 0.5) * 2, y: p.y - 1, vx: 0, vy: 6, life: 0.4, maxLife: 0.4, kind: "puff", hue: 190 });
      }
    }

    // gravity
    p.vy -= 24 * dt;
    p.vy = Math.max(p.vy, -22);

    const body = playerRect(p);
    const vel = { x: p.vx, y: p.vy };
    const wasGrounded = p.grounded;
    const res = moveBody(s, body, vel, dt);
    p.x = body.x + p.w / 2; p.y = body.y + p.h / 2;
    p.vx = vel.x; p.vy = vel.y;
    p.grounded = res.grounded;
    if (res.standingOn && res.standingOn.speed > 0) {
      // carry with moving platform
      const m = res.standingOn;
      const mvx = (m.x1 - m.x0) * m.dir * (m.speed / Math.max(0.001, Math.hypot(m.x1 - m.x0, m.y1 - m.y0)));
      p.x += mvx * dt;
    }
    if (p.grounded) p.coyote = 0.1; else p.coyote = Math.max(0, p.coyote - dt);
    if (!wasGrounded && p.grounded && velDown(p)) p.landedImpact = Math.min(1, Math.abs(p.vy) / 15);
    if (!wasGrounded && p.grounded) p.landedImpact = 0.5;

    // facing = velocity, with input fallback
    if (Math.abs(p.vx) > 0.3) p.face = p.vx > 0 ? 1 : -1;
    else if (input.left) p.face = -1;
    else if (input.right) p.face = 1;

    if (Math.abs(p.vx) > 0.5 && p.grounded) p.walkPhase += dt * Math.abs(p.vx) * 1.6;
  }

  // ---- trunk tether (TrunkClimb spirit): hold E near a ring to anchor & swing ----
  if (!cart.riding) {
    if (input.interactHeld) {
      if (!s.tether) {
        let best = -1, bestD = 2.6;
        for (let i = 0; i < s.rings.length; i++) {
          const d = Math.hypot(s.rings[i].x - p.x, s.rings[i].y - p.y);
          if (d < bestD) { bestD = d; best = i; }
        }
        if (best >= 0) s.tether = { ringIndex: best, len: Math.max(1.2, Math.min(bestD, 3.4)) };
      }
      if (s.tether) {
        const ring = s.rings[s.tether.ringIndex];
        const dx = p.x - ring.x, dy = p.y - ring.y;
        const d = Math.hypot(dx, dy);
        if (d > s.tether.len) {
          const nx = dx / d, ny = dy / d;
          p.x = ring.x + nx * s.tether.len;
          p.y = ring.y + ny * s.tether.len;
          const vr = p.vx * nx + p.vy * ny; // radial component
          if (vr > 0) { p.vx -= vr * nx; p.vy -= vr * ny; }
        }
      }
    } else {
      s.tether = null;
    }
  }

  // ---- aim ----
  p.aimX = input.aimX; p.aimY = input.aimY;
  if (input.shootHeld || input.shoot) p.aimActive = 0.6;
  else p.aimActive = Math.max(0, p.aimActive - dt);

  // ---- shooting ----
  p.shootCooldown = Math.max(0, p.shootCooldown - dt);
  if (input.shoot && p.shootCooldown <= 0 && !p.dead && !s.won) {
    const dx = input.aimX - p.x, dy = input.aimY - (p.y + 0.3);
    const len = Math.hypot(dx, dy) || 1;
    s.bullets.push({
      x: p.x + (dx / len) * 0.9, y: p.y + 0.3 + (dy / len) * 0.9,
      dx: dx / len, dy: dy / len, speed: 26, bounces: 0, dead: false, age: 0,
    });
    p.shootCooldown = 0.28;
    s.stats.shots++;
    // small shoulder kick
    p.vx -= (dx / len) * 0.8;
    if (!p.grounded) p.vy -= (dy / len) * 0.5;
    s.shake = Math.max(s.shake, 0.12);
  }

  // ---- bullets ----
  for (const b of s.bullets) {
    if (b.dead) continue;
    b.age += dt;
    if (b.age > 6) { b.dead = true; continue; }
    const steps = 3; // substeps for fast bullets
    for (let i = 0; i < steps && !b.dead; i++) {
      const sub = dt / steps;
      b.x += b.dx * b.speed * sub;
      b.y += b.dy * b.speed * sub;
      const br: Rect = { x: b.x - 0.14, y: b.y - 0.14, w: 0.28, h: 0.28 };

      // boss hit
      const boss = s.boss;
      if (boss.phase !== "waiting" && boss.phase !== "dead" &&
          overlaps(br, { x: boss.x - boss.w / 2, y: boss.y - boss.h / 2, w: boss.w, h: boss.h })) {
        // dodge chance (the DodgeBullets spirit): boss sidesteps if off cooldown
        if (boss.dodgeCooldown <= 0 && rng(s) < 0.4 && boss.phase === "chase") {
          boss.dodgeCooldown = 1.2;
          boss.vx = (b.dx > 0 ? 1 : -1) * 9; // dash same direction bullet travels (out of its path)
          s.toast = { text: "he dodged!", timer: 0.8 };
        } else {
          boss.hp = Math.max(0, boss.hp - 8);
          boss.hitFlash = 0.15;
          b.dead = true;
          s.shake = Math.max(s.shake, 0.2);
          spawnParticles(s, b.x, b.y, 8, "spark", 20);
          if (boss.hp <= 0 && boss.phase !== "dead") {
            boss.phase = "dead"; boss.timer = 0;
            s.won = true;
            s.toast = { text: "BOSS DOWN!", timer: 4 };
          }
        }
        continue;
      }

      // lever hit (shooting a lever toggles it)
      for (const lv of s.levers) {
        const guard = s.doors.find(d => d.id === "leverguard");
        const guarded = guard ? guard.openAmount < 0.9 : false;
        if (!guarded && Math.hypot(lv.x - b.x, lv.y - b.y) < 0.7) {
          toggleLever(s, lv);
          b.dead = true;
        }
      }
      if (b.dead) break;

      // box hit → slide
      for (const box of s.boxes) {
        if (overlaps(br, box)) {
          box.vx += b.dx * 7;
          b.dead = true;
          spawnParticles(s, b.x, b.y, 6, "puff", 30);
        }
      }
      if (b.dead) break;

      // world bounce
      for (const { rect, mover } of solids(s, true)) {
        if (!overlaps(br, rect)) continue;
        if (mover && !mover.ricochet) { b.dead = true; spawnParticles(s, b.x, b.y, 5, "puff", 0); break; }
        // reflect: find smallest penetration axis
        const penL = b.x + 0.14 - rect.x;
        const penR = rect.x + rect.w - (b.x - 0.14);
        const penB = b.y + 0.14 - rect.y;
        const penT = rect.y + rect.h - (b.y - 0.14);
        const minPen = Math.min(penL, penR, penB, penT);
        let nx = 0, ny = 0;
        if (minPen === penL) { nx = -1; b.x = rect.x - 0.15; }
        else if (minPen === penR) { nx = 1; b.x = rect.x + rect.w + 0.15; }
        else if (minPen === penB) { ny = -1; b.y = rect.y - 0.15; }
        else { ny = 1; b.y = rect.y + rect.h + 0.15; }
        // reflect dir
        const dot = b.dx * nx + b.dy * ny;
        b.dx -= 2 * dot * nx; b.dy -= 2 * dot * ny;
        b.speed *= 0.8;
        b.bounces++;
        s.stats.bounces++;
        spawnParticles(s, b.x, b.y, 6, "spark", 45);

        // RECOIL: impulse player away from surface if bounce is near
        const pd = Math.hypot(p.x - b.x, p.y - b.y);
        if (pd < 6 && !cart.riding) {
          const falloff = 1 - pd / 6;
          let ix = nx, iy = ny;
          if (ny > 0.5) iy += 1; // ground bounces boost extra up (the original's trick)
          const il = Math.hypot(ix, iy) || 1;
          const force = b.speed * 0.55 * falloff;
          p.vx += (ix / il) * force;
          p.vy += (iy / il) * force;
          s.stats.launches++;
          s.shake = Math.max(s.shake, 0.25);
          p.grounded = false;
        }
        if (b.bounces >= 5) b.dead = true;
        break;
      }
    }
    // spikeball kills bullets
    for (const sb of s.spikeBalls) {
      if (!b.dead && Math.hypot(sb.x - b.x, sb.y - b.y) < sb.r + 0.15) { b.dead = true; spawnParticles(s, b.x, b.y, 5, "puff", 0); }
    }
  }
  s.bullets = s.bullets.filter(b => !b.dead);

  // ---- trunk lever flick (touch) ----
  for (const lv of s.levers) {
    const guard = s.doors.find(d => d.id === "leverguard");
    const guarded = guard ? guard.openAmount < 0.9 : false;
    if (!guarded && input.interact && Math.hypot(lv.x - p.x, lv.y - p.y) < 1.6) toggleLever(s, lv);
  }

  // ---- hazards ----
  const pr = playerRect(p);
  for (const sb of s.spikeBalls) {
    const cx = Math.max(pr.x, Math.min(sb.x, pr.x + pr.w));
    const cy = Math.max(pr.y, Math.min(sb.y, pr.y + pr.h));
    if (Math.hypot(sb.x - cx, sb.y - cy) < sb.r) {
      damagePlayer(s, 20, (p.x - sb.x) * 4, 6);
    }
  }
  for (const st of s.spikeStrips) {
    if (overlaps(pr, st) && !cart.riding) damagePlayer(s, 20, 0, 9);
  }

  // ---- springs (BoxSnapZone spirit: puzzle reveals a bouncer) ----
  for (const sp of s.springs) {
    if (!sp.revealed && s.plates.some(pl => pl.pressed)) {
      sp.revealed = true;
      s.toast = { text: "a spring appears!", timer: 1.6 };
      spawnParticles(s, sp.x + sp.w / 2, sp.y + 0.5, 12, "confetti", 300);
    }
    sp.compress = Math.max(0, sp.compress - dt * 4);
    if (sp.revealed && !p.dead) {
      const springRect = { x: sp.x, y: sp.y, w: sp.w, h: 0.5 };
      if (overlaps(playerRect(p), springRect) && p.vy <= 0) {
        p.vy = 16;
        p.grounded = false;
        sp.compress = 1;
        s.shake = Math.max(s.shake, 0.2);
        spawnParticles(s, p.x, sp.y + 0.4, 8, "puff", 180);
      }
    }
  }

  // ---- hearts ----
  for (const h of s.hearts) {
    if (!h.taken && Math.hypot(h.x - p.x, h.y - p.y) < 1) {
      h.taken = true;
      p.hp = Math.min(p.maxHp, p.hp + 25);
      s.toast = { text: "+25 hp", timer: 1.2 };
      spawnParticles(s, h.x, h.y, 10, "confetti", 340);
    }
  }

  // ---- dodger toads (DodgeBullets spirit) ----
  for (const dg of s.dodgers) {
    if (dg.dead) continue;
    dg.hitFlash = Math.max(0, dg.hitFlash - dt);
    dg.phase += dt;
    // hover bob
    const bobY = dg.homeY + Math.sin(dg.phase * 2) * 0.5;
    // dodge: sense bullets within radius, dash perpendicular-ish
    let threat: Bullet | null = null;
    for (const b of s.bullets) {
      if (Math.hypot(b.x - dg.x, b.y - dg.y) < 4.5) { threat = b; break; }
    }
    if (threat && Math.abs(dg.dodgeVx) < 0.5) {
      dg.dodgeVx = (threat.dy !== 0 ? Math.sign(threat.dx || 1) : 1) * 10 * (rng(s) < 0.5 ? 1 : -1);
    }
    dg.dodgeVx *= Math.pow(0.03, dt);
    dg.x += dg.dodgeVx * dt;
    dg.x = Math.max(dg.homeX - 3.2, Math.min(dg.homeX + 3.2, dg.x));
    dg.y = bobY;
    // bullet hits
    for (const b of s.bullets) {
      if (!b.dead && Math.hypot(b.x - dg.x, b.y - dg.y) < 0.75) {
        b.dead = true;
        dg.hp--;
        dg.hitFlash = 0.15;
        spawnParticles(s, dg.x, dg.y, 8, "spark", 130);
        if (dg.hp <= 0) {
          dg.dead = true;
          s.toast = { text: "popped!", timer: 0.9 };
          spawnParticles(s, dg.x, dg.y, 16, "confetti", 130);
        }
      }
    }
    // contact damage
    if (Math.hypot(dg.x - p.x, dg.y - p.y) < 1.1) damagePlayer(s, 10, Math.sign(p.x - dg.x) * 7, 5);
  }

  // ---- boss ----
  updateBoss(s, input, dt);

  // ---- checkpoints ----
  for (const c of s.checkpoints) {
    if (Math.abs(p.x - c.x) < 1.5 && Math.abs(p.y - c.y) < 2.5 &&
        (s.checkpoint.x !== c.x || s.checkpoint.y !== c.y)) {
      s.checkpoint = { ...c };
      s.toast = { text: "checkpoint!", timer: 1.2 };
      spawnParticles(s, c.x, c.y + 1, 8, "confetti", 140);
    }
  }

  // ---- death pit ----
  if (p.y < s.deathY) killPlayer(s);

  // ---- timers ----
  p.iframes = Math.max(0, p.iframes - dt);
  p.landedImpact = Math.max(0, p.landedImpact - dt * 3);
  s.shake = Math.max(0, s.shake - dt * 2.5);

  updateParticles(s, dt);
  updateCamera(s, dt);
}

function velDown(p: Player): boolean { return p.vy < -8; }

function toggleLever(s: State, lv: Lever) {
  lv.on = !lv.on;
  for (const id of lv.targets) {
    const d = s.doors.find(dd => dd.id === id);
    if (d) d.open = lv.on;
  }
  s.toast = { text: lv.on ? "click! door open" : "door closed", timer: 1.2 };
  s.shake = Math.max(s.shake, 0.15);
  spawnParticles(s, lv.x, lv.y + 0.5, 8, "spark", 120);
}

function updateBoss(s: State, input: Input, dt: number) {
  const boss = s.boss, p = s.player;
  boss.hitFlash = Math.max(0, boss.hitFlash - dt);
  boss.dodgeCooldown = Math.max(0, boss.dodgeCooldown - dt);

  switch (boss.phase) {
    case "waiting":
      if (p.x > s.bossArenaX && p.y > 43) {
        boss.phase = "intro"; boss.timer = 0;
        s.zoomTarget = 0.72;
        boss.vy = 13; boss.vx = -6;
        s.toast = { text: "!!!", timer: 1.5 };
        s.shake = 0.4;
      }
      return;
    case "intro":
      boss.timer += dt;
      boss.vy -= 24 * dt;
      boss.x += boss.vx * dt; boss.y += boss.vy * dt;
      if (boss.y <= 45.4 + boss.h / 2 && boss.vy < 0) {
        boss.y = 45.4 + boss.h / 2; boss.vy = 0; boss.vx = 0;
        s.shake = 0.8;
        s.zoomTarget = 1;
        spawnParticles(s, boss.x, boss.y - 1, 16, "puff", 20);
        if (boss.timer > 1.2) { boss.phase = "chase"; boss.timer = 0; }
      }
      if (boss.timer > 2) { boss.phase = "chase"; boss.timer = 0; }
      return;
    case "chase": {
      boss.faceDir = p.x < boss.x ? -1 : 1;
      const dist = Math.abs(p.x - boss.x);
      boss.leapCooldown -= dt;
      boss.attackCooldown -= dt;
      // friction after dodge dash
      boss.vx *= Math.pow(0.05, dt);
      const chaseV = 3.4 * boss.faceDir;
      boss.x += (chaseV + boss.vx) * dt;
      // gravity/floor
      boss.y = Math.max(45.4 + boss.h / 2, boss.y - 10 * dt);
      if (boss.leapCooldown <= 0 && dist > 3) {
        boss.phase = "leap"; boss.timer = 0;
        boss.vy = 11;
        boss.vx = Math.sign(p.x - boss.x) * Math.min(9, dist * 1.4);
        boss.leapCooldown = 5;
        return;
      }
      if (dist < 2.2 && boss.attackCooldown <= 0) {
        boss.phase = "windup"; boss.timer = 0;
        return;
      }
      contactDamage(s, dt);
      return;
    }
    case "windup":
      boss.timer += dt;
      boss.faceDir = p.x < boss.x ? -1 : 1;
      if (boss.timer > 0.35) {
        // punch!
        const dist = Math.abs(p.x - boss.x);
        const inFront = Math.sign(p.x - boss.x) === boss.faceDir;
        if (dist < 2.8 && inFront && Math.abs(p.y - boss.y) < 2)
          damagePlayer(s, 15, boss.faceDir * 10, 7);
        boss.attackCooldown = 1.6;
        boss.phase = "chase";
      }
      return;
    case "leap":
      boss.timer += dt;
      boss.vy -= 24 * dt;
      boss.x += boss.vx * dt; boss.y += boss.vy * dt;
      if (boss.y <= 45.4 + boss.h / 2 && boss.vy < 0) {
        boss.y = 45.4 + boss.h / 2;
        boss.vy = 0; boss.vx = 0;
        s.shake = 0.6;
        spawnParticles(s, boss.x, boss.y - 1, 12, "puff", 20);
        // shockwave damage if player grounded & near
        if (p.grounded && Math.abs(p.x - boss.x) < 3.5) damagePlayer(s, 10, Math.sign(p.x - boss.x) * 8, 8);
        boss.phase = "chase";
      }
      // clamp to arena
      boss.x = Math.max(153.5, Math.min(183.5, boss.x));
      return;
    case "dead":
      boss.timer += dt;
      boss.y = Math.max(45.4 + boss.h / 2 - 0.6, boss.y - dt * 0.5); // slumps
      return;
  }
}

function contactDamage(s: State, dt: number) {
  const boss = s.boss, p = s.player;
  const br = { x: boss.x - boss.w / 2, y: boss.y - boss.h / 2, w: boss.w, h: boss.h };
  if (overlaps(playerRect(p), br)) damagePlayer(s, 10, Math.sign(p.x - boss.x) * 9, 6);
}

function updateParticles(s: State, dt: number) {
  for (const pt of s.particles) {
    pt.life -= dt;
    pt.x += pt.vx * dt; pt.y += pt.vy * dt;
    if (pt.kind !== "confetti") pt.vy -= 10 * dt;
    else pt.vy -= 1 * dt;
  }
  s.particles = s.particles.filter(pt => pt.life > 0);
}

function updateCamera(s: State, dt: number) {
  const p = s.player;
  s.zoom += (s.zoomTarget - s.zoom) * (1 - Math.pow(0.01, dt));
  const tx = s.cart.riding ? s.cart.x : p.x + p.face * 2;
  const ty = (s.cart.riding ? s.cart.y : p.y) + 2;
  const k = 1 - Math.pow(0.001, dt); // smooth
  s.camX += (tx - s.camX) * k;
  s.camY += (ty - s.camY) * k;
}
