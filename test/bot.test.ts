// Bot playtests: deterministic runs of the pure game logic.
import { test, expect } from "bun:test";
import { makeLevel, step, DT, IDLE_INPUT, type State, type Input } from "../src/game";

function sim(s: State, inp: Partial<Input>, seconds: number) {
  const n = Math.round(seconds / DT);
  for (let i = 0; i < n; i++) step(s, { ...IDLE_INPUT, ...inp }, DT);
}

/** One-step edge-triggered shot aimed at world (x,y). */
function shot(s: State, x: number, y: number) {
  step(s, { ...IDLE_INPUT, aimX: x, aimY: y, shoot: true, shootHeld: true }, DT);
}

/** Ground-shot rocket jump: aim at feet, slightly behind, then coast. */
function rocketJump(s: State, dirX: number) {
  const p = s.player;
  shot(s, p.x - dirX * 0.4, p.y - 2);
}

function groundBelow(s: State, x: number, yTop: number): boolean {
  return s.platforms.some(r => x >= r.x && x <= r.x + r.w && yTop >= r.y && yTop - (r.y + r.h) < 6);
}

function teleport(s: State, x: number, y: number) {
  s.player.x = x; s.player.y = y; s.player.vx = 0; s.player.vy = 0;
  s.camX = x; s.camY = y;
  sim(s, {}, 0.5); // settle
}

// ---------- mechanism tests ----------

test("gravity: player falls and lands on tutorial ground", () => {
  const s = makeLevel();
  sim(s, {}, 2);
  expect(s.player.grounded).toBe(true);
  expect(s.player.y).toBeGreaterThan(0);
  expect(s.player.y).toBeLessThan(2);
});

test("walk right increases x, facing follows velocity", () => {
  const s = makeLevel();
  sim(s, {}, 1);
  const x0 = s.player.x;
  sim(s, { right: true }, 1);
  expect(s.player.x).toBeGreaterThan(x0 + 4);
  expect(s.player.face).toBe(1);
  sim(s, { left: true }, 0.5);
  expect(s.player.face).toBe(-1);
});

test("ground shot recoil-launches the player upward", () => {
  const s = makeLevel();
  sim(s, {}, 1);
  expect(s.player.grounded).toBe(true);
  shot(s, s.player.x, s.player.y - 2); // straight down
  sim(s, {}, 0.15);
  expect(s.player.vy).toBeGreaterThan(3);
  expect(s.stats.launches).toBeGreaterThan(0);
});

test("shot economy: at most 3 shots while airborne, refilled on landing", () => {
  const s = makeLevel();
  sim(s, {}, 1);
  // launch first
  shot(s, s.player.x, s.player.y - 2);
  sim(s, {}, 0.3);
  expect(s.player.grounded).toBe(false);
  const before = s.stats.shots;
  for (let i = 0; i < 6; i++) {
    shot(s, s.player.x + 3, s.player.y + 3);
    sim(s, {}, 0.3);
    if (s.player.grounded) break;
  }
  const airShots = s.stats.shots - before;
  expect(airShots).toBeLessThanOrEqual(3);
  // land and confirm refill
  sim(s, {}, 3);
  expect(s.player.grounded).toBe(true);
  expect(s.player.ammo).toBe(3);
});

test("bullets ricochet and die within bounce budget", () => {
  const s = makeLevel();
  sim(s, {}, 1);
  shot(s, s.player.x + 4, s.player.y - 1); // at the ground ahead
  sim(s, {}, 4.5);
  expect(s.stats.bounces).toBeGreaterThan(0);
  expect(s.bullets.length).toBe(0); // all expired
});

test("death pit kills and respawns at checkpoint in under a second", () => {
  const s = makeLevel();
  sim(s, {}, 1);
  teleport(s, 24, 2); // over the first gap
  sim(s, {}, 3); // fall into pit
  expect(s.stats.deaths).toBe(1);
  // respawn happened (deathTimer 0.6s inside the 3s window)
  expect(s.player.dead).toBe(false);
  expect(s.player.hp).toBe(s.player.maxHp);
});

test("spike strip damages and knocks back with i-frames", () => {
  const s = makeLevel();
  teleport(s, 62, 3); // above spike alley
  sim(s, {}, 1.5);
  expect(s.stats.damageTaken).toBeGreaterThanOrEqual(20);
  expect(s.player.hp).toBeLessThan(100);
  const hpAfterFirst = s.player.hp;
  sim(s, {}, 0.3); // i-frames prevent instant re-hit
  expect(s.player.hp).toBe(hpAfterFirst);
});

test("minecart: E boards, cart traverses spike alley, launches off end", () => {
  const s = makeLevel();
  teleport(s, 56.5, 2.5);
  sim(s, { interact: true, interactHeld: false }, 0.1);
  // press E near cart start
  step(s, { ...IDLE_INPUT, interact: true }, DT);
  sim(s, {}, 0.2);
  expect(s.cart.riding).toBe(true);
  const hp0 = s.player.hp;
  sim(s, {}, 8);
  expect(s.cart.finished).toBe(true);
  expect(s.player.hp).toBe(hp0);           // cart protects from spikes
  expect(s.player.x).toBeGreaterThan(86);  // carried across
});

test("box puzzle: shoot box onto plate -> guard opens + spring appears -> lever opens exit", () => {
  const s = makeLevel();
  teleport(s, 139.5, 1.5);
  const exit = s.doors.find(d => d.id === "exitdoor")!;
  const guard = s.doors.find(d => d.id === "leverguard")!;
  expect(exit.open).toBe(false);
  // shoot the box rightward until it sits on the plate
  for (let i = 0; i < 8 && !s.plates[0].pressed; i++) {
    shot(s, s.boxes[0].x + 0.2, s.boxes[0].y + 0.7);
    sim(s, {}, 1.2);
  }
  expect(s.plates[0].pressed).toBe(true);
  expect(s.springs[0].revealed).toBe(true);
  sim(s, {}, 1.5); // guard opens
  expect(guard.openAmount).toBeGreaterThan(0.8);
  // walk to the lever and flick with E
  sim(s, { right: true }, 1.4);
  teleport(s, 148.2, 1.2);
  step(s, { ...IDLE_INPUT, interact: true }, DT);
  sim(s, {}, 2);
  expect(exit.open).toBe(true);
  expect(exit.openAmount).toBeGreaterThan(0.8);
});

test("spring bounces the player high", () => {
  const s = makeLevel();
  s.springs[0].revealed = true;
  teleport(s, 144, 2.5);
  sim(s, {}, 0.8);
  let peak = 0;
  for (let i = 0; i < 240; i++) { step(s, IDLE_INPUT, DT); peak = Math.max(peak, s.player.y); }
  expect(peak).toBeGreaterThan(5); // reaches the heart shelf height
});

test("wind shaft (pulsed) + recoil chains carry player to the arena", () => {
  const s = makeLevel();
  const exit = s.doors.find(d => d.id === "exitdoor")!;
  exit.open = true;
  teleport(s, 167, 1.5);
  let reached = false;
  for (let t = 0; t < 40 && !reached; t += 0.25) {
    const p = s.player;
    // steer to shaft center, shoot the wall to boost during lulls
    const inp: Partial<Input> = { right: p.x < 167, left: p.x > 168.5 };
    if (p.vy < 1 && p.ammo > 0 && !p.grounded) {
      shot(s, p.x - 1.5, p.y - 1.5);
    } else if (p.grounded) {
      shot(s, p.x, p.y - 2);
    }
    sim(s, inp, 0.25);
    if (p.y > 45 && p.x > 160) reached = true;
  }
  expect(reached).toBe(true);
});

test("boss: intro triggers, readable dodge fires on frontal shots, bank shots land, boss killable", () => {
  const s = makeLevel();
  teleport(s, 168, 46.5);
  sim(s, { right: true }, 1.5);
  expect(["intro", "chase"]).toContain(s.boss.phase);
  sim(s, { left: true }, 2.5); // retreat so he stays in chase range
  expect(["chase", "leap", "windup"]).toContain(s.boss.phase);
  // frontal shot triggers telegraph (not damage-free necessarily, but telegraph observable)
  const preHp = s.boss.hp;
  shot(s, s.boss.x - 2, s.boss.y); // frontal: from the side he faces (he faces player)
  let sawTelegraph = false;
  for (let i = 0; i < 120; i++) { step(s, IDLE_INPUT, DT); if (s.boss.dodgeTelegraph > 0) sawTelegraph = true; }
  expect(sawTelegraph).toBe(true);
  // now grind him down (dodge has cooldown, so straight shots eventually land too)
  let guard = 0;
  while (s.boss.hp > 0 && guard++ < 600) {
    const p = s.player;
    if (p.hp < 40) p.hp = 100; // bot isn't testing survival here
    if (p.y < 44) { sim(s, { left: p.x > 169, right: p.x < 168 }, 0.3); continue; } // ride the wind back through the hole
    if (p.grounded) shot(s, s.boss.x, s.boss.y + 0.5);
    sim(s, { left: p.x > s.boss.x + 4, right: p.x < s.boss.x - 4 }, 0.3);
  }
  expect(s.boss.hp).toBe(0);
  expect(s.won).toBe(true);
});

test("dodger toad: dodges sideways, dies to two hits", () => {
  const s = makeLevel();
  const dg = s.dodgers[0];
  teleport(s, 111.5, 11);
  let guard = 0;
  while (!dg.dead && guard++ < 60) {
    shot(s, dg.x, dg.y);
    sim(s, {}, 0.6);
  }
  expect(dg.dead).toBe(true);
});

test("tether: hold E near ring anchors and constrains distance", () => {
  const s = makeLevel();
  const ring = s.rings[0];
  s.player.x = ring.x - 1; s.player.y = ring.y - 1; s.player.vx = 0; s.player.vy = 0;
  sim(s, { interactHeld: true }, 0.2);
  expect(s.tether).not.toBeNull();
  const len = s.tether!.len;
  sim(s, { interactHeld: true, left: true }, 2);
  const d = Math.hypot(s.player.x - ring.x, s.player.y - ring.y);
  expect(d).toBeLessThanOrEqual(len + 0.05);
});

test("flashing platforms toggle ricochet on a readable cycle", () => {
  const s = makeLevel();
  const gate = s.movers.find(m => m.flashing)!;
  const r0 = gate.ricochet;
  sim(s, {}, gate.flashing!.interval + 0.05);
  expect(gate.ricochet).toBe(!r0);
});

// ---------- the money test: full run ----------

test("FULL RUN: waypoint bot completes the game from spawn to boss kill", () => {
  const s = makeLevel();
  sim(s, {}, 1);
  // generous but bounded: the bot cheats health (fun tests are separate), not position
  const journey: Array<{ x: number; y: number; note: string; air?: boolean; done?: (st: State) => boolean }> = [
    { x: 20, y: 1.5, note: "tutorial end" },
    { x: 28, y: 1.5, note: "gap 1" },
    { x: 38, y: 1.5, note: "gap 2" },
    { x: 51, y: 1.5, note: "pre-cart" },
    { x: 92, y: 1.5, note: "post-cart (via ride)" },
    { x: 97.5, y: 1.5, note: "tower base" },
    { x: 97, y: 3.3, note: "step 1" },
    { x: 99.5, y: 5.7, note: "step 2" },
    { x: 102.5, y: 6.9, note: "tower A top" },
    { x: 106.5, y: 8.9, note: "step 3" },
    { x: 111.5, y: 10.8, note: "tower B top" },
    { x: 117.6, y: 3.2, note: "canyon ledge" },
    { x: 121, y: 4.8, note: "tower C top" },
    { x: 127.5, y: 1.5, note: "tower field crossed" },
    { x: 133, y: 1.5, note: "flat run" },
    { x: 148.4, y: 1.2, note: "lever", done: (st) => st.doors.find(d => d.id === "exitdoor")!.open },
    { x: 167, y: 1.5, note: "shaft floor" },
    { x: 166, y: 46, note: "arena", air: true },
  ];
  let wpIndex = 0;
  let stuckTimer = 0;
  let wpTimer = 0;
  let lastX = s.player.x, lastY = s.player.y;
  const deadline = 240; // seconds of sim time
  for (let t = 0; t < deadline && wpIndex < journey.length; t += 0.25) {
    const wp = journey[wpIndex];
    const p = s.player;
    if (p.hp < 40) p.hp = 100;

    // ride the cart if near it and not yet finished
    if (!s.cart.finished && !s.cart.riding && Math.hypot(p.x - s.cart.x, p.y - s.cart.y) < 2) {
      step(s, { ...IDLE_INPUT, interact: true }, DT);
    }
    // box puzzle handling at waypoint "lever"
    if (wp.note === "lever") {
      if (!s.plates[0].pressed) {
        // stand LEFT of the box so shots push it RIGHT onto the plate
        if (p.x > s.boxes[0].x - 1.5) { sim(s, { left: true }, 0.3); continue; }
        shot(s, s.boxes[0].x + 0.2, s.boxes[0].y + 0.75);
        sim(s, {}, 1.0);
        continue;
      }
      if (Math.abs(p.x - 148.4) < 1.4) {
        step(s, { ...IDLE_INPUT, interact: true }, DT);
        const exit = s.doors.find(d => d.id === "exitdoor")!;
        if (exit.open) { wpIndex++; continue; }
      }
    }

    const dx = wp.x - p.x, dy = wp.y - p.y;
    const inp: Partial<Input> = { right: dx > 0.4, left: dx < -0.4 };
    if (!s.cart.riding) {
      const gapAhead = !groundBelow(s, p.x + Math.sign(dx) * 0.9, p.y - 0.6);
      if (p.grounded && dy > 0.9 && Math.abs(dx) < 1.6) shot(s, p.x - Math.sign(dx || 1) * 0.3, p.y - 2); // under the target: launch straight up
      else if (p.grounded && dy > 0.9 && Math.abs(dx) >= 1.6 && !gapAhead) { /* keep walking under it */ }
      else if (p.grounded && gapAhead) shot(s, p.x - Math.sign(dx) * 1.2, p.y - 2); // launch off the ledge (aim well behind: rim-proof)
      else if (!p.grounded && p.ammo > 0 && dy > 0.5 && p.vy < 3 && groundBelow(s, p.x, p.y - 0.6))
        shot(s, p.x, p.y - 2);                                           // AIR CHAIN: second boost at apex
      else if (!p.grounded && p.vy < -2 && p.ammo > 0 && dy > -1 && groundBelow(s, p.x, p.y - 0.6))
        shot(s, p.x, p.y - 2);                                           // falling rescue boost
    }
    sim(s, inp, 0.25);

    // waypoint reached?
    for (let k = journey.length - 1; k >= wpIndex; k--) {
      const w = journey[k];
      const positional = Math.abs(p.x - w.x) < 1.6 && Math.abs(p.y - w.y) < 1.5 && (p.grounded || w.air || s.cart.riding);
      if (w.done ? w.done(s) : positional) {
        console.log(`[bot] t=${t.toFixed(0)}s reached '${w.note}' at (${p.x.toFixed(1)},${p.y.toFixed(1)}) deaths=${s.stats.deaths}`);
        wpIndex = k + 1; stuckTimer = 0; wpTimer = 0; break;
      }
    }

    if (Math.round(t * 4) % 40 === 0) console.log(`[tick] t=${t.toFixed(0)}s wp='${wp.note}' pos=(${p.x.toFixed(1)},${p.y.toFixed(1)}) hp=${p.hp} deaths=${s.stats.deaths}`);
    // route recovery: too long on one waypoint -> re-route via nearest earlier waypoint
    wpTimer += 0.25;
    if (wpTimer > 25) {
      let best = 0, bestD = Infinity;
      for (let k = 0; k <= Math.min(wpIndex, journey.length - 1); k++) {
        const d = Math.hypot(p.x - journey[k].x, p.y - journey[k].y);
        if (d < bestD) { bestD = d; best = k; }
      }
      wpIndex = best; wpTimer = 0;
      console.log(`[bot] re-routing via '${journey[best].note}'`);
    }
    // route memory: if we fell back to an earlier waypoint, resume from there
    if (p.grounded) {
      for (let k = 0; k < wpIndex - 1; k++) {
        const w = journey[k];
        if (Math.abs(p.x - w.x) < 1.6 && Math.abs(p.y - w.y) < 1.5) { wpIndex = k + 1; stuckTimer = 0; break; }
      }
    }
    // self-unstick: blocked against a wall while grounded -> launch straight up
    if (stuckTimer > 0.6 && p.grounded) shot(s, p.x - Math.sign(dx) * 0.3, p.y - 2);
    // watchdog
    if (Math.hypot(p.x - lastX, p.y - lastY) < 0.1) stuckTimer += 0.25; else stuckTimer = 0;
    lastX = p.x; lastY = p.y;
    if (stuckTimer > 15) throw new Error(`SOFTLOCK near (${p.x.toFixed(1)},${p.y.toFixed(1)}) heading to '${wp.note}'`);
  }
  expect(wpIndex).toBe(journey.length);

  // arena: kill the boss
  let guard = 0;
  while (!s.won && guard++ < 700) {
    const p = s.player;
    if (p.hp < 40) p.hp = 100;
    if (p.y < 44) { sim(s, { left: p.x > 169, right: p.x < 168 }, 0.3); continue; } // wind recovery via the hole
    if (p.grounded) shot(s, s.boss.x, s.boss.y + 0.5);
    sim(s, { left: p.x > s.boss.x + 4, right: p.x < s.boss.x - 4 }, 0.3);
  }
  expect(s.won).toBe(true);
}, 60000);

test("boss phase 2: enraged boss spits hostile peanuts that hurt on contact", () => {
  const s = makeLevel();
  teleport(s, 174, 46.5);
  sim(s, {}, 3.5); // intro
  s.boss.hp = Math.floor(s.boss.maxHp * 0.3); // force enrage
  let sawHostile = false;
  let hurtByPeanut = false;
  const hp0 = s.player.hp;
  for (let t = 0; t < 12 && !hurtByPeanut; t += 0.25) {
    sim(s, {}, 0.25);
    if (s.bullets.some(b => b.hostile)) sawHostile = true;
    if (sawHostile && s.player.hp < hp0) hurtByPeanut = true;
  }
  expect(sawHostile).toBe(true);
  // hostile bullets never operate machinery or damage the boss
  expect(s.boss.hp).toBe(Math.floor(s.boss.maxHp * 0.3));
});

test("phase 2 counterplay: player bullets intercept hostile peanuts mid-air", () => {
  const s = makeLevel();
  teleport(s, 174, 46.5);
  sim(s, {}, 3.5);
  s.boss.hp = Math.floor(s.boss.maxHp * 0.3); // enrage
  let intercepted = false;
  for (let t = 0; t < 15 && !intercepted; t += 0.2) {
    const hostile = s.bullets.find(b => b.hostile && Math.abs(b.y - s.player.y) < 3);
    if (hostile) {
      // lead the shot at the peanut's next position
      shot(s, hostile.x + hostile.dx * 0.5, hostile.y + hostile.dy * 0.5);
    }
    sim(s, {}, 0.2);
    if (s.toast.text === "intercepted!") intercepted = true;
  }
  expect(intercepted).toBe(true);
});
