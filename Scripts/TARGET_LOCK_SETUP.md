# Target Lock — scene setup

How to wire the target / combination-lock / gate system into a Unity scene. The five
scripts (`SequenceLock`, `Target`, `TargetGroup`, `Gate`, `LevelManager`) are already in
`Scripts/`. `SequenceLock` is pure logic (no setup); the other four are components you place
in the scene. Nothing here needs code changes — it's all inspector wiring.

## How the pieces connect

```
LevelManager  ── activates one ──▶  TargetGroup  ── owns ──▶  SequenceLock (logic)
   (per scene)   group at a time        │  │
                                        │  └── drives colors of ──▶  Target  (the buttons)
                                        └── opens on solve ──────▶  Gate    (the barrier)
Bullet (BulletRicochet) ── on hitting a Target ──▶ TargetGroup.OnTargetHit()  (registers the hit)
```

Play flow per level: group 1 flashes its order → you shoot the buttons in that order (max 2s
between hits or it resets) → solving opens that group's gate → the next group activates → the
last group solving completes the level.

## Step by step

### 1. Targets (the red buttons)
- Each button is a GameObject with a **Renderer** (a cube/quad/sphere is fine).
- Add the **`Target`** component to each.
- Add a **Collider** to each (so bullets can hit it). A non-trigger collider is simplest.
- Colors have sensible defaults (red → blue on hit → green on solve). Override per-Target if
  you want.
- You do **not** set the order number here — the group assigns it (see below).

### 2. Target groups
- Create an empty GameObject per puzzle, e.g. `Group_1`. Add the **`TargetGroup`** component.
- Drag that group's `Target` objects into its **Targets** list.
- **Ordered** (checkbox): on = must be hit in sequence; off = just hit them all, any order.
- **Ordering the sequence** — two options:
  - Leave **Auto Sort By X = ON** (default): the required order becomes **left-to-right** by
    world X. Simplest for a wall of buttons.
  - For a custom order that is NOT left-to-right, turn **Auto Sort By X = OFF** and arrange the
    **Targets list** in the exact order you want (list index 0 = first, etc.).
- Drag the group's **Gate** into the `Gate` slot (step 3).

### 3. Gates (the barriers)
- Create the barrier GameObject (a wall/door) with a **Collider** (blocks the player) and a
  **Renderer**. Put it on a layer the player physically collides with.
- Add the **`Gate`** component. Leave **Start Closed = ON**.
- When its group solves, `Gate.Open()` disables the colliders + renderers so the player can pass.
- Bullets pass through gates (only the player is blocked) — that's intended, so you can shoot
  buttons that sit past a gate.

### 4. Level manager
- One empty GameObject per scene, e.g. `LevelManager`. Add the **`LevelManager`** component.
- Drag the `TargetGroup`s into its **Groups** list **in progression order** (Group_1 first).
- Only the first group is live at start; each solve activates the next. This is what prevents
  the "all groups accept hits at once" bug.
- Optional: set **Next Scene Name** to auto-load the next level when the last group solves
  (the scene must be in Build Settings). Leave blank to just log "level complete".

### 5. Bullets + player
- The bullet prefab needs **`BulletRicochet` + Rigidbody + Collider** (already the case).
- The player GameObject must be tagged **`Player`** (BulletRicochet/PlayerController look it up).
- Because targets have colliders, bullets ricochet off them and register the hit (they are NOT
  destroyed on a target — they keep bouncing, up to `maxBounces`). Recoil only fires on the
  first wall/floor a bullet hits (not on targets).

## Quick test checklist
1. Enter Play. Group 1's buttons should flash in order, then go dark, then turn solid red.
2. Shoot them in the flashed order. Each turns blue; the whole group turns green on the last hit.
3. That group's gate disappears; group 2 begins flashing.
4. Pause >2s mid-sequence → progress resets and it re-flashes. Hit out of order → red flash + reset.
5. Solve the last group → console logs `[LevelManager] All groups solved - level complete.`

## Gotchas
- A Target with no Renderer logs nothing but won't change color — give every button a Renderer.
- If bullets pass through buttons without registering, the button is missing a Collider or the
  bullet's collision layers don't include the target's layer.
- If a gate never blocks the player, it's on a layer the player doesn't collide with, or it has
  no non-trigger collider.
