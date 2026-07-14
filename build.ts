// Bundle the game into a single self-contained dist/index.html.
const result = await Bun.build({ entrypoints: ["./src/main.ts"], target: "browser", minify: true });
if (!result.success) {
  for (const log of result.logs) console.error(log);
  process.exit(1);
}
const js = await result.outputs[0].text();
const html = `<meta charset="utf-8">
<title>TRUNK! — an elephant with a plan</title>
<style>
  html,body{margin:0;height:100%;background:#1a1230;display:flex;align-items:center;justify-content:center;flex-direction:column;font-family:system-ui,sans-serif}
  canvas{max-width:100vw;max-height:88vh;aspect-ratio:16/9;border-radius:8px;box-shadow:0 12px 60px rgba(0,0,0,.55);cursor:crosshair}
  .bar{color:#b9a6d9;font-size:13px;padding:10px;text-align:center;line-height:1.6}
  .bar b{color:#ffd24a}
</style>
<canvas id="game"></canvas>
<div class="bar"><b>A/D</b> move · <b>mouse</b> aim trunk · <b>click</b> shoot (shots bounce — bounces near you LAUNCH you; that's your jump) · <b>E</b> ride carts / grab rings / flick levers · <b>P</b> pause · <b>R</b> retry<br>3 shots per flight — land, grab, or bounce off green to reload. Reach the top. Defeat the boss.</div>
<script>${js.replace(/<\/script>/g, "<\\/script>")}</script>
`;
await Bun.write("dist/index.html", html);
console.log(`dist/index.html: ${(html.length / 1024).toFixed(1)} KB`);
