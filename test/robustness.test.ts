// Robustness + difficulty tests promoted from ad-hoc scripts (review finding #12).
import { test, expect } from "bun:test";
import { makeLevel, step, DT, IDLE_INPUT, type Input } from "../src/game";

test("fuzz: 36k random-input steps — no NaN, no leaks, no out-of-world", () => {
  let noise = 424243;
  const rnd = () => { noise = (noise * 1103515245 + 12345) % 2147483648; return noise / 2147483648; };
  const s = makeLevel();
  for (let i = 0; i < 36000; i++) {
    step(s, {
      ...IDLE_INPUT,
      left: rnd() < 0.3, right: rnd() < 0.4,
      aimX: s.player.x + (rnd() - 0.5) * 20, aimY: s.player.y + (rnd() - 0.5) * 20,
      shoot: rnd() < 0.06, shootHeld: rnd() < 0.3,
      interact: rnd() < 0.02, interactHeld: rnd() < 0.1,
    }, DT);
    const p = s.player;
    expect(Number.isFinite(p.x) && Number.isFinite(p.y) && Number.isFinite(p.vx) && Number.isFinite(p.vy)).toBe(true);
    if (p.y < -50) throw new Error(`out of world: y=${p.y}`);
    if (s.bullets.length > 40) throw new Error(`bullet leak: ${s.bullets.length}`);
    if (s.particles.length > 400) throw new Error(`particle leak: ${s.particles.length}`);
  }
}, 30000);

test("casual difficulty: sloppy player crosses the gap field within a death budget", () => {
  const results: number[] = [];
  for (const seed of [1, 2, 3]) {
    const s = makeLevel();
    let noise = seed * 7919;
    const rnd = () => { noise = (noise * 1103515245 + 12345) % 2147483648; return noise / 2147483648; };
    for (let i = 0; i < 120; i++) step(s, IDLE_INPUT, DT);
    for (let t = 0; t < 60 && s.player.x < 50; t += 0.3) {
      const p = s.player;
      if (p.grounded) {
        const nearEdge = !s.platforms.some(r => p.x + 1 >= r.x && p.x + 1 <= r.x + r.w && p.y - 0.6 >= r.y && p.y - 0.6 - (r.y + r.h) < 6);
        if (nearEdge && rnd() > 0.3) step(s, { ...IDLE_INPUT, aimX: p.x - 0.8 + rnd() * 0.6 + (rnd() - 0.5) * 1.6, aimY: p.y - 2, shoot: true, shootHeld: true }, DT);
      } else if (p.vy < -3 && p.ammo > 0 && rnd() > 0.5) {
        step(s, { ...IDLE_INPUT, aimX: p.x + (rnd() - 0.5), aimY: p.y - 2, shoot: true, shootHeld: true }, DT);
      }
      for (let i = 0; i < 36; i++) step(s, { ...IDLE_INPUT, right: true }, DT);
    }
    expect(s.player.x).toBeGreaterThanOrEqual(50); // crossed
    results.push(s.stats.deaths);
  }
  // death budget: clumsy play may die, but not grind
  expect(Math.min(...results)).toBeLessThanOrEqual(2);
  expect(Math.max(...results)).toBeLessThanOrEqual(12);
}, 30000);
