// Bundle the game into a single self-contained dist/index.html.
const result = await Bun.build({ entrypoints: ["./src/main.ts"], target: "browser", minify: true });
if (!result.success) {
  for (const log of result.logs) console.error(log);
  process.exit(1);
}
const js = await result.outputs[0].text();
const html = `<meta charset="utf-8">
<title>TRUNK!</title>
<style>
  :root{--ink:#1a1230;--dusk:#2b1d4f;--peach:#e8875a;--gold:#ffd24a;--lilac:#b9a6d9;--rose:#ff7e67}
  html,body{margin:0;min-height:100%;background:linear-gradient(180deg,var(--ink),#241740 70%,#2d1a3e);display:flex;align-items:center;justify-content:center;flex-direction:column;font-family:system-ui,sans-serif;gap:14px;padding:18px 0}
  h1{margin:0;font-size:clamp(28px,4vw,44px);font-weight:900;letter-spacing:.06em;color:var(--gold);text-shadow:0 3px 0 rgba(0,0,0,.35)}
  h1 .bang{color:var(--rose)}
  .sub{margin:-8px 0 0;color:var(--lilac);font-size:13px;letter-spacing:.14em;text-transform:uppercase}
  canvas{max-width:min(96vw,1200px);max-height:78vh;aspect-ratio:16/9;border-radius:10px;box-shadow:0 16px 70px rgba(0,0,0,.6),0 0 0 1px rgba(185,166,217,.18);cursor:crosshair}
  .chips{display:flex;flex-wrap:wrap;gap:8px;justify-content:center;max-width:900px;padding:0 16px}
  .chip{background:rgba(185,166,217,.1);border:1px solid rgba(185,166,217,.25);color:var(--lilac);font-size:12.5px;padding:5px 11px;border-radius:999px;line-height:1.5}
  .chip b{color:var(--gold);font-weight:800}
  .lore{color:#8d7bb0;font-size:12px;text-align:center;max-width:720px;line-height:1.7;padding:0 16px}
  .lore em{color:var(--peach);font-style:normal;font-weight:700}
</style>
<h1>TRUNK<span class="bang">!</span></h1>
<p class="sub">no jump button · recoil is the jump</p>
<canvas id="game"></canvas>
<div class="chips">
  <span class="chip"><b>A/D</b> move</span>
  <span class="chip"><b>mouse</b> aim trunk</span>
  <span class="chip"><b>click</b> shoot — bounces near you launch you</span>
  <span class="chip"><b>space</b> shot at your feet (rocket jump)</span>
  <span class="chip"><b>E</b> carts · rings · levers</span>
  <span class="chip"><b>3</b> shots per flight — land or bounce off <b>green</b> to reload</span>
  <span class="chip"><b>P</b> pause</span>
  <span class="chip"><b>R</b> retry</span>
</div>
<p class="lore">Ride the cart. Climb the towers. Solve the box room. Surf the wind shaft. <em>Bank shots behind the boss — he reads the ones aimed at his face.</em></p>
<script>${js.replace(/<\/script>/g, "<\\/script>")}</script>
`;
await Bun.write("dist/index.html", html);
console.log(`dist/index.html: ${(html.length / 1024).toFixed(1)} KB`);
