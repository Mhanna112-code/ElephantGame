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
  oneWay?: boolean; // player passes from below, lands on top (bullets still interact)
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

export interface WindZone extends Rect { force: number; onTime: number; offTime: number; timer: number; active: boolean }

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
  hostile?: boolean; // boss peanuts: hurt the player, bounce like yours
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
  dodgeTelegraph: number;
  spitCooldown: number;
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
export interface Peanut { x: number; y: number; taken: boolean }

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
  ammo: number;
  maxAmmo: number;
  dryFire: number; // toast cooldown for empty-clip feedback
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
  peanuts: Peanut[];
  tether: { ringIndex: number; len: number } | null;
  zoom: number; zoomTarget: number;
  paused: boolean;
  checkpoints: Vec[];
  checkpoint: Vec;
  deathY: number;       // below this = death
  camX: number; camY: number;
  shake: number;
  hitStop: number;
  rngState: number;
  won: boolean;
  wonTimer: number;
  bossArenaX: number;   // boss activates when player passes this
  stats: { shots: number; bounces: number; launches: number; deaths: number; damageTaken: number; peanuts: number };
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

  // 2 GAP FIELD x 22..52 (islands over a death pit; gaps ramp 3u -> 4u -> 3.5u)
  ground(25, 6);
  ground(35, 4.5);
  ground(43, 13);

  // 3 SPIKE ALLEY + MINECART x 56..88
  ground(56, 34, 0, 3, "stone");
  // spike floor is decorated by strip (damage), cart rides above it

  // 4 TOWER FIELD x 90..126 — climb via one-way platforms
  ground(90, 11);        // runs under the ladder to tower A: missed launches retry, not die
  ground(101, 25, 0, 3, "stone"); // tower-field floor: falls are recoverable, canyons escape via chains
  wall(101, -3, 9, 3);   // tower A y -3..6
  wall(110, -3, 13, 3);  // tower B y -3..10
  wall(119, -3, 7, 3);   // tower C
  plat(95.5, 2.4, 3, true, "metal");   // step 1: launch straight up from the ground
  plat(97.5, 4.6, 3.5, true, "metal"); // step 2: single clean launch from step 1; chains are speed-tech, not a wall
  plat(105.5, 7.8, 3, true, "metal");  // step 3: comfortable launch from tower A top
  plat(114.5, 11.2, 3, true, "metal"); // bridge toward the drop to tower C
  plat(116.6, 2.1, 2, true, "metal");  // canyon exit ledge: fall recovery is a two-hop staircase
  ground(126, 12, 0, 3);

  // 5 PUZZLE ROOM x 138..160 (door blocks exit)
  ground(138, 24, 0, 3, "stone");
  wall(138, 0, 8);        // room left wall above entrance? no—entrance at floor: raise wall with gap
  P.pop();                // rethink: entrance opening — low ceiling instead
  wall(138, 5, 6);        // decorative upper wall
  // 6 WIND SHAFT x 160..176, climbs y 0..44 — entered at ground level through the doorway
  wall(160, 3.4, 42.6, 1.5); // left shaft wall floats above a walk-in entrance
  wall(174.5, 0, 46, 1.5);
  ground(160, 16, 0, 3, "stone"); // shaft floor (entered through door)

  // 7 BOSS ARENA y 44 top, x 152..184
  P.push({ x: 152, y: 44, w: 14, h: 1.5, deco: "cloud" }); // arena floor (left of shaft exit)
  P.push({ x: 171, y: 44, w: 15, h: 1.5, deco: "cloud" }); // arena floor (right of shaft exit)
  P.push({ x: 166, y: 45.1, w: 5, h: 0.4, oneWay: true, deco: "cloud" }); // shaft exit: flush with the arena floors
  wall(151, 44, 10);  // arena left wall
  wall(185, 44, 10);  // arena right wall

  const movers: MovingPlatform[] = [
    // gap field mover (carries across widest gap)
    { x: 31.6, y: 0.5, w: 2.4, h: 0.5, x0: 31.6, x1: 31.6, y0: 0.5, y1: 0.5, speed: 0, t: 0, dir: 1, ricochet: true },
    // tower field: flashing ricochet gate (times your bounce shots)
    { x: 111, y: 16.2, w: 3.5, h: 0.5, x0: 108, x1: 114, y0: 16.2, y1: 16.2, speed: 1.6, t: 0, dir: 1, ricochet: false, flashing: { interval: 1.4, timer: 0 } },
    // wind shaft moving covers
    { x: 163, y: 14, w: 5, h: 0.5, x0: 161.8, x1: 169.5, y0: 14, y1: 14, speed: 2.2, t: 0, dir: 1, ricochet: true, oneWay: true },
    { x: 168, y: 26, w: 5, h: 0.5, x0: 161.8, x1: 169.5, y0: 26, y1: 26, speed: 2.8, t: 0, dir: -1, ricochet: false, flashing: { interval: 1.1, timer: 0.5 }, oneWay: true },
    { x: 164, y: 38, w: 5, h: 0.5, x0: 161.8, x1: 169.5, y0: 38, y1: 38, speed: 3.2, t: 0, dir: 1, ricochet: true, oneWay: true },
    // tower field: second gate, offset phase (develop: plan a two-bounce)
    { x: 116, y: 10.5, w: 3, h: 0.5, x0: 116, x1: 121, y0: 10.5, y1: 10.5, speed: 1.2, t: 0.5, dir: -1, ricochet: true, flashing: { interval: 1.4, timer: 0.7 } },
    // boss arena mirrors (conclude: bank shots behind the boss beat the dodge)
    { x: 154.5, y: 47.5, w: 0.6, h: 3.2, x0: 154.5, x1: 154.5, y0: 47.5, y1: 47.5, speed: 0, t: 0, dir: 1, ricochet: true },
    { x: 183, y: 47.5, w: 0.6, h: 3.2, x0: 183, x1: 183, y0: 47.5, y1: 47.5, speed: 0, t: 0, dir: 1, ricochet: false, flashing: { interval: 1.6, timer: 0 } },
  ];

  const spikeBalls: SpikeBall[] = [
    { x: 109.6, y: 6.2, r: 0.75 },  // guards the gap right of step 3 (overshoot punish)
    { x: 118.9, y: 15.2, r: 0.75 }, // high above the bridge corner: only wild chains reach it
    { x: 124.8, y: 7.2, r: 0.75 },  // right of tower C: overshooting the drop stings
    { x: 164.2, y: 20, r: 0.9 },   // wind shaft (left)
    { x: 171.2, y: 32, r: 0.9 },   // wind shaft (right)
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
    { x: 161.5, y: 0, w: 13, h: 44, force: 34, onTime: 2.6, offTime: 0.8, timer: 0, active: true },
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
    hp: 160, maxHp: 160, phase: "waiting", timer: 0,
    attackCooldown: 0, leapCooldown: 4, hitFlash: 0, faceDir: -1, dodgeCooldown: 0, dodgeTelegraph: 0, spitCooldown: 2,
  };

  const signs: Sign[] = [
    { x: 2.5, y: 1.6, text: "A / D — walk" },
    { x: 7.5, y: 1.6, text: "mouse — trunk aims" },
    { x: 12.5, y: 1.6, text: "click — shoot!" },
    { x: 17.5, y: 1.6, text: "shoot the GROUND to launch ↑" },
    { x: 24, y: 1.6, text: "gaps ahead — launch across!" },
    { x: 57.5, y: 4.2, text: "press E — ride the cart" },
    { x: 92, y: 1.6, text: "shots BOUNCE off green" },
    { x: 96.5, y: 1.6, text: "chain shots mid-air to climb higher!" },
    { x: 93.8, y: 3.2, text: "hold E by a ring — swing (grabs reload!)" },
    { x: 139.7, y: 4.6, text: "box → plate → lever → door" },
    { x: 161.7, y: 4.5, text: "the wind carries you — steer!" },
  ];

  const checkpoints: Vec[] = [
    { x: 27.5, y: 1.5 },   // gap field start
    { x: 51, y: 1.5 },     // before cart
    { x: 92, y: 1.5 },     // tower field
    { x: 139.5, y: 1.5 },  // puzzle room
    { x: 161.7, y: 1.5 },  // wind shaft floor
    { x: 168.5, y: 46.2 },  // boss arena: on the shaft exit platform
  ];

  const dodgers: Dodger[] = [
    { x: 121, y: 9.5, homeX: 121, homeY: 9.5, hp: 2, dead: false, phase: 0, dodgeVx: 0, hitFlash: 0 },
    { x: 68, y: 5.5, homeX: 68, homeY: 5.5, hp: 2, dead: false, phase: 1, dodgeVx: 0, hitFlash: 0 },
    { x: 78, y: 6, homeX: 78, homeY: 6, hp: 2, dead: false, phase: 3, dodgeVx: 0, hitFlash: 0 },
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

  const peanuts: Peanut[] = [
    { x: 24, y: 3.4, taken: false },     // above gap 1: launch through it
    { x: 32.8, y: 2.6, taken: false },   // over the helper mover
    { x: 39, y: 3.8, taken: false },     // gap C apex
    { x: 66, y: 3.6, taken: false },     // cart route
    { x: 75, y: 4.4, taken: false },     // cart route high
    { x: 96.5, y: 8.2, taken: false },   // above ring 1 (swing up)
    { x: 100.8, y: 10.8, taken: false }, // above ring 2
    { x: 112, y: 13.2, taken: false },   // above tower B (chain)
    { x: 116, y: 12.6, taken: false },   // bridge walk
    { x: 128, y: 9.4, taken: false },    // above ring 3
    { x: 144.1, y: 8.2, taken: false },  // above the spring shelf
    { x: 168.5, y: 41, taken: false },   // just under the shaft exit
  ];

  const hearts: Heart[] = [
    { x: 144.1, y: 6.4, taken: false },  // spring shelf reward
    { x: 168.5, y: 46.6, taken: false }, // on the landing path out of the shaft
    { x: 92.5, y: 1.2, taken: false },   // after cart
    { x: 72, y: 4.2, taken: false },     // over the rail — shoot nothing, just lean and grab
    { x: 172.6, y: 27.5, taken: false }, // wind shaft, safe side opposite the ball
  ];
  // heart shelf platform (reach via spring)
  P.push({ x: 142.6, y: 5.2, w: 3, h: 0.4, oneWay: true, deco: "metal" });

  const player: Player = {
    x: 2.5, y: 1.5, w: 0.9, h: 1.3, vx: 0, vy: 0,
    grounded: false, coyote: 0, face: 1,
    hp: 100, maxHp: 100, iframes: 0, shootCooldown: 0,
    aimX: 6, aimY: 2, aimActive: 0,
    dead: false, deathTimer: 0, walkPhase: 0, landedImpact: 0, inCart: false,
    ammo: 3, maxAmmo: 3, dryFire: 0,
  };

  return {
    t: 0, player, bullets: [], particles: [],
    platforms: P, movers, spikeBalls, spikeStrips, levers, doors, boxes, plates, winds,
    cart, boss, signs, dodgers, rings, springs, hearts, peanuts, tether: null,
    zoom: 1, zoomTarget: 1, paused: false, checkpoints, checkpoint: { x: 2.5, y: 1.5 },
    deathY: -14, camX: player.x, camY: player.y + 1.5, shake: 0, hitStop: 0,
    rngState: 1337, won: false, wonTimer: 0, bossArenaX: 168.2,
    stats: { shots: 0, bounces: 0, launches: 0, deaths: 0, damageTaken: 0, peanuts: 0 },
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
  for (const p of s.platforms) out.push({ rect: p }); // one-ways included; callers apply direction rules
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
  exclude?: Rect,
): { grounded: boolean; hitHead: boolean; hitWall: boolean; standingOn: MovingPlatform | null } {
  const sol = solids(s).filter(h => h.rect !== exclude && !(h.rect === (exclude as unknown)));
  let grounded = false, hitHead = false, hitWall = false;
  let standingOn: MovingPlatform | null = null;

  // X axis
  body.x += vel.x * dt;
  for (const { rect } of sol) {
    if (isOneWay(s, rect)) continue;
    if (overlaps(body, rect)) {
      // LEDGE FORGIVENESS: falling into a wall whose top edge is barely above the
      // feet pops the body up onto the ledge instead of zeroing vx into the face.
      const ledgeTop = rect.y + rect.h;
      if (vel.y <= 0.5 && ledgeTop - body.y <= 0.5 && ledgeTop - body.y > 0) {
        body.y = ledgeTop;
        continue;
      }
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
      if (!oneWay || prevBottom >= rect.y + rect.h - 0.15) {
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
  return (r as Platform).oneWay === true; // platforms AND movers may carry the flag
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
  s.hitStop = Math.max(s.hitStop, 0.07);
  p.vx += knockX; p.vy += knockY;
  s.shake = Math.max(s.shake, 0.5);
  s.stats.damageTaken += dmg;
  spawnParticles(s, p.x, p.y, 10, "hit", 0);
  if (p.hp <= 0) killPlayer(s);
}

function killPlayer(s: State) {
  const p = s.player;
  if (p.dead) return;
  p.dead = true; p.deathTimer = 0.6;
  s.stats.deaths++;
  s.shake = 1;
  spawnParticles(s, p.x, p.y, 24, "hit", 0);
}

function respawn(s: State) {
  const p = s.player;
  p.dead = false; p.hp = p.maxHp;
  s.tether = null;
  p.x = s.checkpoint.x; p.y = s.checkpoint.y;
  p.vx = 0; p.vy = 0; p.iframes = 1.5; p.inCart = false;
  s.cart.riding = false;
  if (s.cart.finished) { /* keep */ } else { s.cart.dist = 0; s.cart.speed = 0; s.cart.started = false; }
}

// ---------- main step ----------
export const DT = 1 / 120;

export function step(s: State, input: Input, dt: number): void {
  if (s.paused) return;
  if (s.hitStop > 0) { s.hitStop -= dt; return; }
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
      moveBody(s, b, vel, dt, b);
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
      if (rng(s) < 0.35) s.particles.push({ x: cart.x - 0.5, y: cart.y - 0.15, vx: -3 - rng(s) * 3, vy: 1 + rng(s) * 2, life: 0.3, maxLife: 0.3, kind: "spark", hue: 40 });
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

    // wind (pulses: floating alone stalls during the off-phase — recoil chains carry you)
    for (const w of s.winds) {
      w.timer += dt;
      if (w.active && w.timer >= w.onTime) { w.active = false; w.timer = 0; }
      else if (!w.active && w.timer >= w.offTime) { w.active = true; w.timer = 0; }
      if (w.active && overlaps(playerRect(p), w)) {
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
    const fallVy = p.vy;
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
    if (!wasGrounded && p.grounded) p.landedImpact = Math.max(0.35, Math.min(1, Math.abs(fallVy) / 15));

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
        if (best >= 0) {
          s.tether = { ringIndex: best, len: Math.max(1.2, Math.min(bestD, 3.4)) };
          p.ammo = p.maxAmmo; // grabbing the world = reload
        }
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

  // ---- shooting (shot economy: grounded/riding = full clip, airborne shots limited) ----
  p.shootCooldown = Math.max(0, p.shootCooldown - dt);
  p.dryFire = Math.max(0, p.dryFire - dt);
  if (p.grounded || cart.riding) p.ammo = p.maxAmmo;
  if (input.shoot && p.shootCooldown <= 0 && !p.dead && !s.won && p.ammo <= 0) {
    if (p.dryFire <= 0) { s.toast = { text: "out of puff! land to reload", timer: 1 }; p.dryFire = 1.1; }
  }
  if (input.shoot && p.shootCooldown <= 0 && !p.dead && !s.won && p.ammo > 0) {
    const dx = input.aimX - p.x, dy = input.aimY - (p.y + 0.3);
    const len = Math.hypot(dx, dy) || 1;
    s.bullets.push({
      x: p.x + (dx / len) * 0.35, y: p.y + 0.3 + (dy / len) * 0.35,
      dx: dx / len, dy: dy / len, speed: 26, bounces: 0, dead: false, age: 0,
    });
    p.shootCooldown = 0.28;
    if (!p.grounded && !cart.riding && p.coyote <= 0) p.ammo--; // coyote grace: just-walked-off shots are free
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
    if (b.age > 3.5) { b.dead = true; continue; }
    const steps = 3; // substeps for fast bullets
    for (let i = 0; i < steps && !b.dead; i++) {
      const sub = dt / steps;
      b.x += b.dx * b.speed * sub;
      b.y += b.dy * b.speed * sub;
      const br: Rect = { x: b.x - 0.14, y: b.y - 0.14, w: 0.28, h: 0.28 };

      // player bullets can shoot hostile peanuts down (both pop)
      if (!b.hostile) {
        for (const hb of s.bullets) {
          if (hb.hostile && !hb.dead && Math.hypot(hb.x - b.x, hb.y - b.y) < 0.42) {
            hb.dead = true; b.dead = true;
            s.hitStop = Math.max(s.hitStop, 0.04);
            s.shake = Math.max(s.shake, 0.15);
            spawnParticles(s, (hb.x + b.x) / 2, (hb.y + b.y) / 2, 10, "confetti", 25);
            s.toast = { text: "intercepted!", timer: 0.7 };
            break;
          }
        }
        if (b.dead) break;
      }
      // hostile peanuts hurt the player instead
      if (b.hostile) {
        if (Math.hypot(b.x - p.x, b.y - (p.y + 0.2)) < 0.75) {
          damagePlayer(s, 12, b.dx * 6, 4);
          b.dead = true;
        }
      }
      // boss hit
      const boss = s.boss;
      if (!b.hostile && boss.phase !== "waiting" && boss.phase !== "dead" &&
          overlaps(br, { x: boss.x - boss.w / 2, y: boss.y - boss.h / 2, w: boss.w, h: boss.h })) {
        const wasAbove = boss.hp >= boss.maxHp * 0.4;
        boss.hp = Math.max(0, boss.hp - 7);
        if (wasAbove && boss.hp < boss.maxHp * 0.4 && boss.hp > 0) s.toast = { text: "he's FURIOUS!", timer: 1.6 };
        boss.hitFlash = 0.15;
        b.dead = true;
        s.shake = Math.max(s.shake, 0.2);
        s.hitStop = Math.max(s.hitStop, 0.05);
        spawnParticles(s, b.x, b.y, 8, "spark", 20);
        if (boss.hp <= 0 && boss.phase !== "dead") {
          boss.phase = "dead"; boss.timer = 0;
          s.hitStop = 0.25;
          s.won = true;
          s.toast = { text: "BOSS DOWN!", timer: 4 };
        }
        continue;
      }

      // lever hit (shooting a lever toggles it)
      if (b.hostile) { /* hostile shots don't operate machinery */ }
      else for (const lv of s.levers) {
        const guard = s.doors.find(d => d.id === "leverguard");
        const guarded = guard ? guard.openAmount < 0.9 : false;
        if (!guarded && Math.hypot(lv.x - b.x, lv.y - b.y) < 0.7) {
          toggleLever(s, lv);
          b.dead = true;
        }
      }
      if (b.dead) break;

      // box hit → slide
      if (!b.hostile) for (const box of s.boxes) {
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
        // one-way: bullets bounce only when traveling down onto the top face (same
        // rule as feet) — shooting up through a platform still works
        if (isOneWay(s, rect) && !(b.dy < 0 && b.y > rect.y + rect.h - 0.2)) continue;
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
        if (!b.hostile && pd < 6 && !cart.riding) {
          const falloff = 1 - pd / 6;
          let ix = nx, iy = ny;
          if (ny > 0.5) iy += 1; // ground bounces boost extra up (the original's trick)
          const il = Math.hypot(ix, iy) || 1;
          const force = b.speed * 0.6 * falloff;
          p.vx += (ix / il) * force;
          p.vy += (iy / il) * force;
          s.stats.launches++;
          s.shake = Math.max(s.shake, 0.25);
          s.hitStop = Math.max(s.hitStop, 0.03);
          if (mover && mover.ricochet) p.ammo = p.maxAmmo; // green bounce = reload
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

  // ---- peanuts (optional-route collectibles) ----
  for (const pn of s.peanuts) {
    if (!pn.taken && Math.hypot(pn.x - p.x, pn.y - p.y) < 0.9) {
      pn.taken = true;
      s.stats.peanuts++;
      spawnParticles(s, pn.x, pn.y, 6, "confetti", 45);
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
    case "intro": {
      boss.timer += dt;
      const airborne = boss.y > 45.4 + boss.h / 2 + 0.001 || boss.vy > 0;
      if (airborne) {
        boss.vy -= 24 * dt;
        boss.x += boss.vx * dt; boss.y += boss.vy * dt;
        if (boss.y <= 45.4 + boss.h / 2 && boss.vy < 0) {
          // single landing moment
          boss.y = 45.4 + boss.h / 2; boss.vy = 0; boss.vx = 0;
          s.shake = 0.8;
          s.zoomTarget = 1;
          spawnParticles(s, boss.x, boss.y - 1, 16, "puff", 20);
        }
      }
      if (boss.timer > 2) { boss.phase = "chase"; boss.timer = 0; }
      return;
    }
    case "chase": {
      boss.faceDir = p.x < boss.x ? -1 : 1;
      // READABLE DODGE: only bullets entering his facing arc trigger it, after a
      // visible crouch telegraph. Bank shots off the mirrors arrive from behind.
      boss.dodgeTelegraph = Math.max(0, boss.dodgeTelegraph - dt);
      if (boss.dodgeTelegraph > 0 && boss.dodgeTelegraph - dt <= 0) {
        boss.vx = -boss.faceDir * 11; // hop backward out of the shot line
        boss.dodgeCooldown = boss.hp < boss.maxHp * 0.4 ? 0.7 : 1.0;
      }
      if (boss.dodgeCooldown <= 0 && boss.dodgeTelegraph <= 0) {
        for (const b of s.bullets) {
          const toBossX = boss.x - b.x;
          // bullet is heading at the boss AND coming from the side he faces
          const headingAtBoss = Math.sign(b.dx) === Math.sign(toBossX) && b.dx !== 0;
          const fromFacingSide = Math.sign(-toBossX) === boss.faceDir;
          if (headingAtBoss && fromFacingSide && Math.abs(toBossX) < 9 && Math.abs(b.y - boss.y) < 2.5) {
            boss.dodgeTelegraph = 0.22;
            break;
          }
        }
      }
      const dist = Math.abs(p.x - boss.x);
      boss.leapCooldown -= dt;
      boss.attackCooldown -= dt;
      // friction after dodge dash
      boss.vx *= Math.pow(0.05, dt);
      const enraged = boss.hp < boss.maxHp * 0.4;
      const chaseV = (enraged ? 5.6 : 4.6) * boss.faceDir;
      // PHASE 2: enraged, he uses your own trick — bouncing trunk-shots
      boss.spitCooldown -= dt;
      if (enraged && boss.spitCooldown <= 0) {
        boss.spitCooldown = 2.6;
        for (const spread of [-0.25, 0.05, 0.35]) {
          const ddx = Math.sign(p.x - boss.x) || boss.faceDir;
          const len = Math.hypot(1, spread) || 1;
          s.bullets.push({
            x: boss.x + ddx * 1.4, y: boss.y + 0.4,
            dx: ddx / len, dy: spread / len,
            speed: 13, bounces: 0, dead: false, age: 0, hostile: true,
          });
        }
        s.toast = { text: "peanut volley!", timer: 0.9 };
        spawnParticles(s, boss.x + (Math.sign(p.x - boss.x) || boss.faceDir) * 1.4, boss.y + 0.4, 8, "spark", 10);
        s.shake = Math.max(s.shake, 0.2);
      }
      boss.x += (chaseV + boss.vx) * dt;
      // gravity/floor
      boss.y = Math.max(45.4 + boss.h / 2, boss.y - 10 * dt);
      if (boss.leapCooldown <= 0 && dist > 3) {
        boss.phase = "leap"; boss.timer = 0;
        boss.vy = 11;
        // lead the target: aim where the player is heading
        const predicted = p.x + p.vx * 0.55;
        boss.vx = Math.sign(predicted - boss.x) * Math.min(11, Math.abs(predicted - boss.x) * 1.3);
        boss.leapCooldown = enraged ? 2.4 : 3.5;
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
        if (p.grounded && Math.abs(p.x - boss.x) < 4.5) damagePlayer(s, 10, Math.sign(p.x - boss.x) * 8, 8);
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

// Trajectory preview: march the shot direction, reflect once, return polyline.
export function previewPath(s: State, fx: number, fy: number, dx: number, dy: number): Vec[] {
  const pts: Vec[] = [{ x: fx, y: fy }];
  let x = fx, y = fy, vx = dx, vy = dy;
  let bounces = 0;
  const sol = solids(s, true);
  for (let i = 0; i < 240; i++) {
    x += vx * 0.12; y += vy * 0.12;
    const br = { x: x - 0.14, y: y - 0.14, w: 0.28, h: 0.28 };
    for (const { rect, mover } of sol) {
      if (!overlaps(br, rect)) continue;
      if (isOneWay(s, rect) && !(vy < 0 && y > rect.y + rect.h - 0.2)) continue;
      if (mover && !mover.ricochet) { pts.push({ x, y }); return pts; }
      const penL = x + 0.14 - rect.x, penR = rect.x + rect.w - (x - 0.14);
      const penB = y + 0.14 - rect.y, penT = rect.y + rect.h - (y - 0.14);
      const m = Math.min(penL, penR, penB, penT);
      let nx = 0, ny = 0;
      if (m === penL) { nx = -1; x = rect.x - 0.15; }
      else if (m === penR) { nx = 1; x = rect.x + rect.w + 0.15; }
      else if (m === penB) { ny = -1; y = rect.y - 0.15; }
      else { ny = 1; y = rect.y + rect.h + 0.15; }
      const dot = vx * nx + vy * ny;
      vx -= 2 * dot * nx; vy -= 2 * dot * ny;
      pts.push({ x, y });
      bounces++;
      if (bounces >= 2) return pts;
      break;
    }
    if (i % 6 === 0) pts.push({ x, y });
  }
  pts.push({ x, y });
  return pts;
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
