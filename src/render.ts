// TRUNK! — canvas renderer. Pure function of State; no game logic here.
import type { State, Player, Platform } from "./game";

export const VIEW_W = 960;
export const VIEW_H = 540;
const BASE_SCALE = 40; // px per world unit at zoom 1

export function worldToScreen(s: State, wx: number, wy: number): { x: number; y: number } {
  const sc = BASE_SCALE * s.zoom;
  return { x: VIEW_W / 2 + (wx - s.camX) * sc, y: VIEW_H / 2 - (wy - s.camY) * sc };
}

export function screenToWorld(s: State, sx: number, sy: number): { x: number; y: number } {
  const sc = BASE_SCALE * s.zoom;
  return { x: s.camX + (sx - VIEW_W / 2) / sc, y: s.camY - (sy - VIEW_H / 2) / sc };
}

const PAL = {
  skyTop: "#2b1d4f",
  skyMid: "#7a3b6e",
  skyLow: "#e8875a",
  hillFar: "#4b2a5e",
  hillNear: "#38204a",
  grass: "#7ec850",
  grassDark: "#4e9137",
  dirt: "#8a5a3b",
  dirtDark: "#6e4227",
  stone: "#585178",
  stoneDark: "#494263",
  metal: "#5b7d8a",
  metalDark: "#41606c",
  cloud: "#e9d9ef",
  cloudDark: "#c3aed3",
  skin: "#b9c6d9",
  skinDark: "#93a3bb",
  dress: "#ff7e67",
  dressDark: "#d95a48",
  cheek: "#f2a2b0",
  bullet: "#ffd24a",
  spike: "#3d3548",
  boss: "#7b6ff0",
  bossDark: "#5a4fc0",
};

const REDUCED_MOTION = typeof matchMedia !== "undefined" && matchMedia("(prefers-reduced-motion: reduce)").matches;

export function render(ctx: CanvasRenderingContext2D, s: State, mouse: { x: number; y: number }) {
  const sc = BASE_SCALE * s.zoom;
  // camera shake (disabled under prefers-reduced-motion)
  const shx = (!REDUCED_MOTION && s.shake > 0 ? (Math.sin(s.t * 91) * s.shake * 8) : 0);
  const shy = (!REDUCED_MOTION && s.shake > 0 ? (Math.cos(s.t * 83) * s.shake * 6) : 0);

  // ---- sky ----
  const g = ctx.createLinearGradient(0, 0, 0, VIEW_H);
  g.addColorStop(0, PAL.skyTop);
  g.addColorStop(0.55, PAL.skyMid);
  g.addColorStop(1, PAL.skyLow);
  ctx.fillStyle = g;
  ctx.fillRect(0, 0, VIEW_W, VIEW_H);

  // sun
  const sun = worldToScreen(s, s.camX + 6, s.camY + 5.5);
  ctx.fillStyle = "rgba(255,210,140,0.9)";
  ctx.beginPath(); ctx.arc(VIEW_W * 0.72, VIEW_H * 0.26, 46, 0, 7); ctx.fill();
  ctx.fillStyle = "rgba(255,210,140,0.25)";
  ctx.beginPath(); ctx.arc(VIEW_W * 0.72, VIEW_H * 0.26, 70, 0, 7); ctx.fill();

  // stars fade in with altitude (the climb ends above the clouds)
  const alt = Math.max(0, Math.min(1, (s.camY - 14) / 26));
  if (alt > 0.02) {
    for (let i = 0; i < 60; i++) {
      const sx = ((i * 137.5) % 97) / 97 * VIEW_W;
      const sy = (((i * 61.8) % 89) / 89) * VIEW_H * 0.75 - (s.camY * 2) % 40;
      const tw = 0.5 + 0.5 * Math.sin(s.t * (1 + (i % 5) * 0.5) + i);
      ctx.fillStyle = `rgba(255,240,220,${(0.55 * alt * tw).toFixed(3)})`;
      ctx.fillRect(sx, ((sy % VIEW_H) + VIEW_H) % VIEW_H * 0.8, 2, 2);
    }
  }

  // birds (ambient life, parallax)
  ctx.strokeStyle = "rgba(40,25,60,0.5)"; ctx.lineWidth = 2;
  for (let i = 0; i < 5; i++) {
    const bx = ((i * 331 + s.t * (14 + i * 3)) % (VIEW_W + 240)) - 120 - s.camX * 8 % 60;
    const by = 60 + (i * 67) % 160 + Math.sin(s.t * 2 + i * 2) * 8;
    const flap = Math.sin(s.t * 7 + i * 1.7) * 5;
    ctx.beginPath();
    ctx.moveTo(bx - 7, by - flap * 0.4);
    ctx.quadraticCurveTo(bx, by + flap, bx + 7, by - flap * 0.4);
    ctx.stroke();
  }

  // parallax hills
  drawHills(ctx, s, 0.25, PAL.hillFar, 150, 90);
  drawHills(ctx, s, 0.5, PAL.hillNear, 90, 60);

  ctx.save();
  ctx.translate(shx, shy);

  // ---- flora (Tree/MushroomBud/Stone from the original's prop shelf) ----
  for (let i = 0; i < s.platforms.length; i++) {
    const p = s.platforms[i];
    if (p.deco !== "grass" || p.w < 4) continue;
    const h1 = ((i * 2654435761) >>> 0) / 4294967296;
    const h2 = (((i + 7) * 2246822519) >>> 0) / 4294967296;
    const baseY = p.y + p.h;
    if (h1 < 0.6) drawTree(ctx, s, p.x + 1 + h1 * (p.w - 2), baseY, 0.8 + h2 * 0.5);
    if (h2 < 0.5) drawMushroom(ctx, s, p.x + p.w - 1.2 - h2 * (p.w - 2.4), baseY, 0.5 + h1 * 0.3);
    if ((h1 + h2) % 0.3 < 0.15) drawStone(ctx, s, p.x + (p.w * (h1 + h2)) % p.w, baseY);
  }

  // ---- platforms ----
  for (const p of s.platforms) drawPlatform(ctx, s, p);

  // ---- doors ----
  for (const d of s.doors) {
    const closedH = d.h * (1 - d.openAmount);
    if (closedH < 0.05) continue;
    const a = worldToScreen(s, d.x, d.y + closedH);
    ctx.fillStyle = "#c9a86a";
    ctx.fillRect(a.x, a.y, d.w * sc, closedH * sc);
    ctx.strokeStyle = "#8a6a3a"; ctx.lineWidth = 3;
    ctx.strokeRect(a.x + 2, a.y + 2, d.w * sc - 4, closedH * sc - 4);
  }

  // ---- plates ----
  for (const pl of s.plates) {
    const a = worldToScreen(s, pl.x, pl.y + (pl.pressed ? pl.h * 0.4 : pl.h));
    ctx.fillStyle = pl.pressed ? "#79e06a" : "#d0c060";
    ctx.fillRect(a.x, a.y, pl.w * sc, (pl.pressed ? pl.h * 0.4 : pl.h) * sc);
  }

  // ---- springs ----
  for (const sp of s.springs) {
    if (!sp.revealed) continue;
    const squish = 1 - sp.compress * 0.6;
    const a = worldToScreen(s, sp.x, sp.y + 0.5 * squish);
    ctx.fillStyle = "#e05a7a";
    ctx.fillRect(a.x, a.y, sp.w * sc, 0.18 * sc);
    // coil
    ctx.strokeStyle = "#f0f0f0"; ctx.lineWidth = 3;
    ctx.beginPath();
    const coils = 4;
    for (let i = 0; i <= coils * 8; i++) {
      const t = i / (coils * 8);
      const x = a.x + sp.w * sc * (0.25 + 0.5 * ((i % 8) / 8));
      const y = a.y + 0.18 * sc + t * (0.32 * squish) * sc;
      if (i === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
    }
    ctx.stroke();
  }

  // ---- boxes ----
  for (const b of s.boxes) {
    const a = worldToScreen(s, b.x, b.y + b.h);
    ctx.fillStyle = "#b07a3e";
    ctx.fillRect(a.x, a.y, b.w * sc, b.h * sc);
    ctx.strokeStyle = "#7d5426"; ctx.lineWidth = 3;
    ctx.strokeRect(a.x + 2, a.y + 2, b.w * sc - 4, b.h * sc - 4);
    ctx.beginPath();
    ctx.moveTo(a.x, a.y); ctx.lineTo(a.x + b.w * sc, a.y + b.h * sc);
    ctx.moveTo(a.x + b.w * sc, a.y); ctx.lineTo(a.x, a.y + b.h * sc);
    ctx.stroke();
  }

  // ---- movers ----
  for (const m of s.movers) {
    const a = worldToScreen(s, m.x, m.y + m.h);
    ctx.fillStyle = m.ricochet ? "#57d95a" : "#e0524f";
    ctx.fillRect(a.x, a.y, m.w * sc, m.h * sc);
    ctx.strokeStyle = "rgba(20,12,30,0.55)"; ctx.lineWidth = 3;
    ctx.strokeRect(a.x + 1.5, a.y + 1.5, m.w * sc - 3, m.h * sc - 3);
    ctx.fillStyle = "rgba(255,255,255,0.35)";
    ctx.fillRect(a.x, a.y, m.w * sc, 4);
    if (m.flashing) {
      // countdown pips
      const frac = 1 - m.flashing.timer / m.flashing.interval;
      ctx.fillStyle = "rgba(0,0,0,0.4)";
      ctx.fillRect(a.x + 3, a.y + m.h * sc - 6, (m.w * sc - 6) * frac, 3);
    }
  }

  // ---- wind zones: rising streaks while the updraft is ON ----
  for (const w of s.winds) {
    if (!w.active) continue;
    ctx.strokeStyle = "rgba(190,225,255,0.6)";
    ctx.lineWidth = 3;
    for (let i = 0; i < 16; i++) {
      const wx = w.x + ((i * 0.83) % 1) * w.w + (i % 3) * 0.4;
      const cycle = 2.2;
      const ph = ((s.t * (3.5 + (i % 4) * 0.9) + i * 1.7) % cycle) / cycle;
      const wy = w.y + ph * w.h;
      const a = worldToScreen(s, wx, wy);
      const b = worldToScreen(s, wx, wy + 0.9 + (i % 3) * 0.4);
      if (a.y < -40 || a.y > VIEW_H + 40) continue;
      ctx.globalAlpha = 0.85 * Math.sin(ph * Math.PI);
      ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(b.x, b.y); ctx.stroke();
    }
    ctx.globalAlpha = 1;
  }

  // ---- rails + cart ----
  drawRail(ctx, s);
  drawCart(ctx, s);

  // ---- spikes ----
  for (const st of s.spikeStrips) drawSpikeStrip(ctx, s, st);
  for (const sb of s.spikeBalls) drawSpikeBall(ctx, s, sb.x, sb.y, sb.r);

  // ---- grab rings ----
  for (let i = 0; i < s.rings.length; i++) {
    const r = s.rings[i];
    const a = worldToScreen(s, r.x, r.y);
    const active = s.tether?.ringIndex === i;
    ctx.strokeStyle = active ? "#ffe066" : "#d9b3ff";
    ctx.lineWidth = active ? 6 : 4;
    ctx.beginPath(); ctx.arc(a.x, a.y, r.r * sc, 0, 7); ctx.stroke();
    ctx.fillStyle = "rgba(217,179,255,0.25)";
    ctx.beginPath(); ctx.arc(a.x, a.y, r.r * sc * 0.55, 0, 7); ctx.fill();
  }

  // ---- levers ----
  for (const lv of s.levers) {
    const a = worldToScreen(s, lv.x, lv.y);
    ctx.strokeStyle = "#888"; ctx.lineWidth = 4;
    ctx.beginPath(); ctx.moveTo(a.x, a.y + 8);
    const ang = lv.on ? -0.7 : 0.7;
    ctx.lineTo(a.x + Math.sin(ang) * 0.8 * sc, a.y + 8 - Math.cos(ang) * 0.8 * sc);
    ctx.stroke();
    ctx.fillStyle = lv.on ? "#57d95a" : "#e0524f";
    ctx.beginPath();
    ctx.arc(a.x + Math.sin(ang) * 0.8 * sc, a.y + 8 - Math.cos(ang) * 0.8 * sc, 7, 0, 7);
    ctx.fill();
    ctx.fillStyle = "#666";
    ctx.fillRect(a.x - 8, a.y + 6, 16, 6);
  }

  // ---- peanuts ----
  for (const pn of s.peanuts) {
    if (pn.taken) continue;
    const a = worldToScreen(s, pn.x, pn.y + Math.sin(s.t * 3 + pn.x) * 0.08);
    ctx.save();
    ctx.translate(a.x, a.y); ctx.rotate(0.4 + Math.sin(s.t * 2 + pn.x) * 0.15);
    ctx.fillStyle = "#e8b04a";
    ctx.beginPath(); ctx.ellipse(0, 0, 9, 6.2, 0, 0, 7); ctx.fill();
    ctx.strokeStyle = "rgba(120,70,20,0.55)"; ctx.lineWidth = 1.5;
    ctx.beginPath(); ctx.moveTo(-6, -1); ctx.quadraticCurveTo(0, 3, 6, -1); ctx.stroke();
    ctx.fillStyle = "rgba(255,255,255,0.5)";
    ctx.beginPath(); ctx.arc(-3, -2, 2, 0, 7); ctx.fill();
    ctx.restore();
  }

  // ---- hearts ----
  for (const h of s.hearts) {
    if (h.taken) continue;
    const bob = Math.sin(s.t * 3 + h.x) * 0.1;
    drawHeart(ctx, s, h.x, h.y + bob, 0.4, "#ff5a76");
  }

  // ---- signs ----
  ctx.textAlign = "center";
  for (const sign of s.signs) {
    const a = worldToScreen(s, sign.x, sign.y);
    if (a.x < -200 || a.x > VIEW_W + 200) continue;
    ctx.fillStyle = "#8a6a3a";
    ctx.fillRect(a.x - 2, a.y, 4, 0.8 * sc);
    ctx.font = "600 13px system-ui, sans-serif";
    ctx.fillStyle = "rgba(30,20,40,0.55)";
    const w = ctx.measureText(sign.text).width + 20;
    roundRect(ctx, a.x - w / 2, a.y - 26, w, 24, 6);
    ctx.fill();
    ctx.fillStyle = "#ffe9c9";
    ctx.font = "600 13px system-ui, sans-serif";
    ctx.fillText(sign.text, a.x, a.y - 9);
  }

  // ---- checkpoint flags ----
  for (const c of s.checkpoints) {
    const active = s.checkpoint.x === c.x && s.checkpoint.y === c.y;
    const a = worldToScreen(s, c.x, c.y - 1.4);
    ctx.strokeStyle = "#ddd"; ctx.lineWidth = 3;
    ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(a.x, a.y - 1.6 * sc); ctx.stroke();
    ctx.fillStyle = active ? "#57d95a" : "#aaa";
    const wave = Math.sin(s.t * 4) * 4;
    ctx.beginPath();
    ctx.moveTo(a.x, a.y - 1.6 * sc);
    ctx.lineTo(a.x + 0.75 * sc, a.y - 1.45 * sc + wave * 0.3);
    ctx.lineTo(a.x, a.y - 1.25 * sc);
    ctx.closePath(); ctx.fill();
  }

  // ---- dodger toads ----
  for (const dg of s.dodgers) {
    if (dg.dead) continue;
    drawDodger(ctx, s, dg.x, dg.y, dg.hitFlash > 0);
  }

  // ---- tether rope ----
  if (s.tether) {
    const ring = s.rings[s.tether.ringIndex];
    const a = worldToScreen(s, ring.x, ring.y);
    const trunkTip = getTrunkTip(s);
    const b = worldToScreen(s, trunkTip.x, trunkTip.y);
    ctx.strokeStyle = PAL.skinDark; ctx.lineWidth = 6; ctx.lineCap = "round";
    ctx.beginPath(); ctx.moveTo(b.x, b.y);
    const mx = (a.x + b.x) / 2, my = Math.max(a.y, b.y) + 10;
    ctx.quadraticCurveTo(mx, my, a.x, a.y);
    ctx.stroke();
  }

  // ---- bullets ----
  for (const b of s.bullets) {
    const a = worldToScreen(s, b.x, b.y);
    if (b.hostile) {
      ctx.fillStyle = "rgba(224,82,79,0.35)";
      ctx.beginPath(); ctx.arc(a.x - b.dx * 8, a.y + b.dy * 8, 9, 0, 7); ctx.fill();
      ctx.fillStyle = "#e0524f";
      ctx.beginPath(); ctx.ellipse(a.x, a.y, 8, 6, s.t * 6, 0, 7); ctx.fill();
      ctx.fillStyle = "#8a2c2a";
      ctx.beginPath(); ctx.arc(a.x + 2, a.y, 2.5, 0, 7); ctx.fill();
    } else {
      ctx.fillStyle = "rgba(255,210,74,0.35)";
      ctx.beginPath(); ctx.arc(a.x - b.dx * 8, a.y + b.dy * 8, 8, 0, 7); ctx.fill();
      ctx.fillStyle = PAL.bullet;
      ctx.beginPath(); ctx.arc(a.x, a.y, 6, 0, 7); ctx.fill();
      ctx.fillStyle = "#fff";
      ctx.beginPath(); ctx.arc(a.x - 1.5, a.y - 1.5, 2.5, 0, 7); ctx.fill();
    }
  }

  // ---- boss ----
  drawBoss(ctx, s);

  // ---- player ----
  if (!s.player.dead) drawPlayer(ctx, s, mouse);
  else {
    const p = s.player;
    const a = worldToScreen(s, p.x, p.y);
    const spin = (0.6 - p.deathTimer) * 14;
    ctx.save();
    ctx.translate(a.x, a.y - (0.6 - p.deathTimer) * 30);
    ctx.rotate(spin);
    ctx.globalAlpha = Math.max(0, p.deathTimer / 0.6);
    ctx.fillStyle = PAL.skin;
    ctx.beginPath(); ctx.arc(0, -8, 14, 0, 7); ctx.fill();
    ctx.fillStyle = PAL.dress;
    ctx.beginPath(); ctx.ellipse(0, 8, 13, 11, 0, 0, 7); ctx.fill();
    ctx.restore();
    ctx.globalAlpha = 1;
  }

  // ---- particles ----
  for (const pt of s.particles) {
    const a = worldToScreen(s, pt.x, pt.y);
    const alpha = Math.max(0, pt.life / pt.maxLife);
    if (pt.kind === "confetti") {
      ctx.fillStyle = `hsla(${pt.hue},80%,65%,${alpha})`;
      ctx.fillRect(a.x - 3, a.y - 3, 6, 6);
    } else if (pt.kind === "hit") {
      ctx.fillStyle = `rgba(255,80,80,${alpha})`;
      ctx.beginPath(); ctx.arc(a.x, a.y, 4, 0, 7); ctx.fill();
    } else if (pt.kind === "puff") {
      ctx.fillStyle = `hsla(${pt.hue},30%,85%,${alpha * 0.7})`;
      ctx.beginPath(); ctx.arc(a.x, a.y, 6 * (1.5 - alpha), 0, 7); ctx.fill();
    } else {
      ctx.fillStyle = `hsla(${pt.hue},90%,60%,${alpha})`;
      ctx.beginPath(); ctx.arc(a.x, a.y, 3, 0, 7); ctx.fill();
    }
  }

  ctx.restore();

  // ---- HUD ----
  drawHud(ctx, s);
}

function drawTree(ctx: CanvasRenderingContext2D, s: State, x: number, y: number, size: number) {
  const sc = BASE_SCALE * s.zoom;
  const a = worldToScreen(s, x, y);
  if (a.x < -80 || a.x > VIEW_W + 80) return;
  const sway = Math.sin(s.t * 0.9 + x) * 2;
  ctx.fillStyle = "#6e4a30";
  ctx.fillRect(a.x - 3, a.y - 1.6 * size * sc, 6, 1.6 * size * sc);
  ctx.fillStyle = "#5da84e";
  ctx.beginPath(); ctx.arc(a.x + sway, a.y - 1.9 * size * sc, 0.55 * size * sc, 0, 7); ctx.fill();
  ctx.beginPath(); ctx.arc(a.x - 0.35 * size * sc + sway, a.y - 1.55 * size * sc, 0.4 * size * sc, 0, 7); ctx.fill();
  ctx.beginPath(); ctx.arc(a.x + 0.38 * size * sc + sway, a.y - 1.5 * size * sc, 0.42 * size * sc, 0, 7); ctx.fill();
  ctx.fillStyle = "rgba(255,255,255,0.12)";
  ctx.beginPath(); ctx.arc(a.x - 0.12 * size * sc + sway, a.y - 2.05 * size * sc, 0.22 * size * sc, 0, 7); ctx.fill();
}

function drawMushroom(ctx: CanvasRenderingContext2D, s: State, x: number, y: number, size: number) {
  const sc = BASE_SCALE * s.zoom;
  const a = worldToScreen(s, x, y);
  if (a.x < -60 || a.x > VIEW_W + 60) return;
  ctx.fillStyle = "#e8dcc9";
  ctx.fillRect(a.x - 3, a.y - 0.55 * size * sc, 6, 0.55 * size * sc);
  ctx.fillStyle = "#d95a48";
  ctx.beginPath(); ctx.ellipse(a.x, a.y - 0.55 * size * sc, 0.42 * size * sc, 0.26 * size * sc, 0, Math.PI, 0); ctx.fill();
  ctx.fillStyle = "rgba(255,255,255,0.7)";
  ctx.beginPath(); ctx.arc(a.x - 0.15 * size * sc, a.y - 0.62 * size * sc, 2.2, 0, 7); ctx.fill();
  ctx.beginPath(); ctx.arc(a.x + 0.12 * size * sc, a.y - 0.68 * size * sc, 1.7, 0, 7); ctx.fill();
}

function drawStone(ctx: CanvasRenderingContext2D, s: State, x: number, y: number) {
  const sc = BASE_SCALE * s.zoom;
  const a = worldToScreen(s, x, y);
  if (a.x < -40 || a.x > VIEW_W + 40) return;
  ctx.fillStyle = "#7a7189";
  ctx.beginPath(); ctx.ellipse(a.x, a.y - 4, 9, 6, 0, Math.PI, 0); ctx.fill();
  ctx.fillStyle = "rgba(255,255,255,0.15)";
  ctx.beginPath(); ctx.ellipse(a.x - 2, a.y - 6, 3, 2, 0, 0, 7); ctx.fill();
}

function drawHills(ctx: CanvasRenderingContext2D, s: State, parallax: number, color: string, base: number, amp: number) {
  ctx.fillStyle = color;
  ctx.beginPath();
  ctx.moveTo(0, VIEW_H);
  const off = s.camX * BASE_SCALE * s.zoom * parallax;
  for (let x = 0; x <= VIEW_W; x += 16) {
    const wx = (x + off) / 90;
    const y = VIEW_H - base + (VIEW_H * 0.13 - s.camY * 2) - Math.sin(wx) * amp * 0.5 - Math.sin(wx * 0.37 + 2) * amp;
    ctx.lineTo(x, Math.min(VIEW_H, Math.max(120, y)));
  }
  ctx.lineTo(VIEW_W, VIEW_H);
  ctx.closePath(); ctx.fill();
}

function drawPlatform(ctx: CanvasRenderingContext2D, s: State, p: Platform) {
  const sc = BASE_SCALE * s.zoom;
  const a = worldToScreen(s, p.x, p.y + p.h);
  const w = p.w * sc, h = p.h * sc;
  if (a.x > VIEW_W + 100 || a.x + w < -100 || a.y > VIEW_H + 100 || a.y + h < -100) return;
  const deco = p.deco ?? "stone";
  if (deco === "grass") {
    ctx.fillStyle = PAL.dirt; ctx.fillRect(a.x, a.y, w, h);
    ctx.fillStyle = PAL.dirtDark; ctx.fillRect(a.x, a.y + Math.min(h, 14), w, Math.max(0, h - 14));
    ctx.fillStyle = PAL.grass; ctx.fillRect(a.x, a.y, w, 10);
    ctx.fillStyle = PAL.grassDark;
    for (let x = 0; x < w; x += 22) ctx.fillRect(a.x + x, a.y + 8, 12, 5);
  } else if (deco === "metal") {
    ctx.fillStyle = PAL.metal; ctx.fillRect(a.x, a.y, w, h);
    ctx.fillStyle = PAL.metalDark; ctx.fillRect(a.x, a.y + h * 0.55, w, h * 0.45);
    ctx.fillStyle = "rgba(255,255,255,0.25)"; ctx.fillRect(a.x, a.y, w, 3);
  } else if (deco === "cloud") {
    ctx.fillStyle = PAL.cloud; roundRect(ctx, a.x, a.y, w, h, 10); ctx.fill();
    ctx.fillStyle = PAL.cloudDark; ctx.fillRect(a.x + 4, a.y + h - 6, w - 8, 4);
  } else {
    ctx.fillStyle = PAL.stone; ctx.fillRect(a.x, a.y, w, h);
    // subtle brickwork: darker mortar lines, low contrast
    ctx.strokeStyle = PAL.stoneDark; ctx.lineWidth = 2;
    ctx.beginPath();
    for (let yy = 24; yy < h; yy += 24) { ctx.moveTo(a.x, a.y + yy); ctx.lineTo(a.x + w, a.y + yy); }
    for (let yy = 0; yy < h; yy += 24) {
      for (let xx = ((yy / 24) % 2 === 0 ? 22 : 44); xx < w; xx += 44) {
        ctx.moveTo(a.x + xx, a.y + yy); ctx.lineTo(a.x + xx, a.y + Math.min(yy + 24, h));
      }
    }
    ctx.stroke();
    ctx.fillStyle = "rgba(255,255,255,0.12)"; ctx.fillRect(a.x, a.y, w, 4);
    ctx.fillStyle = "rgba(0,0,0,0.18)"; ctx.fillRect(a.x, a.y + h - 5, w, 5);
  }
}

function drawSpikeStrip(ctx: CanvasRenderingContext2D, s: State, st: { x: number; y: number; w: number; h: number }) {
  const sc = BASE_SCALE * s.zoom;
  const a = worldToScreen(s, st.x, st.y + st.h);
  ctx.fillStyle = PAL.spike;
  const n = Math.round(st.w / 0.45);
  for (let i = 0; i < n; i++) {
    const x0 = a.x + (i / n) * st.w * sc;
    const x1 = a.x + ((i + 1) / n) * st.w * sc;
    ctx.beginPath();
    ctx.moveTo(x0, a.y + st.h * sc);
    ctx.lineTo((x0 + x1) / 2, a.y);
    ctx.lineTo(x1, a.y + st.h * sc);
    ctx.closePath(); ctx.fill();
  }
}

function drawSpikeBall(ctx: CanvasRenderingContext2D, s: State, x: number, y: number, r: number) {
  const sc = BASE_SCALE * s.zoom;
  const a = worldToScreen(s, x, y);
  ctx.fillStyle = PAL.spike;
  for (let i = 0; i < 10; i++) {
    const ang = (i / 10) * Math.PI * 2 + s.t * 0.8;
    ctx.beginPath();
    ctx.moveTo(a.x + Math.cos(ang - 0.18) * r * sc * 0.8, a.y + Math.sin(ang - 0.18) * r * sc * 0.8);
    ctx.lineTo(a.x + Math.cos(ang) * r * sc * 1.25, a.y + Math.sin(ang) * r * sc * 1.25);
    ctx.lineTo(a.x + Math.cos(ang + 0.18) * r * sc * 0.8, a.y + Math.sin(ang + 0.18) * r * sc * 0.8);
    ctx.closePath(); ctx.fill();
  }
  ctx.beginPath(); ctx.arc(a.x, a.y, r * sc * 0.85, 0, 7); ctx.fill();
  ctx.fillStyle = "#5a4f6b";
  ctx.beginPath(); ctx.arc(a.x - r * sc * 0.2, a.y - r * sc * 0.25, r * sc * 0.3, 0, 7); ctx.fill();
}

function drawRail(ctx: CanvasRenderingContext2D, s: State) {
  const pts = s.cart.rail.points;
  ctx.strokeStyle = "#7d5426"; ctx.lineWidth = 5;
  ctx.beginPath();
  for (let i = 0; i < pts.length; i++) {
    const a = worldToScreen(s, pts[i].x, pts[i].y - 0.15);
    if (i === 0) ctx.moveTo(a.x, a.y); else ctx.lineTo(a.x, a.y);
  }
  ctx.stroke();
  // sleepers
  ctx.lineWidth = 3;
  for (let i = 0; i < pts.length - 1; i++) {
    const p0 = pts[i], p1 = pts[i + 1];
    const seg = Math.hypot(p1.x - p0.x, p1.y - p0.y);
    for (let d = 0; d < seg; d += 0.8) {
      const t = d / seg;
      const a = worldToScreen(s, p0.x + (p1.x - p0.x) * t, p0.y + (p1.y - p0.y) * t - 0.15);
      ctx.beginPath(); ctx.moveTo(a.x - 7, a.y + 6); ctx.lineTo(a.x + 7, a.y + 6); ctx.stroke();
    }
  }
}

function drawCart(ctx: CanvasRenderingContext2D, s: State) {
  const c = s.cart;
  if (c.finished && !c.riding) return;
  const sc = BASE_SCALE * s.zoom;
  const a = worldToScreen(s, c.x, c.y);
  ctx.fillStyle = "#8a6a3a";
  ctx.beginPath();
  ctx.moveTo(a.x - 0.7 * sc, a.y - 0.55 * sc);
  ctx.lineTo(a.x + 0.7 * sc, a.y - 0.55 * sc);
  ctx.lineTo(a.x + 0.5 * sc, a.y);
  ctx.lineTo(a.x - 0.5 * sc, a.y);
  ctx.closePath(); ctx.fill();
  ctx.fillStyle = "#6e4227";
  ctx.fillRect(a.x - 0.7 * sc, a.y - 0.58 * sc, 1.4 * sc, 5);
  ctx.fillStyle = "#3a3a3a";
  const wob = c.dist * 3;
  for (const wx of [-0.35, 0.35]) {
    ctx.beginPath(); ctx.arc(a.x + wx * sc, a.y + 2, 8, 0, 7); ctx.fill();
    ctx.strokeStyle = "#777"; ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(a.x + wx * sc, a.y + 2);
    ctx.lineTo(a.x + wx * sc + Math.cos(wob) * 7, a.y + 2 + Math.sin(wob) * 7);
    ctx.stroke();
  }
}

function drawDodger(ctx: CanvasRenderingContext2D, s: State, x: number, y: number, flash: boolean) {
  const sc = BASE_SCALE * s.zoom;
  const a = worldToScreen(s, x, y);
  const r = 0.55 * sc;
  const look = Math.max(-2.5, Math.min(2.5, (s.player.x - x) * 0.6));
  ctx.fillStyle = flash ? "#fff" : "#79c96a";
  ctx.beginPath(); ctx.ellipse(a.x, a.y, r, r * 0.85, 0, 0, 7); ctx.fill();
  // belly
  ctx.fillStyle = flash ? "#eee" : "#cfe8b8";
  ctx.beginPath(); ctx.ellipse(a.x, a.y + r * 0.25, r * 0.6, r * 0.45, 0, 0, 7); ctx.fill();
  // eyes track the player
  ctx.fillStyle = "#222";
  ctx.beginPath(); ctx.arc(a.x - r * 0.3 + look, a.y - r * 0.35, 3.5, 0, 7); ctx.fill();
  ctx.beginPath(); ctx.arc(a.x + r * 0.3 + look, a.y - r * 0.35, 3.5, 0, 7); ctx.fill();
  // hover wobble feet
  ctx.strokeStyle = "#5a9c4c"; ctx.lineWidth = 3;
  const k = Math.sin(s.t * 6) * 4;
  ctx.beginPath(); ctx.moveTo(a.x - r * 0.4, a.y + r * 0.8); ctx.lineTo(a.x - r * 0.5, a.y + r * 0.8 + 6 + k); ctx.stroke();
  ctx.beginPath(); ctx.moveTo(a.x + r * 0.4, a.y + r * 0.8); ctx.lineTo(a.x + r * 0.5, a.y + r * 0.8 + 6 - k); ctx.stroke();
}

function drawHeart(ctx: CanvasRenderingContext2D, s: State, x: number, y: number, size: number, color: string) {
  const sc = BASE_SCALE * s.zoom;
  const a = worldToScreen(s, x, y);
  const r = size * sc;
  ctx.fillStyle = color;
  ctx.beginPath();
  ctx.moveTo(a.x, a.y + r * 0.7);
  ctx.bezierCurveTo(a.x - r * 1.2, a.y - r * 0.3, a.x - r * 0.5, a.y - r, a.x, a.y - r * 0.35);
  ctx.bezierCurveTo(a.x + r * 0.5, a.y - r, a.x + r * 1.2, a.y - r * 0.3, a.x, a.y + r * 0.7);
  ctx.fill();
}

export function getTrunkTip(s: State): { x: number; y: number } {
  const p = s.player;
  if (s.tether) {
    // trunk reaches toward the ring
    const ring = s.rings[s.tether.ringIndex];
    return { x: ring.x, y: ring.y };
  }
  if (p.aimActive > 0) {
    const dx = p.aimX - p.x, dy = p.aimY - (p.y + 0.45);
    const len = Math.hypot(dx, dy) || 1;
    const reach = Math.min(len, 1.1);
    return { x: p.x + (dx / len) * reach, y: p.y + 0.45 + (dy / len) * reach };
  }
  return { x: p.x + p.face * 0.95, y: p.y + 0.25 + Math.sin(s.t * 2.2) * 0.06 };
}

function drawPlayer(ctx: CanvasRenderingContext2D, s: State, mouse: { x: number; y: number }) {
  const p = s.player;
  const sc = BASE_SCALE * s.zoom;
  const a = worldToScreen(s, p.x, p.y);

  // i-frame flash
  if (p.iframes > 0 && Math.floor(s.t * 14) % 2 === 0) ctx.globalAlpha = 0.35;

  const idle = p.grounded && Math.abs(p.vx) < 0.3 ? Math.sin(s.t * 2.4) * 0.02 : 0;
  const squash = 1 + p.landedImpact * 0.35 + idle;
  const stretch = (p.grounded ? 1 : 1 + Math.min(0.25, Math.abs(p.vy) / 40)) - idle;
  const bw = 0.62 * sc * squash;
  const bh = 0.6 * sc * stretch / squash;
  const bob = p.grounded ? Math.abs(Math.sin(p.walkPhase * 3)) * 3 : 0;
  const by = a.y - bob;

  ctx.save();
  ctx.translate(a.x, by);
  if (p.face < 0) ctx.scale(-1, 1);

  // legs (walk cycle)
  ctx.strokeStyle = PAL.skinDark; ctx.lineWidth = 7; ctx.lineCap = "round";
  const lk = p.grounded ? Math.sin(p.walkPhase * 3) * 0.35 : 0.15;
  ctx.beginPath(); ctx.moveTo(-6, 0.35 * sc); ctx.lineTo(-6 + lk * 14, 0.62 * sc); ctx.stroke();
  ctx.beginPath(); ctx.moveTo(7, 0.35 * sc); ctx.lineTo(7 - lk * 14, 0.62 * sc); ctx.stroke();

  // dress
  ctx.fillStyle = PAL.dress;
  ctx.beginPath();
  ctx.moveTo(-bw * 0.55, -bh * 0.1);
  ctx.quadraticCurveTo(0, -bh * 0.55, bw * 0.55, -bh * 0.1);
  ctx.lineTo(bw * 0.72, bh * 0.7);
  ctx.quadraticCurveTo(0, bh * 0.95, -bw * 0.72, bh * 0.7);
  ctx.closePath(); ctx.fill();
  ctx.fillStyle = PAL.dressDark;
  ctx.beginPath();
  ctx.moveTo(-bw * 0.72, bh * 0.7);
  ctx.quadraticCurveTo(0, bh * 0.95, bw * 0.72, bh * 0.7);
  ctx.lineTo(bw * 0.6, bh * 0.55);
  ctx.quadraticCurveTo(0, bh * 0.75, -bw * 0.6, bh * 0.55);
  ctx.closePath(); ctx.fill();

  // head
  const hy = -bh * 0.62;
  ctx.fillStyle = PAL.skin;
  ctx.beginPath(); ctx.arc(2, hy, 0.42 * sc, 0, 7); ctx.fill();
  // ear (big elephant ear behind)
  ctx.fillStyle = PAL.skinDark;
  ctx.beginPath(); ctx.ellipse(-0.28 * sc, hy - 2, 0.26 * sc, 0.34 * sc, -0.3, 0, 7); ctx.fill();
  ctx.fillStyle = PAL.cheek;
  ctx.beginPath(); ctx.ellipse(-0.24 * sc, hy + 1, 0.15 * sc, 0.2 * sc, -0.3, 0, 7); ctx.fill();
  // hair tuft
  ctx.fillStyle = "#4a3c60";
  ctx.beginPath(); ctx.ellipse(0, hy - 0.36 * sc, 0.3 * sc, 0.14 * sc, 0.15, 0, 7); ctx.fill();
  // eye
  ctx.fillStyle = "#222";
  const blink = Math.sin(s.t * 1.3 + 1) > 0.98 ? 0.15 : 1;
  ctx.beginPath(); ctx.ellipse(0.18 * sc, hy - 2, 3.2, 3.2 * blink, 0, 0, 7); ctx.fill();
  // cheek blush
  ctx.fillStyle = "rgba(242,162,176,0.8)";
  ctx.beginPath(); ctx.arc(0.3 * sc, hy + 5, 3.5, 0, 7); ctx.fill();

  ctx.restore();

  // trunk — drawn unmirrored in world space (aims at cursor when aiming)
  const tip = getTrunkTip(s);
  const tipS = worldToScreen(s, tip.x, tip.y);
  const baseS = { x: a.x + p.face * 0.3 * sc, y: by - 0.6 * sc * stretch / squash - 0.42 * sc * 0.1 };
  const baseHeadY = by + (-0.6 * sc * stretch / squash * 0.62) + 4;
  ctx.strokeStyle = PAL.skin; ctx.lineWidth = 9; ctx.lineCap = "round";
  ctx.beginPath();
  ctx.moveTo(a.x + p.face * 8, baseHeadY);
  const midX = (a.x + p.face * 8 + tipS.x) / 2;
  const midY = Math.max(baseHeadY, tipS.y) + 12;
  ctx.quadraticCurveTo(midX, midY, tipS.x, tipS.y);
  ctx.stroke();
  ctx.strokeStyle = PAL.skinDark; ctx.lineWidth = 3;
  ctx.beginPath(); ctx.arc(tipS.x, tipS.y, 4, 0, 7); ctx.stroke();

  ctx.globalAlpha = 1;

  // aim reticle
  const m = worldToScreen(s, mouse.x, mouse.y);
  ctx.strokeStyle = "rgba(255,255,255,0.75)"; ctx.lineWidth = 2;
  ctx.beginPath(); ctx.arc(m.x, m.y, 8, 0, 7); ctx.stroke();
  ctx.beginPath(); ctx.moveTo(m.x - 12, m.y); ctx.lineTo(m.x - 5, m.y); ctx.stroke();
  ctx.beginPath(); ctx.moveTo(m.x + 5, m.y); ctx.lineTo(m.x + 12, m.y); ctx.stroke();
  ctx.beginPath(); ctx.moveTo(m.x, m.y - 12); ctx.lineTo(m.x, m.y - 5); ctx.stroke();
  ctx.beginPath(); ctx.moveTo(m.x, m.y + 5); ctx.lineTo(m.x, m.y + 12); ctx.stroke();
}

function drawBoss(ctx: CanvasRenderingContext2D, s: State) {
  const boss = s.boss;
  if (boss.phase === "waiting") {
    // dozing on the far ledge: the arena should read "boss ahead", not "empty roof"
    const sc = BASE_SCALE * s.zoom;
    const a = worldToScreen(s, boss.x, boss.y - 0.25);
    const w = boss.w * sc, h = boss.h * sc;
    const breathe = 1 + Math.sin(s.t * 1.1) * 0.03;
    ctx.fillStyle = PAL.boss;
    ctx.beginPath(); ctx.ellipse(a.x, a.y, (w / 2) * 1.05, (h / 2) * 0.8 * breathe, 0, 0, 7); ctx.fill();
    ctx.fillStyle = PAL.bossDark;
    ctx.beginPath(); ctx.ellipse(a.x + w * 0.28, a.y - h * 0.18, w * 0.18, h * 0.24, 0.4, 0, 7); ctx.fill();
    // closed eye
    ctx.strokeStyle = "#2a2440"; ctx.lineWidth = 3;
    ctx.beginPath(); ctx.moveTo(a.x - w * 0.28, a.y - h * 0.1); ctx.lineTo(a.x - w * 0.14, a.y - h * 0.1); ctx.stroke();
    // trunk curled on the floor
    ctx.strokeStyle = PAL.boss; ctx.lineWidth = 11; ctx.lineCap = "round";
    ctx.beginPath();
    ctx.moveTo(a.x - w * 0.32, a.y + h * 0.05);
    ctx.quadraticCurveTo(a.x - w * 0.62, a.y + h * 0.3, a.x - w * 0.5, a.y + h * 0.36);
    ctx.stroke();
    // zzz
    ctx.fillStyle = "rgba(233,217,239,0.8)";
    ctx.font = "700 16px system-ui, sans-serif";
    ctx.textAlign = "center";
    const zt = (s.t % 2) / 2;
    ctx.globalAlpha = 1 - zt;
    ctx.fillText("z", a.x + w * 0.4 + zt * 10, a.y - h * 0.6 - zt * 18);
    ctx.globalAlpha = 1;
    return;
  }
  const sc = BASE_SCALE * s.zoom;
  const a = worldToScreen(s, boss.x, boss.y);
  const w = boss.w * sc, h = boss.h * sc;
  const dead = boss.phase === "dead";

  ctx.save();
  ctx.translate(a.x, a.y);
  if (boss.faceDir > 0) ctx.scale(-1, 1);
  if (dead) { ctx.rotate(2.4); ctx.translate(0, -h * 0.18); } // full belly-up flop

  const windup = boss.phase === "windup" ? Math.min(1, boss.timer / 0.35) : 0;
  // dodge telegraph: a readable crouch — he's about to hop out of your shot line
  const crouch = boss.dodgeTelegraph > 0 ? 1 : 0;
  if (crouch) ctx.transform(1.12, 0, 0, 0.82, 0, h * 0.09);

  // body
  ctx.fillStyle = boss.hitFlash > 0 ? "#fff" : (crouch ? "#9a8ffa" : PAL.boss);
  ctx.beginPath(); ctx.ellipse(0, 0, w / 2, h / 2, 0, 0, 7); ctx.fill();
  // belly
  ctx.fillStyle = boss.hitFlash > 0 ? "#eee" : PAL.bossDark;
  ctx.beginPath(); ctx.ellipse(0, h * 0.15, w * 0.32, h * 0.28, 0, 0, 7); ctx.fill();
  // ears
  ctx.fillStyle = PAL.bossDark;
  ctx.beginPath(); ctx.ellipse(w * 0.32, -h * 0.3, w * 0.2, h * 0.28, 0.4, 0, 7); ctx.fill();
  // eye (angry)
  ctx.fillStyle = dead ? "#222" : "#ff3b3b";
  ctx.beginPath(); ctx.arc(-w * 0.22, -h * 0.18, 6, 0, 7); ctx.fill();
  ctx.strokeStyle = "#2a2440"; ctx.lineWidth = 4;
  ctx.beginPath(); ctx.moveTo(-w * 0.34, -h * 0.3); ctx.lineTo(-w * 0.1, -h * 0.22); ctx.stroke();
  if (dead) {
    ctx.strokeStyle = "#222"; ctx.lineWidth = 3;
    ctx.beginPath(); ctx.moveTo(-w*0.28,-h*0.24); ctx.lineTo(-w*0.16,-h*0.12); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(-w*0.16,-h*0.24); ctx.lineTo(-w*0.28,-h*0.12); ctx.stroke();
  }
  // trunk (boss trunk swings)
  ctx.strokeStyle = PAL.boss; ctx.lineWidth = 12; ctx.lineCap = "round";
  const swing = dead ? 0.3 : Math.sin(s.t * (boss.phase === "chase" ? 6 : 2)) * 0.3 - windup * 0.9;
  ctx.beginPath();
  ctx.moveTo(-w * 0.3, -h * 0.05);
  ctx.quadraticCurveTo(-w * 0.62, h * 0.15, -w * 0.62 - windup * 14, h * 0.35 + swing * 18);
  ctx.stroke();
  // punch arm
  ctx.strokeStyle = PAL.bossDark; ctx.lineWidth = 10;
  ctx.beginPath();
  ctx.moveTo(-w * 0.2, h * 0.1);
  ctx.lineTo(-w * (0.4 + windup * 0.35), h * (0.3 - windup * 0.25));
  ctx.stroke();
  // legs
  ctx.strokeStyle = PAL.bossDark; ctx.lineWidth = 11;
  const trot = boss.phase === "chase" ? Math.sin(s.t * 9) * 6 : 0;
  ctx.beginPath(); ctx.moveTo(-w * 0.2, h * 0.4); ctx.lineTo(-w * 0.22 + trot, h * 0.58); ctx.stroke();
  ctx.beginPath(); ctx.moveTo(w * 0.2, h * 0.4); ctx.lineTo(w * 0.22 - trot, h * 0.58); ctx.stroke();
  ctx.restore();

  // hp bar above
  if (!dead) {
    ctx.fillStyle = "rgba(0,0,0,0.5)";
    roundRect(ctx, a.x - 60, a.y - h / 2 - 22, 120, 10, 5); ctx.fill();
    ctx.fillStyle = "#e0524f";
    roundRect(ctx, a.x - 58, a.y - h / 2 - 20, 116 * (boss.hp / boss.maxHp), 6, 3); ctx.fill();
    ctx.fillStyle = "rgba(255,233,201,0.9)";
    ctx.font = "700 11px system-ui, sans-serif";
    ctx.textAlign = "center";
    ctx.fillText("BIG GRUMP", a.x, a.y - h / 2 - 28);
  }
}

function drawHud(ctx: CanvasRenderingContext2D, s: State) {
  const p = s.player;
  // health bar
  ctx.fillStyle = "rgba(20,12,30,0.6)";
  roundRect(ctx, 16, 16, 220, 26, 13); ctx.fill();
  ctx.fillStyle = p.hp > 30 ? "#57d95a" : "#e0524f";
  if (p.hp > 0) { roundRect(ctx, 20, 20, 212 * (p.hp / p.maxHp), 18, 9); ctx.fill(); }
  drawHeartIcon(ctx, 30, 29, 7);
  ctx.fillStyle = "#fff";
  ctx.font = "700 13px system-ui, sans-serif";
  ctx.textAlign = "left";
  ctx.fillText(`${Math.ceil(p.hp)}`, 44, 33);
  // run timer
  if (s.runStarted) {
    ctx.fillStyle = "rgba(255,233,201,0.85)";
    ctx.font = "700 14px system-ui, sans-serif";
    ctx.textAlign = "right";
    ctx.fillText(fmtTime(s.runTime), VIEW_W - 20, 33);
    ctx.textAlign = "left";
  }
  // peanut tally
  ctx.fillStyle = "#e8b04a";
  ctx.beginPath(); ctx.ellipse(258, 29, 8, 5.6, 0.4, 0, 7); ctx.fill();
  ctx.fillStyle = "#ffe9c9";
  ctx.fillText(`${s.stats.peanuts}/${s.peanuts.length}`, 270, 33);

  // toast
  if (s.toast.timer > 0) {
    ctx.textAlign = "center";
    ctx.font = "800 26px system-ui, sans-serif";
    const alpha = Math.min(1, s.toast.timer * 2);
    ctx.fillStyle = `rgba(30,20,40,${alpha * 0.6})`;
    const w = ctx.measureText(s.toast.text).width + 36;
    roundRect(ctx, VIEW_W / 2 - w / 2, 64, w, 42, 12); ctx.fill();
    ctx.fillStyle = `rgba(255,233,201,${alpha})`;
    ctx.fillText(s.toast.text, VIEW_W / 2, 93);
  }

  // win banner
  if (s.won) {
    ctx.fillStyle = "rgba(20,12,30,0.55)";
    ctx.fillRect(0, 0, VIEW_W, VIEW_H);
    ctx.textAlign = "center";
    const allPeanuts = s.stats.peanuts === s.peanuts.length;
    ctx.fillStyle = "#ffd24a";
    ctx.font = "900 64px system-ui, sans-serif";
    ctx.fillText(s.grump ? "GRUMP TAMED!" : (allPeanuts ? "PERFECT WIN!" : "YOU WIN!"), VIEW_W / 2, VIEW_H / 2 - 30);
    if (allPeanuts) {
      ctx.font = "700 20px system-ui, sans-serif";
      ctx.fillStyle = "#e8b04a";
      ctx.fillText("every peanut found — the trunk is satisfied", VIEW_W / 2, VIEW_H / 2 - 90);
    }
    ctx.fillStyle = "#ffe9c9";
    ctx.font = "600 20px system-ui, sans-serif";
    const st = s.stats;
    ctx.fillText(`${fmtTime(s.runTime)} · shots ${st.shots} · launches ${st.launches} · deaths ${st.deaths} · peanuts ${st.peanuts}/${s.peanuts.length}`, VIEW_W / 2, VIEW_H / 2 + 14);
    const best = bestTime();
    if (best !== null) {
      ctx.font = "600 15px system-ui, sans-serif";
      ctx.fillStyle = s.runTime <= best ? "#ffd24a" : "#b9a6d9";
      ctx.fillText(s.runTime <= best ? "NEW BEST TIME!" : `best ${fmtTime(best)}`, VIEW_W / 2, VIEW_H / 2 + 76);
    }
    ctx.font = "500 12px system-ui, sans-serif";
    ctx.fillStyle = "rgba(185,166,217,0.7)";
    ctx.fillText("TRUNK! — a from-scratch rebuild of Team Ratateam's ElephantGame", VIEW_W / 2, VIEW_H - 24);
    ctx.font = "600 16px system-ui, sans-serif";
    ctx.fillText("press R to play again", VIEW_W / 2, VIEW_H / 2 + 48);
  }

  // death vignette
  if (p.dead) {
    ctx.fillStyle = "rgba(120,20,30,0.35)";
    ctx.fillRect(0, 0, VIEW_W, VIEW_H);
  }

  if (s.paused) {
    ctx.fillStyle = "rgba(20,12,30,0.6)";
    ctx.fillRect(0, 0, VIEW_W, VIEW_H);
    ctx.textAlign = "center";
    ctx.fillStyle = "#ffe9c9";
    ctx.font = "900 42px system-ui, sans-serif";
    ctx.fillText("PAUSED", VIEW_W / 2, VIEW_H / 2);
    ctx.font = "500 15px system-ui, sans-serif";
    ctx.fillStyle = "#b9a6d9";
    ctx.fillText("A/D move · mouse aims · click shoots · space rocket-jumps · E interacts · M music · shift+R restart", VIEW_W / 2, VIEW_H / 2 + 36);
  }
}

function drawHeartIcon(ctx: CanvasRenderingContext2D, x: number, y: number, r: number) {
  ctx.fillStyle = "#ff5a76";
  ctx.beginPath();
  ctx.moveTo(x, y + r * 0.6);
  ctx.bezierCurveTo(x - r * 1.2, y - r * 0.4, x - r * 0.5, y - r, x, y - r * 0.3);
  ctx.bezierCurveTo(x + r * 0.5, y - r, x + r * 1.2, y - r * 0.4, x, y + r * 0.6);
  ctx.fill();
}

function fmtTime(t: number): string {
  const m = Math.floor(t / 60), sec = t - m * 60;
  return `${m}:${sec.toFixed(1).padStart(4, "0")}`;
}

let cachedBest: number | null | undefined;
function bestTime(): number | null {
  if (cachedBest !== undefined) return cachedBest;
  try { const v = localStorage.getItem("trunk-best"); cachedBest = v ? Number(v) : null; }
  catch { cachedBest = null; }
  return cachedBest;
}
export function recordBest(t: number) {
  const b = bestTime();
  if (b === null || t < b) {
    try { localStorage.setItem("trunk-best", String(t)); } catch {}
    cachedBest = t;
  }
}

function roundRect(ctx: CanvasRenderingContext2D, x: number, y: number, w: number, h: number, r: number) {
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + w, y, x + w, y + h, r);
  ctx.arcTo(x + w, y + h, x, y + h, r);
  ctx.arcTo(x, y + h, x, y, r);
  ctx.arcTo(x, y, x + w, y, r);
  ctx.closePath();
}
