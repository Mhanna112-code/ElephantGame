# TRUNK! — ElephantGame rebuilt

## Spirit statement

You are an elephant girl whose trunk is a multitool. It is your gun, your jump,
your key, and your hands. Bullets ricochet off the world; every bounce near you
shoves you through the air — the game has no jump button, **recoil is the jump**.
The journey: escape a high ledge, ride a minecart, scale a tower field, solve a
lever-and-box room, surf a wind shaft, and duel a boss who dodges your shots.

This is a redesign, not a port. The Unity original's ideas are kept; its
failure modes are designed out.

## What broke the original, and the redesign rule that prevents it

| Original failure | Redesign rule |
|---|---|
| Facing fought the animator (5 stacked bugs) | Facing = sign of velocity.x, one line, no animator |
| Trunk-aim read as "facing backwards" | Trunk aims at cursor ONLY while cursor moved recently or button held; otherwise folds forward. Facing is carried by the whole body sprite, drawn unambiguous |
| Mode switches keyed off height checks (IsAbovePlatform) fired mid-flight | All mode switches are explicit trigger zones, never height comparisons |
| 3D physics fighting scripts (camera child of player, rotation constraints) | Pure 2D AABB physics, no rotation anywhere, camera is code not hierarchy |
| Top-down z-section camera gimmick (source of confusion) | Cut. One camera, one plane, always |
| Global recoil from any bounce anywhere | Recoil only from bounces within a radius, falloff with distance — readable cause/effect |
| Verified nothing until players screamed | Bot playtests + screenshot review are part of the build loop |

## Core loop

move (A/D) → aim trunk (mouse) → shoot (click) → bullet ricochets (≤5 bounces,
0.8 speed retained) → nearby bounces impulse the player away from the surface →
chain launches to reach places → spend health on mistakes → checkpoints forgive.

Feel targets: coyote time 0.1s, air control full, recoil launch ≈ 2.5x walk
speed vertical, i-frames 1.2s with sprite flash, camera lerp-follow with
look-ahead.

## Mechanics (all from the original, redesigned)

1. **Ricochet shot** — reflect off surfaces, speed×0.8 per bounce, max 5.
2. **Recoil launch** — impulse away from bounce surface if bounce within 6u of
   player, scaled by bullet speed and 1/distance. Ground-shot = rocket jump.
3. **Trunk aim** — IK-free: trunk is a drawn curve from face to aim point,
   folds to a forward rest pose when idle/airborne-unaimed.
4. **Levers** — flick by touching with trunk tip (hold near it) or shooting
   them; toggle doors/platforms. Green/red state color.
5. **Flashing platforms** — cycle red/green; bullets bounce off green, die on
   red. Used as timing gates for ricochet routes.
6. **One-way platforms** — pass from below, solid from above.
7. **Spikes / spike balls** — 20 damage + knockback, i-frames after.
8. **Minecart** — press E near cart to ride a fixed rail; duck/lean is free
   movement inside the cart; spikes on the route; jump out with a recoil shot.
9. **Wind shaft** — upward force zone between two walls; steer while floating,
   red/green platforms block/allow the climb.
10. **Boss** — intro jump cutscene, then chase. Punch when close (windup +
    cooldown), leap attack periodically, contact damage with cooldown. 100 HP,
    takes bullet hits, sidesteps ~40% of incoming bullets (the DodgeBullets
    spirit). Death = win screen.
11. **Health/checkpoints** — 100 HP, damage flash, deathzones below level,
    respawn at last checkpoint flag. Heart pickups restore 25.
12. **Dodger toads** (DodgeBullets.cs was a separate floating creature, not the
    boss) — hovering critters that sidestep incoming bullets; 2 hits to pop,
    light contact damage. The boss inherits a weaker version of the dodge.
13. **Spring pad** (BoxSnapZone + Springs.fbx) — completing the box puzzle
    reveals a hidden spring platform that bounces the player high; rewards a
    heart shelf.
14. **Trunk tether** (TrunkClimb.cs) — hold E near a grab ring to anchor the
    trunk; rope-constrained swing with preserved momentum on release. Rings
    offer an alternate swing route across the tower field and wind shaft.
15. **Boss intro cutscene zoom** (BossCutsceneCamera) — camera widens during
    the boss's entrance leap, returns on fight start.
16. **Pause** (GamePause) — P toggles.
17. Bullets spawn from the trunk tip (issue #33's intent), not the body.

## Level beats (left-to-right this time, ~8 screens)

1. **Ledge** — safe area, signs teach move/aim/shoot/recoil-jump.
2. **Gap field** — recoil jumps over deathzone gaps, first checkpoint.
3. **Spike alley + minecart** — board cart, ride over spike floor, bail at end.
4. **Tower field** — one-way platforms up staggered towers, spike balls
   between, ricochet corridor (green flashing platform timing).
5. **Puzzle room** — locked door; lever behind a box; shoot box to slide it
   onto pressure plate, flick lever with trunk, door opens.
6. **Wind shaft** — updraft between walls, steer past spike balls; flashing
   platforms as moving cover.
7. **Boss arena** — flat top, boss intro leap, duel. Win banner.

## Tech

- TypeScript, strict. Two files: `src/game.ts` (pure: types, step(state,input,dt),
  no DOM) and `src/render.ts` + `src/main.ts` (canvas 2D, input, loop).
- Bun for typecheck/tests/bundling into one self-contained `dist/index.html`.
- Deterministic step (fixed dt accumulator) → bot playtests in Bun assert:
  tutorial reachable, every checkpoint reachable by scripted inputs, boss
  killable, no softlock (stuck-position watchdog), death/respawn works.
- Firefox --headless --screenshot for render-and-look at key moments
  (?scene=N&t=S query params jump the game to a state for screenshots).
- Playable artifact published for the morning.

## Visual direction

Flat-color vector look drawn in canvas: warm sky gradient, silhouette hills
parallax, chunky platforms with grass lips, elephant girl as layered shapes
(round body, skirt, ears, expressive trunk curve, blink), fat soft-edged
bullets with bounce sparks, screenshake on recoil, squash/stretch on land.
Palette: dusk oranges/purples + teal accents (jam-poster energy).

## Fun audit (round 2 — critique folded in)

Fun = satisfying core loop + interesting decisions + right-kind uncertainty +
challenge curve + juicy feedback, in service of a chosen pleasure: **kinetic
mastery** (Downwell lineage).

1. **Shot economy** — 3 trunk-shots while airborne; grounded = infinite (and
   refills). Green-platform bounces and ring grabs also refill. Airborne
   ammo = the interesting decision: the shot that hits is rarely the shot
   that leaves you standing where you want.
2. **Develop, don't just teach** — flashing ricochet timing is a vocabulary:
   tower gate (teach), double-phase gates (develop), wind-shaft moving covers
   (twist), boss-arena bank-mirrors (conclude: they're how you beat the dodge).
   Wind pulses on/off so updraft floating alone stalls — recoil chains keep you
   climbing. Shooting stays live during the cart ride (toads + pickups on the
   route).
3. **Readable boss dodge** — boss sidesteps only bullets entering his facing
   arc, after a visible crouch telegraph, on cooldown. Shots banked off the
   arena mirrors arrive from behind and always land. Skill you defeat, not a
   dice roll you endure.
4. **Juice** — WebAudio procedural SFX (shoot/bounce/launch/hurt/lever/spring/
   checkpoint/boss-hit/win), hit-stop (30-80ms by impact), screenshake budget,
   squash & stretch.
5. **Trajectory preview** — faint one-bounce ghost line while aiming: planning,
   not reflex-guessing.
6. **Restart < 1s** — death fade 0.6s, respawn with i-frames, zero menus.
7. Bot tests prove solvability; screenshots + feel targets guard fun. Known
   limit: green tests ≠ fun — that's what the feel numbers and juice pass are for.
