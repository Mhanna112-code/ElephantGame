// TRUNK! — browser shell: input, fixed-step loop, audio, trajectory preview.
import { makeLevel, step, DT, IDLE_INPUT, previewPath, type State, type Input } from "./game";
import { render, screenToWorld, getTrunkTip, worldToScreen, VIEW_W, VIEW_H, recordBest } from "./render";

const canvas = document.getElementById("game") as HTMLCanvasElement;
const ctx = canvas.getContext("2d")!;
canvas.width = VIEW_W; canvas.height = VIEW_H;

let state: State = makeLevel(new URLSearchParams(location.search).has("grump"));

// ---------- query params (test/screenshot hooks) ----------
const qp = new URLSearchParams(location.search);
if (qp.has("x")) { state.player.x = Number(qp.get("x")); state.player.y = Number(qp.get("y") ?? state.player.y); state.camX = state.player.x; state.camY = state.player.y + 2; }
if (qp.has("checkpoint")) { const i = Number(qp.get("checkpoint")); const c = state.checkpoints[i]; if (c) { state.player.x = c.x; state.player.y = c.y; state.checkpoint = { ...c }; state.camX = c.x; state.camY = c.y + 2; } }
const AUTOPLAY = qp.get("bot"); // bot=tutorial etc: replay scripted inputs for screenshots
let started = qp.has("x") || qp.has("checkpoint") || qp.has("demo"); // tests skip the title

// ---------- input ----------
const keys = new Set<string>();
let mouseScreen = { x: VIEW_W / 2, y: VIEW_H / 2 };
let shootQueued = false;
let spaceQueued = false;
let shootHeld = false;
let interactQueued = false;

window.addEventListener("keydown", (e) => {
  if (e.repeat) return;
  if (!started && (e.code === "Space" || e.code === "Enter")) { started = true; startFresh(); audio.resume(); audio.startMusic(); return; }
  if (!started && e.code === "KeyG" && wonBefore()) { started = true; startFresh(true); audio.resume(); audio.startMusic(); return; }
  if (e.code === "KeyM") audio.toggleMusic();
  keys.add(e.code);
  if (e.code === "KeyE") interactQueued = true;
  if (e.code === "Space") { spaceQueued = true; audio.resume(); e.preventDefault(); }
  if (e.code === "KeyP") state.paused = !state.paused;
  if (e.code === "KeyR" && (state.won || state.player.dead || e.shiftKey)) {
    state = makeLevel();
    window.__state = state;
    prev = { shots: 0, bounces: 0, launches: 0, deaths: 0, hp: 100, bossHp: state.boss.maxHp, won: false, toast: "", hearts: 0, leverOn: false, cp: "", peanuts: 0 };
  }
});
window.addEventListener("keyup", (e) => keys.delete(e.code));
canvas.addEventListener("mousemove", (e) => {
  const r = canvas.getBoundingClientRect();
  mouseScreen = { x: (e.clientX - r.left) * (VIEW_W / r.width), y: (e.clientY - r.top) * (VIEW_H / r.height) };
});
canvas.addEventListener("mousedown", (e) => {
  if (e.button === 0) {
    audio.resume();
    if (!started) { started = true; startFresh(); audio.startMusic(); return; }
    shootQueued = true; shootHeld = true;
  }
});

function wonBefore(): boolean {
  try { return localStorage.getItem("trunk-won") === "1"; } catch { return false; }
}

function startFresh(grump = false) {
  state = makeLevel(grump);
  window.__state = state;
  prev = { shots: 0, bounces: 0, launches: 0, deaths: 0, hp: 100, bossHp: state.boss.maxHp, won: false, toast: "", hearts: 0, leverOn: false, cp: "", peanuts: 0 };
}
window.addEventListener("mouseup", () => { shootHeld = false; });
canvas.addEventListener("contextmenu", (e) => e.preventDefault());
document.addEventListener("visibilitychange", () => { if (document.hidden && started && !state.won) state.paused = true; });

// ---------- audio (procedural, WebAudio) ----------
class Sfx {
  ctx: AudioContext | null = null;
  master: GainNode | null = null;
  resume() {
    if (!this.ctx) {
      this.ctx = new AudioContext();
      this.master = this.ctx.createGain();
      this.master.gain.value = 0.35;
      this.master.connect(this.ctx.destination);
    }
    if (this.ctx.state === "suspended") this.ctx.resume();
  }
  private blip(freq: number, dur: number, type: OscillatorType, vol = 1, slide = 0) {
    if (!this.ctx || !this.master || this.ctx.state !== "running") return;
    const t = this.ctx.currentTime;
    const o = this.ctx.createOscillator();
    const g = this.ctx.createGain();
    o.type = type;
    o.frequency.setValueAtTime(freq, t);
    if (slide !== 0) o.frequency.exponentialRampToValueAtTime(Math.max(30, freq + slide), t + dur);
    g.gain.setValueAtTime(vol * 0.5, t);
    g.gain.exponentialRampToValueAtTime(0.001, t + dur);
    o.connect(g); g.connect(this.master);
    o.start(t); o.stop(t + dur);
  }
  private noise(dur: number, vol = 1, low = false) {
    if (!this.ctx || !this.master || this.ctx.state !== "running") return;
    const t = this.ctx.currentTime;
    const buf = this.ctx.createBuffer(1, this.ctx.sampleRate * dur, this.ctx.sampleRate);
    const d = buf.getChannelData(0);
    for (let i = 0; i < d.length; i++) d[i] = (Math.random() * 2 - 1) * (1 - i / d.length);
    const src = this.ctx.createBufferSource();
    src.buffer = buf;
    const g = this.ctx.createGain();
    g.gain.value = vol * 0.4;
    if (low) {
      const f = this.ctx.createBiquadFilter();
      f.type = "lowpass"; f.frequency.value = 400;
      src.connect(f); f.connect(g);
    } else src.connect(g);
    g.connect(this.master);
    src.start(t);
  }
  shoot() { this.blip(520, 0.12, "square", 0.7, -300); this.noise(0.06, 0.4); }
  bounce() { this.blip(300, 0.08, "triangle", 0.8, 200); }
  launch() { this.blip(180, 0.25, "sawtooth", 0.7, 320); this.noise(0.12, 0.5, true); }
  hurt() { this.blip(160, 0.3, "sawtooth", 0.9, -80); this.noise(0.2, 0.7, true); }
  lever() { this.blip(700, 0.1, "square", 0.6, 200); this.blip(900, 0.08, "square", 0.5, 100); }
  spring() { this.blip(250, 0.3, "sine", 0.9, 500); }
  checkpoint() { this.blip(660, 0.12, "sine", 0.7); this.blip(880, 0.18, "sine", 0.7); }
  bossHit() { this.blip(220, 0.15, "square", 0.9, -60); this.noise(0.08, 0.6); }
  pickup() { this.blip(780, 0.1, "sine", 0.7, 240); }
  peanut() { this.blip(880, 0.07, "triangle", 0.6, 180); this.blip(1180, 0.09, "triangle", 0.5, 120); }
  dry() { this.blip(140, 0.08, "square", 0.5, -40); }
  win() { [523, 659, 784, 1046].forEach((f, i) => setTimeout(() => this.blip(f, 0.35, "triangle", 0.8), i * 130)); }
  death() { this.blip(300, 0.5, "sawtooth", 0.8, -240); }

  // ---- gentle chiptune loop (lookahead scheduler) ----
  private musicOn = false;
  private musicTimer: ReturnType<typeof setInterval> | null = null;
  private nextNote = 0;
  private stepIdx = 0;
  startMusic() {
    this.resume();
    if (this.musicOn || !this.ctx) return;
    this.musicOn = true;
    this.nextNote = this.ctx.currentTime + 0.1;
    const bass = [110, 110, 165, 110, 131, 131, 98, 98];   // A A E A C C G G
    const arp = [440, 523, 659, 523, 440, 587, 523, 494];
    this.musicTimer = setInterval(() => {
      if (!this.ctx || !this.musicOn) return;
      while (this.nextNote < this.ctx.currentTime + 0.25) {
        const i = this.stepIdx % 8;
        this.note(bass[i], this.nextNote, 0.22, "triangle", 0.16);
        if (this.stepIdx % 2 === 0) this.note(arp[i], this.nextNote, 0.12, "square", 0.05);
        this.nextNote += 0.24;
        this.stepIdx++;
      }
    }, 120);
  }
  toggleMusic() {
    if (this.musicOn) { this.musicOn = false; if (this.musicTimer) clearInterval(this.musicTimer); this.musicTimer = null; }
    else this.startMusic();
  }
  private note(freq: number, at: number, dur: number, type: OscillatorType, vol: number) {
    if (!this.ctx || !this.master || this.ctx.state !== "running") return;
    const o = this.ctx.createOscillator();
    const g = this.ctx.createGain();
    o.type = type; o.frequency.value = freq;
    g.gain.setValueAtTime(vol, at);
    g.gain.exponentialRampToValueAtTime(0.001, at + dur);
    o.connect(g); g.connect(this.master);
    o.start(at); o.stop(at + dur);
  }
}
const audio = new Sfx();

// audio triggers by diffing state
let prev = { shots: 0, bounces: 0, launches: 0, deaths: 0, hp: 100, bossHp: 160, won: false, toast: "", hearts: 0, leverOn: false, cp: "", peanuts: 0 };
function audioTick(s: State) {
  if (s.stats.shots > prev.shots) audio.shoot();
  if (s.stats.bounces > prev.bounces) audio.bounce();
  if (s.stats.launches > prev.launches) audio.launch();
  if (s.stats.deaths > prev.deaths) audio.death();
  else if (s.player.hp < prev.hp) audio.hurt();
  if (s.boss.hp < prev.bossHp) audio.bossHit();
  if (s.won && !prev.won) { audio.win(); recordBest(s.runTime, s.grump); try { localStorage.setItem("trunk-won", "1"); } catch {} }
  const hearts = s.hearts.filter(h => h.taken).length;
  if (hearts > prev.hearts) audio.pickup();
  if (s.stats.peanuts > prev.peanuts) audio.peanut();
  const lv = s.levers.some(l => l.on);
  if (lv !== prev.leverOn) audio.lever();
  const cp = `${s.checkpoint.x},${s.checkpoint.y}`;
  if (cp !== prev.cp && prev.cp !== "") audio.checkpoint();
  if (s.toast.text === "out of puff! land to reload" && s.toast.timer > 0.99) audio.dry();
  if (s.toast.text === "a spring appears!" && s.toast.text !== prev.toast) audio.spring();
  prev = { shots: s.stats.shots, bounces: s.stats.bounces, launches: s.stats.launches, deaths: s.stats.deaths, hp: s.player.hp, bossHp: s.boss.hp, won: s.won, toast: s.toast.text, hearts, leverOn: lv, cp, peanuts: s.stats.peanuts };
}

// ---------- loop ----------
let acc = 0;
let last = performance.now();
let attractT = 0;

function frame(now: number) {
  acc += Math.min(0.1, (now - last) / 1000);
  last = now;

  const mouseWorld = screenToWorld(state, mouseScreen.x, mouseScreen.y);
  while (acc >= DT && !started) {
    // attract mode: a little demo stroll behind the title card
    attractT += DT;
    const hop = attractT % 1.6 < DT * 1.5 && state.player.grounded;
    step(state, {
      ...IDLE_INPUT, right: true,
      aimX: state.player.x - 0.3, aimY: state.player.y - 2,
      shoot: hop, shootHeld: false,
    }, DT);
    if (state.player.x > 19 || state.player.dead) state = makeLevel();
    acc -= DT;
  }
  while (acc >= DT && started) {
    const input: Input = {
      left: keys.has("KeyA") || keys.has("ArrowLeft"),
      right: keys.has("KeyD") || keys.has("ArrowRight"),
      aimX: spaceQueued ? state.player.x : mouseWorld.x,
      aimY: spaceQueued ? state.player.y - 2 : mouseWorld.y,
      shoot: shootQueued || spaceQueued, shootHeld,
      interact: interactQueued, interactHeld: keys.has("KeyE"),
    };
    shootQueued = false; interactQueued = false; spaceQueued = false;
    step(state, input, DT);
    acc -= DT;
  }
  audioTick(state);

  try {
    paintOnce(mouseWorld);
    if (!started) drawTitle();
  } catch (e) {
    ctx.fillStyle = "#300";
    ctx.fillRect(0, 0, 960, 540);
    ctx.fillStyle = "#fff";
    ctx.font = "14px monospace";
    String(e && (e as Error).stack || e).split("\n").slice(0, 8).forEach((line, i) => ctx.fillText(line.slice(0, 110), 12, 30 + i * 20));
  }
  requestAnimationFrame(frame);
}

function drawTitle() {
  ctx.fillStyle = "rgba(20,12,30,0.55)";
  ctx.fillRect(0, 0, VIEW_W, VIEW_H);
  ctx.textAlign = "center";
  const t = performance.now() / 1000;
  ctx.save();
  ctx.translate(VIEW_W / 2, VIEW_H / 2 - 60 + Math.sin(t * 1.4) * 5);
  ctx.fillStyle = "#ffd24a";
  ctx.font = "900 92px system-ui, sans-serif";
  ctx.fillText("TRUNK", -22, 0);
  ctx.fillStyle = "#ff7e67";
  ctx.fillText("!", 178, 0);
  ctx.restore();
  ctx.fillStyle = "#ffe9c9";
  ctx.font = "600 19px system-ui, sans-serif";
  ctx.fillText("no jump button — recoil is the jump", VIEW_W / 2, VIEW_H / 2 + 4);
  ctx.fillStyle = `rgba(255,233,201,${0.6 + 0.4 * Math.sin(t * 3)})`;
  ctx.font = "800 24px system-ui, sans-serif";
  ctx.fillText("click to start", VIEW_W / 2, VIEW_H / 2 + 64);
  ctx.fillStyle = "#b9a6d9";
  ctx.font = "500 14px system-ui, sans-serif";
  ctx.fillText("A/D move · mouse aims the trunk · click shoots · E interacts · M music", VIEW_W / 2, VIEW_H / 2 + 100);
}

function paintOnce(mouseWorld: { x: number; y: number }) {
  render(ctx, state, mouseWorld);
  drawAmmoAndPreview(mouseWorld);
}

function drawAmmoAndPreview(mouseWorld: { x: number; y: number }) {
  const s = state, p = s.player;
  // trajectory preview (planning beats reflex-guessing)
  if ((shootHeld || p.aimActive > 0.2) && !p.dead && !s.won && p.ammo > 0) {
    const tip = getTrunkTip(s);
    const dx = mouseWorld.x - tip.x, dy = mouseWorld.y - tip.y;
    const len = Math.hypot(dx, dy) || 1;
    const path = previewPath(s, tip.x, tip.y, dx / len, dy / len);
    ctx.save();
    ctx.setLineDash([6, 8]);
    ctx.strokeStyle = "rgba(255,220,120,0.4)";
    ctx.lineWidth = 2;
    ctx.beginPath();
    for (let i = 0; i < path.length; i++) {
      const a = worldToScreen(s, path[i].x, path[i].y);
      if (i === 0) ctx.moveTo(a.x, a.y); else ctx.lineTo(a.x, a.y);
    }
    ctx.stroke();
    ctx.restore();
  }
  // ammo pips (peanuts) under health bar
  for (let i = 0; i < p.maxAmmo; i++) {
    const x = 24 + i * 22, y = 56;
    ctx.fillStyle = i < p.ammo ? "#e8b04a" : "rgba(255,255,255,0.15)";
    ctx.beginPath();
    ctx.ellipse(x, y, 8, 6, 0.4, 0, 7);
    ctx.fill();
    if (i < p.ammo) { ctx.fillStyle = "rgba(120,70,20,0.5)"; ctx.fillRect(x - 4, y - 1, 8, 2); }
  }
  if (!p.grounded && p.ammo === 0) {
    ctx.fillStyle = "rgba(255,120,120,0.9)";
    ctx.font = "700 12px system-ui, sans-serif";
    ctx.textAlign = "left";
    ctx.fillText("land to reload!", 96, 60);
  }
}

// ---------- headless bot mode (for screenshot tests) ----------
declare global { interface Window { __state: State; __step: (inp: Partial<Input>, seconds: number) => void; } }
window.__state = state;
window.__step = (inp, seconds) => {
  const n = Math.round(seconds / DT);
  for (let i = 0; i < n; i++) step(state, { ...IDLE_INPUT, ...inp }, DT);
  render(ctx, state, { x: state.player.aimX, y: state.player.aimY });
};

// canned demo states for screenshot review
const demo = qp.get("demo");
if (demo === "launch") {
  for (let i = 0; i < 140; i++) step(state, { ...IDLE_INPUT, right: true }, DT);
  step(state, { ...IDLE_INPUT, aimX: state.player.x, aimY: state.player.y - 2, shoot: true, shootHeld: true }, DT);
  for (let i = 0; i < 35; i++) step(state, { ...IDLE_INPUT, right: true, shootHeld: true, aimX: state.player.x + 5, aimY: state.player.y + 2 }, DT);
  state.player.aimActive = 1;
} else if (demo === "win") {
  state.won = true; state.player.x = 168; state.player.y = 46.5; state.camX = 168; state.camY = 48;
  for (let i = 0; i < 300; i++) step(state, IDLE_INPUT, DT);
} else if (demo === "boss") {
  state.player.x = 160; state.player.y = 46.5; state.camX = 163; state.camY = 48;
  for (let i = 0; i < 500; i++) step(state, { ...IDLE_INPUT, right: state.player.x < 163 }, DT);
  step(state, { ...IDLE_INPUT, aimX: state.boss.x, aimY: state.boss.y, shoot: true, shootHeld: true }, DT);
  for (let i = 0; i < 20; i++) step(state, { ...IDLE_INPUT, aimX: state.boss.x, aimY: state.boss.y, shootHeld: true }, DT);
  state.player.aimActive = 1;
}

// paint immediately so headless screenshots never race the rAF loop
if (!demo) for (let i = 0; i < 60; i++) step(state, IDLE_INPUT, DT); // settle the spawn
paintOnce({ x: state.player.aimX || state.player.x + 3, y: state.player.aimY || state.player.y + 1 });

requestAnimationFrame(frame);
