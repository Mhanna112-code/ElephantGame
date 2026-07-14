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
    cooldown), leap attack periodically, contact damage with cooldown. 160 HP,
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
18. **Boss phase 2** — below 40% he spits volleys of hostile bouncing peanuts
    (your own mechanic, hostile: can't operate machinery, can't hurt him).
19. **Peanut collectibles** — 12 on optional routes; perfect-win ending at 12/12.
20. **Title card** — click-to-start front door doubles as the audio unlock; M
    toggles a procedural chiptune loop. Attract-mode demo plays behind it.
21. **Speedrun timer** — HUD clock, localStorage best time, NEW BEST celebration.
22. **GRUMP MODE** — unlockable second loop (G on title after any win): boss
    220hp/faster/angrier, player capped at 70hp. The challenge curve's answer
    to a player who has mastered the first clear.

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

## Appendix: original-source traceability (every Unity script → TRUNK!)

| Unity source | Idea | In TRUNK! |
|---|---|---|
| PlayerController.cs | A/D move, mouse aim, trunk-tip shooting | core loop; bullets spawn at trunk tip |
| BulletRicochet.cs | reflect ≤5 bounces, 0.8 speed, recoil impulse | same numbers; recoil radius-limited w/ falloff |
| TrunkSwing.cs | trunk follows cursor (IK) | drawn trunk aims at cursor, folds when idle |
| Climbing.cs (TrunkClimb) | E-anchor tether, range-clamped swing | grab rings with rope constraint + momentum |
| TrunkGrab.cs | spring-joint grapple prototype | folded into the ring tether |
| Lever.cs / ResetLever / HorizontalResetLever | trunk-flick levers toggle doors/platforms | lever (E or shoot) opens exit door; guard door |
| LeverSnapZone.cs | carry lever to slot, hidden door appears | plate reveals guarded lever (simplified chain) |
| BoxSnapZone.cs + Springs.fbx | box in zone reveals rising platform + spring bounce | plate press reveals spring pad + heart shelf |
| SlidingBox.cs | shoot box, slides to wall, floor-edge safe | push-box slides on shots, strong friction |
| MovingPlatform.cs | patrols, ricochet toggleable, red/green | movers with ricochet flag |
| FlashingPlatform.cs | timed red/green ricochet gates | flashing movers: tower gates, shaft covers, boss mirror |
| OneWayPlatform.cs / HighRisePlatform.cs | pass-below platforms, height-gated | one-way platforms (+ directional bullet rule) |
| WindZone.cs | upward impulse zone between walls | pulsing wind shaft (on/off cycle) |
| MinecartInteraction / MovingBoxSpline / Rails1.fbx | E to ride spline cart, wheels, tilt | polyline-rail cart over spike alley, launch exit |
| SpikeDamage.cs / Spike.fbx | 20 dmg + invincibility flash | spike strips + spike balls, 1.2s i-frames |
| Health.cs | 100hp, bar UI, flash, boss-collision ignore | hp bar, damage flash, i-frames |
| Checkpoint / CheckpointManager / Deathzone | respawn points, kill floor | checkpoint flags, death pit, 0.6s respawn |
| DodgeBullets.cs (+ toad.controller) | floating critter that dodges bullets | dodger toads (hover, sidestep, 2hp) |
| BossBehavior / BossFightController / BossHealth | intro leap, chase, punch windup, leap attack, contact dmg, 100hp | boss: intro, chase, telegraphed windup, predictive leap, contact dmg, 160hp, enrage |
| BossCutsceneCamera.cs | zoom-out on boss entrance | camera zoom 0.72x during intro |
| CutSceneCamera.cs (top-down section) | camera mode switch | deliberately cut (see failure table) — depth kept via altitude change instead |
| GamePause.cs | pause | P key |
| FollowCamera.cs (ours, from the fix night) | camera must not inherit player rotation | camera is code-only, lerp + look-ahead |
| StickFront.cs | rig helper | n/a (no 3D rig) |

## Final fun audit (against the formula, with evidence)

- **Satisfying core loop** — shoot→bounce→launch reads in under a second with
  hit-stop, screenshake, launch sfx, squash & stretch. Verified: honest bot
  chains launches across the whole map; every beat screenshot-reviewed.
- **Interesting decisions** — 3-shot air economy makes every launch a spend
  (chain higher vs keep a rescue shot); boss fight trades frontal spam (dodged)
  vs bank shots (always land) vs closing distance (contact risk).
- **Uncertainty, right kind** — flashing gates on readable timers with countdown
  pips; boss dodge only after a visible crouch; leaps lead your movement.
  No coin-flip outcomes anywhere.
- **Challenge curve** — measured, not vibes: casual sim crosses beat 2 with
  median 1 death; honest mid-skill full run finishes with 0 route deaths and
  beats the boss at 31/100 hp (94 damage absorbed across the run). Phase 2
  arrives exactly when the player has mastered launching.
- **Juicy feedback** — 13 procedural sfx + music loop, hit-stop, shake budget,
  particles for every event, i-frame flash, perfect-win golden ending.
- **Chosen pleasure** — kinetic mastery (Downwell lineage): the same input that
  solves combat solves traversal, and both get faster with skill.
