# Architecture

A Unity 6 (6000.3.7f1) 2D arena game. This page says where code goes and which way
dependencies may point. Keep it short; update it when a rule changes.

## Layout

| Folder | Namespace | Assembly | What lives there |
| --- | --- | --- | --- |
| `Assets/Scripts/World/` | `WizardArena.World` | `WizardArena.Runtime` | Arena geometry (`ArenaSurface`, `ArenaBounds`) |
| `Assets/Scripts/Combat/` | `WizardArena.Combat` | `WizardArena.Runtime` | `Health`, `IDamageable`, `DamageInfo`, `Team`, `Projectile` + `ProjectileLauncher` |
| `Assets/Scripts/Player/` | `WizardArena.Player` | `WizardArena.Runtime` | The wizard: `WizardController` composes `PlayerMotor`, `PlayerCaster`, `PlayerAnimator`, `PlayerHitFeedback`, `PlayerTransitions`; input via `IPlayerInput` |
| `Assets/Scripts/Enemies/` | `WizardArena.Enemies` | `WizardArena.Runtime` | Enemy framework (`Enemy`, `EnemyConfig`, `EnemyRegistry`, `ContactDamage`, states) and enemy types (`BatEnemyController`) |
| `Assets/Scripts/Hazards/` | `WizardArena.Hazards` | `WizardArena.Runtime` | Environmental hazards (`SpikeHazard`, `ExplosiveHazard`) as self-contained prefabs |
| `Assets/Scripts/Pickups/` | `WizardArena.Pickups` | `WizardArena.Runtime` | Pickups (`Pickup`, `IPickupEffect`, `HealEffect`, `PickupRegistry`) as self-contained prefabs |
| `Assets/Scripts/Stage/` | `WizardArena.Stage` | `WizardArena.Runtime` | Stage flow, portal, game state and progression (`GameSession`, `StageManager`, `StagePortal`, `StageRunner`, `StageDefinition`, `StageSequence`) |
| `Assets/Scripts/UI/` | `WizardArena.UI` | `WizardArena.Runtime` | HUD, enemy HP bars, screens and the main menu (UI Toolkit); UXML/USS under `Assets/UI/` |
| `Assets/Scripts/Feedback/` | `WizardArena.Feedback` | `WizardArena.Runtime` | Hit-stop, camera shake, hit flash and particle bursts (added in [14]; no sound yet) |
| `Assets/Editor/` | `WizardArena.EditorTools` | `WizardArena.Editor` (Editor only) | Scene builder, menu items |
| `Assets/Tests/EditMode/<Area>/` | `WizardArena.Tests.EditMode.<Area>` | `WizardArena.Tests.EditMode` | Fast logic tests, no scene |
| `Assets/Tests/PlayMode/<Area>/` | `WizardArena.Tests.PlayMode.<Area>` | `WizardArena.Tests.PlayMode` | Tests that run frames or load the scene |

A new area gets a folder under `Assets/Scripts/` and the matching namespace. All runtime
code shares one assembly for now (simple, fast to compile); the dependency rules below are
kept by review. If they start to slip, split the areas into their own asmdefs.

## Dependency direction

```
            UI      Feedback          (listen only; nothing depends on them)
              \      /
               Stage                  (game flow: places hazards/pickups, listens to Player/Enemies/Pickups events)
              /  |   \
         Player  |  Enemies           (never reference each other's concrete classes)
              \  |   /
         Hazards   Pickups            (spikes, explosive crystal/barrel, potions -- never Player/Enemies/Stage)
              \      /
              Combat                  (health, damage, projectiles)
                 |
               World                  (surfaces)
```

- A layer may use the layers **below** it, never the ones above.
- **Combat** knows nothing about Player, Enemies or Hazards. It defines small interfaces
  (e.g. something that can take damage) that they implement or call into.
- **Hazards** only use Combat/World (`Health`, `DamageInfo`, `Team.Neutral`). They never
  reference Player, Enemies or Stage, so a spike or an explosion hurts anyone through the same
  damage API a projectile uses. They are built as self-contained prefabs and placed by Stage.
- **Pickups** only use Combat (`Health`, `Team`). They never reference Player, Enemies or
  Stage, so anything touching one is judged the same way a hazard judges its targets. They are
  built as self-contained prefabs, placed either by Stage (`StageDefinition.PropSpawns`) or
  instantiated at runtime by an Enemies-side `EnemyDropper` (Enemies may use Pickups: it sits
  in the same row as Hazards, below Enemies). `PickupRegistry` is how Stage learns about a
  pickup an `EnemyDropper` spawned, the same way `EnemyRegistry` lets Stage learn about enemies.
- **Player and Enemies** do not reference each other. An enemy that needs a target gets it
  through a Combat interface or a serialized reference set by the scene builder.
- **Stage** reacts to events (enemy defeated, player defeated, pickup spawned) instead of polling.
- **UI and Feedback** subscribe to gameplay events. Gameplay code never calls UI or Feedback.
- **Editor** and **Tests** may reference anything in `WizardArena.Runtime`.

## Rules

- **Events over lookups.** Expose C# events (`public event Action<...> Died;`) and a few
  public methods. No `FindObjectsByType`, `FindFirstObjectByType` or `GameObject.Find` in
  gameplay code; use serialized references, events, or a small registry like
  `ArenaSurface.Active`. The scene builder and tests may use lookups.
- **Small public API per area.** Interfaces, events and a few methods are public; the rest is
  `private` or `internal`. Other areas do not reach into internals.
- **Config in ScriptableObjects.** Tunable numbers (speeds, health, damage, timings) live in
  ScriptableObject assets, one config class per area in that area's namespace, assets under
  `Assets/Config/<Area>/`. No magic numbers in behaviour code.
- **Scenes are generated.** `Assets/Scenes/WizardMovement.unity` (the arena) and
  `Assets/Scenes/MainMenu.unity` (the title screen, `Assets/Editor/MenuSetup.cs`) are both built
  by `WizardArena.EditorTools.SceneBuilder` (menu **Wizard Prototype > Rebuild Demo Scene**, or
  `Tools/unity/unity.sh rebuild-scene`). To change either scene, change its builder
  (`Assets/Editor/`), regenerate, and commit the scene. Never hand-edit scene YAML.
  The builders' automatic on-load hooks are skipped in batch mode, so compile/test runs never
  change either scene. Build Settings list `MainMenu` first, then `WizardMovement`
  (`SceneBuilder.SyncBuildSettings`, called by both builders so either can run alone and still
  end up with the correct full list).
- **Script GUIDs are sacred.** When moving a script, move its `.meta` with it (`git mv` both),
  or the scene loses the component.

## Combat

- Anything that can be hurt implements `IDamageable` (usually by having a `Health`). Owners
  subscribe to `Health.Damaged` / `Died` for visuals and movement; `Health` does neither.
- `Team` decides who can hurt whom: same side is ignored, `Neutral` hurts everyone.
- Projectiles are fired with `ProjectileLauncher.Fire`. Each frame a projectile sweeps a circle
  (`Physics2D.CircleCast`, triggers included) from its last to its new position and damages the
  first `IDamageable` (found with `GetComponentInParent`) that accepts the hit. A target only
  needs a `Collider2D`; no rigidbody or physics layers are required.

## Player

- `WizardController` is a thin composer. It reads `IPlayerInput` once per frame and ticks its
  parts in a fixed order (cast start, motor, hit feedback, caster, animator). The parts have no
  `Update` of their own, so the frame order never depends on Unity's script order.
- Input comes from any `IPlayerInput` component (`KeyboardPlayerInput` in the scene) or
  `WizardController.UseInput(...)`, which tests use to script the wizard.
- `PlayerTransitions` owns appear/vanish and turns `Health` off whenever the wizard is not in play.
- `WizardController.Respawn(position)` moves the wizard to a stage's spawn point, revives its
  `Health` and replays the appear transition (`PlayerTransitions.Respawn`) from any state --
  used by `StageRunner` when a stage starts or restarts.

## Enemies

- Every enemy has `Health` + `Enemy` (+ usually `ContactDamage`) and one `IEnemyBehaviour`
  component that creates its main state (the bat: `BatEnemyController` -> flying wander).
  `Enemy` runs a tiny `EnemyStateMachine`: main state, then the shared `HurtState` on every
  accepted hit, then back to main or on to `DeadState` (fall, land, hold, fade, destroy).
- Numbers and sprites come from an `EnemyConfig` asset in `Assets/Config/Enemies/`.
- `EnemyRegistry` (static events `Spawned`/`Died`, `AliveCount`) is how other layers learn about
  enemies. An enemy counts from `Awake` until its Health dies (not until the corpse is gone);
  a deactivated enemy still counts.
- Enemies move inside `ArenaBounds.Active.Area` (placed by the scene builder) and land on the
  highest walkable `ArenaSurface` below them (`ArenaSurface.TryGetGroundBelow`).
- `ContactDamage` hurts any `Health` of a hostile team its circle touches. No target reference.
- Ground-walking enemy types ([11]: `BlueSlimeController` -> `SlimeState`, `SkeletonWarriorController`
  -> `SkeletonState`) share `GroundMotion`, a small helper that walks along `ArenaSurface` floors
  and stairs using the same physics as the player (`ArenaSurface.Move`, gravity, stepping up small
  stairs) and reports back when a ledge or a wall should turn the state around -- checked every
  step regardless of whether the enemy is airborne, so a slime's hop cannot drift it past a ledge.
- `PlayerSensor.TryFind(from, radius, out Health)` finds the nearest `Team.Player` `Health` with a
  physics query, so a ground state can react to the wizard without ever referencing Player code.
- Their prefabs live under `Assets/Prefabs/Enemies/` (`BlueSlime.prefab`, `SkeletonWarrior.prefab`),
  built by `Assets/Editor/GroundEnemySetup.cs`. Not wired into the demo scene yet -- [10]'s
  `StageDefinition` spawn lists instantiate them.
- `EnemyConfig.Drops` is a list of `DropEntry` (a prefab + a chance in [0, 1]), rolled
  independently on death by `EnemyDropper` (`[RequireComponent(Enemy)]`, added alongside the
  other components on each enemy prefab). It instantiates whatever rolls at the enemy's death
  position, snapped to the ground below via `ArenaSurface.TryGetGroundBelow` if there is one --
  same lookup `DeadState` uses for the corpse. It never references what it drops (e.g. Pickups):
  it only ever `Instantiate`s a generic prefab, so Enemies stays within ARCHITECTURE.md's
  dependency direction (a layer may use anything below it) without a compile-time dependency on
  the dropped item's own area.

## Hazards

- Self-contained prefabs under `Assets/Prefabs/Hazards/`, built by `Assets/Editor/HazardSetup.cs`
  (menu **Wizard Prototype > Build Hazard Prefabs**, or headless via `-executeMethod
  WizardArena.EditorTools.HazardSetup.BuildPrefabsFromCommandLine`). They use only Combat/World
  (`Health`, `DamageInfo`, `Team.Neutral`) and never reference Player, Enemies or Stage, so the
  same hazard hurts the wizard or an enemy through the same damage API a projectile uses.
- `SpikeHazard`: a `Collider2D` trigger area that damages any `Health` touching it
  (`SpikeHazardConfig`: `Damage`, `CooldownSeconds`). It tracks its own per-target cooldown
  (a `Dictionary<Health, float>`) independent of that `Health`'s invulnerability window, so it
  does not re-hit the same target every frame but still hits a different target immediately.
- `ExplosiveHazard`: has a small `Health` (`Team.Neutral`, 1 HP) so a single bolt or hit pops it;
  on `Health.Died` it deals one area hit (`ExplosiveHazardConfig`: `Damage`, `Radius`) to every
  `Health` in range exactly once (enemies included -- shooting it into a crowd is worth it),
  plays a short flash/scale, then destroys itself. Two prefabs (`ExplosiveCrystal`,
  `ExplosiveBarrel`) share the one component with different art.
- Placed per stage via `StageDefinition.PropSpawns` (`StageSetup.SetPropSpawns`, the same
  idempotent-overwrite pattern as `SetEnemySpawns`): a few spikes/explosive props on the floor
  in each of the three stages, positioned to be a choice (route around, or shoot) rather than
  unavoidable, and close enough to a ground enemy in stages 2-3 that an explosion can catch it.
- `ExplosiveHazard.Exploded` (static `event Action<Vector3>`, raised once per explosion, right
  before the flash/shrink coroutine): the one thing Hazards exposes upward, so Feedback ([14])
  can react to any explosion without holding a reference to a specific hazard -- the same shape
  as `EnemyRegistry.Spawned`/`Died`.

## Feedback

- `FeedbackDirector` (one per scene, on the "Feedback" `GameObject`) is the only thing that
  listens for hit feel: the player's `Health` (serialized, wired by `FeedbackSetup`),
  `EnemyRegistry.Spawned` (to hear every enemy that shows up) and
  `ExplosiveHazard.Exploded`. Nothing in Player/Enemies/Hazards calls into Feedback; it only
  subscribes to events they already raise for other reasons.
- On `EnemyRegistry.Spawned` it adds `EnemyFeedbackLink` to that enemy -- a tiny component that
  subscribes to the enemy's own `Health.Damaged`/`Died` in `OnEnable` and unsubscribes in
  `OnDisable` (which Unity also calls right before the enemy's `GameObject` is destroyed, so a
  death and a plain stage-transition `Destroy` both clean up the same way). It plays the white
  hit flash on the enemy's own `SpriteRenderer` and asks `FeedbackDirector` for hit-stop and a
  particle burst.
- **Hit-stop** is a brief `Time.timeScale` dip (`FeedbackConfig.HitStopSeconds`/`HitStopTimeScale`,
  real/unscaled seconds) shared by every enemy hit and every player hurt. Coordination with the
  Pause menu (`PauseMenuController`, the only other writer of `Time.timeScale`): the dip never
  starts while `Time.timeScale` is already 0 (paused), and when it finishes it restores
  `Time.timeScale` to 1 only if `Time.timeScale` is still exactly the dip value it set -- if Pause
  changed it meanwhile, hit-stop leaves it alone so only Resume un-pauses. `FeedbackDirector`
  also restores `Time.timeScale` from `OnDisable` if a dip was still in flight (a scene
  reload/unload can destroy it mid-wait, otherwise leaking a sub-1 `Time.timeScale` forever).
- **Camera shake** (player hurt, explosions) is trauma-based: each trigger adds
  `FeedbackConfig.PlayerHurtShakeTrauma`/`ExplosionShakeTrauma` (clamped to 1), and every `Update`
  decays it (`ShakeDecayPerSecond`, unscaled) and offsets the camera by
  `Random.insideUnitCircle * ShakeMaxOffset * trauma^2` from its captured base position -- once
  trauma reaches 0 the offset is exactly zero, so the camera lands back on its base position
  exactly, not approximately.
- **Particles**: `ParticleBurstFactory.Spawn(position, ParticleBurst, startColor, endColor)`
  builds a one-shot `ParticleSystem` entirely from code (a 4x4 point-filtered white texture, so
  particles read as pixel squares) and destroys itself after its lifetime. Enemy hit and death
  get separate, escalating bursts (`FeedbackConfig.EnemyHitBurst`/`EnemyDeathBurst`); explosions
  get the biggest (`ExplosionBurst`). Requires the `com.unity.modules.particlesystem` built-in
  module (added to `Packages/manifest.json` in [14] -- it is not enabled by default in a
  minimal-modules project like this one).
- `FeedbackConfig` (ScriptableObject, `Assets/Config/Feedback/FeedbackConfig.asset`): every
  number above, plus one `Enabled` bool per effect (hit-stop, shake, hit flash, particles) --
  `FeedbackConfig.Create(bool)` for code/tests turns all four on or off together.
- `FeedbackSetup` (`Assets/Editor/`) creates the config asset and the "Feedback" GameObject once
  and wires `FeedbackDirector` to the wizard's `Health` and the scene's `Camera`; runs after
  `UISetup.Bind`.
- Not attempted: a floating damage number/"BUMP!" text (see Known gaps) -- it would need a UI
  Toolkit label positioned in world space, which is more than the "only if cheap" bar in this
  ticket allowed; the hit flash + particles carry the hit feel instead.

## Pickups

- Self-contained prefabs under `Assets/Prefabs/Pickups/`, built by `Assets/Editor/PickupSetup.cs`
  (menu **Wizard Prototype > Build Pickup Prefabs**, or headless via `-executeMethod
  WizardArena.EditorTools.PickupSetup.BuildPrefabsFromCommandLine`). They use only Combat
  (`Health`, `Team`) and never reference Player, Enemies or Stage.
- `Pickup`: a `Collider2D` trigger area, checked each frame like `SpikeHazard`'s overlap but
  only against a `Team.Player` `Health`. On touch it calls `Apply(collector)` on every sibling
  `IPickupEffect` and consumes itself (destroys the GameObject) only if at least one effect
  actually did something -- e.g. a health potion touched at full HP is left in place rather
  than wasted. It bobs gently in place (`PickupConfig`: `BobHeight`/`BobSpeed`) and despawns on
  its own after `LifetimeSeconds`, blinking (toggling its `SpriteRenderer`) in its last
  `BlinkSeconds`.
- `IPickupEffect.Apply(GameObject collector) : bool` -- one thing a pickup does to whatever
  collected it; `HealEffect` heals through `Health.Heal` (capped there at `Max`) and returns
  `false` (does not consume the pickup) when the collector has no `Health`, is dead, or is
  already at `Max` HP.
- `PickupRegistry` (static `event Action<GameObject> Spawned`, raised from every `Pickup.Awake`)
  is how Stage learns about a pickup the moment it exists, the same way `EnemyRegistry` lets
  Stage learn about enemies -- used for cleanup of a pickup an `EnemyDropper` spawns at
  runtime, never through `StageDefinition.PropSpawns`.
- `HealthPotion.prefab` (`Assets/Art/Items/Potions/potion_health_small.png`, imported as a small
  centred sprite by `PickupSetup`, unlike the rest of `Assets/Art/Items` which isn't
  pre-configured as sprites): `Pickup` + `HealEffect` (4 HP), `PickupConfig` (12 s lifetime, 2 s
  blink). Dropped by enemies via `EnemyConfig.Drops` (Bat/BatSwift 20%, Blue Slime 30%, Skeleton
  Warrior 100%) and placed once directly on Portal Sanctum's floor via `PropSpawn`.

Tests: `Assets/Tests/PlayMode/Pickups/PickupTests.cs` (heals, capped at Max; leaves an
uncollected pickup in place at full HP; despawns after its lifetime), `EnemyDropperTests.cs`
(Assets/Tests/PlayMode/Enemies -- a chance of 1 always drops, 0 never does, at the death
position), `PickupCleanupTests.cs` (a pickup spawned outside PropSpawns is still removed when
the stage restarts, via `PickupRegistry`).

## Stage

- `GameSession` (one per scene) holds `GameState` (`Playing`, `Defeated`, `StageCleared`,
  `Victory`) and raises `StateChanged` when it changes. Anyone may read `State` and call the
  commands `Retry()`, `Continue()` and `RestartRun()`; only `StageManager` and `StagePortal` are
  allowed to end the run (`End(GameState)` is `internal`). All three commands delegate to
  `StageRunner` through the internal `IStageProgression` seam (`GameSession.Progression`, set by
  `StageRunner.Awake`): a scene with no `StageRunner` falls back to reloading itself. `Retry()`
  restarts the CURRENT stage in place (no scene reload); `Continue()` (only valid from
  `StageCleared`) advances to the next one; `RestartRun()` ([15]) restarts the WHOLE run from
  stage 1 -- the Victory screen's "Play Again" calls this one, not `Retry()`, so a finished run
  really starts over rather than just re-running the last stage. `GameSession.BeginStage()`
  (internal) flips the state back to `Playing` and raises `StateChanged(Playing)` so the end
  screen hides -- called by `StageRunner`, not raised for the very first stage.
- `StageManager` listens to `EnemyRegistry.Died`/`AliveCount` and opens the `StagePortal` once
  every enemy is dead (including zero enemies at scene start), and listens to the wizard's
  `Health.Died` to end the run in `Defeated`. It is stage-agnostic: `StageRunner` spawning a new
  batch of enemies just gives it a new count to watch.
- `StagePortal` sits on the exit portal object. Hidden until `Open()`, it grows and fades in
  over `StagePortalConfig.RevealSeconds` (0.8 s, scale 0.7 -> 1). Once fully revealed, the wizard
  standing within `EntryHalfSize` (0.65, 0.5) of it is drawn in through
  `WizardController.EnterPortal`; when the wizard's `Vanished` event then fires, the portal ends
  the run with `ClearOutcome` (internal; `StageCleared` for any stage but the last, `Victory`
  for the last -- set by `StageRunner`) and raises its own `PlayerEntered`/`PlayerExited` events
  for anything else that wants to react (sound, UI).
- `StageDefinition` (ScriptableObject, `Assets/Config/Stage/Stages/`): a stage's `DisplayName`,
  `PlayerSpawn`, `PortalPosition`, `EnemySpawns` (`EnemySpawn`: a prefab + a position) and
  `PropSpawns` (`PropSpawn`: same shape -- hazards ([12]) and pickups ([13]), spawned the same
  way as enemies). `StageSequence` (`Assets/Config/Stage/
  StageSequence.asset`) is the ordered list of stages played front to back.
- `StageRunner` (one per scene, on "Game Session") plays a `StageSequence` inside this one
  scene -- no scene reload for progression. On every stage start (`Awake` for the first stage;
  `RestartCurrentStage`/`AdvanceToNextStage`/`RestartRun` (jumps to index 0) from
  `IStageProgression` after that) it destroys
  whatever the last stage spawned, `Instantiate`s the new stage's `EnemySpawns`/`PropSpawns` at
  their positions, moves the portal and sets its `ClearOutcome`, and (except for the very first
  stage, to avoid an Awake-ordering dependency on the wizard) calls
  `WizardController.Respawn(stage.PlayerSpawn)`. Public API: `CurrentStage`, `IsLastStage`,
  `Sequence`, and `event Action<StageDefinition> StageStarted` (raised on every stage start,
  including the first -- a late subscriber should also read `CurrentStage` once from its own
  `Start`, the same way `HudController` reads `Health`). No enemy exists in the scene file
  itself; `StageRunner` is the only thing that spawns one, always from a prefab
  (`EnemySetup.LoadOrCreateBatPrefab`/`LoadOrCreateBatSwiftPrefab`, `Assets/Prefabs/Enemies/`).
  It also subscribes to `PickupRegistry.Spawned` (`OnEnable`/`OnDisable`) so a pickup an
  `EnemyDropper` spawns at runtime -- never through `PropSpawns` -- is tracked and destroyed on
  the next stage/retry exactly like anything else the stage spawned.

## UI

- Built with UI Toolkit (`UIDocument` + `PanelSettings`), UXML/USS under `Assets/UI/`. UI only
  listens to `Health`/`GameSession` events and calls small commands (`Retry`, `Continue`,
  `TogglePause`); it never touches gameplay state directly.
- `HudController` (on the "HUD" `GameObject`) shows the wizard's HP bar/numbers, the stage name
  and the controls hint. It reads `Health.Current`/`Max` and (if wired) `StageRunner.CurrentStage`
  once in `Start` (after every `Awake` in the scene has run) and after that only on
  `Health.Damaged`/`Healed`/`Died` or `StageRunner.StageStarted` -- no per-frame polling.
  `HudController.SetStageName(string)` is public for anything else that wants to drive it.
- `WorldHealthBar` is a small reusable component that builds its own bar from two flat
  `SpriteRenderer`s (background + fill) above any `Health`, refreshed the same event-driven way
  and hidden once that `Health` dies. Bats get one built into their prefab
  (`EnemySetup.LoadOrCreateBatPrefab`/`LoadOrCreateBatSwiftPrefab`).
- `EndScreenController` (on the "Screens" `GameObject`) shows Game Over / Stage Cleared /
  Victory purely from `GameSession.StateChanged`; its primary button calls `GameSession.Retry()`
  (Defeated), `Continue()` (StageCleared) or `RestartRun()` (Victory -- [15]: "Play Again" starts
  the whole run over, not just the last stage) depending on which state it is showing. Its Quit
  button calls `MainMenuCommand.ReturnToMenu()`.
- `PauseMenuController` (same `GameObject`, its own `UIDocument` sort order above the HUD) opens
  on Esc, sets `Time.timeScale = 0` and swaps the wizard's input for a no-op `IPlayerInput` via
  `WizardController.UseInput` so held keys have no effect while paused; Resume restores both. Its
  Quit button also calls `MainMenuCommand.ReturnToMenu()`.
- Both screens share one `PanelSettings` asset (`Assets/Config/UI/GamePanelSettings.asset`);
  `UIDocument.sortingOrder` (HUD 0, Screens 10) keeps the pause/end overlay above the HUD.
- `UISetup` (`Assets/Editor/`) builds and wires all of the above; it runs after `StageSetup.Bind`
  since it needs the scene's `GameSession`.
- **Main menu** ([15]): `Assets/Scenes/MainMenu.unity` (built by `Assets/Editor/MenuSetup.cs`) is
  a camera + the arena background sprite behind a UI Toolkit overlay (title "Wizard Arena", Play,
  Quit), reusing the same shared `GamePanelSettings.asset` via `UISetup.LoadOrCreatePanelSettings`
  (made `internal` for this reuse). `MainMenuController.Play()` (public, so tests can call it
  without simulating a pointer click) loads the arena by name; a freshly loaded scene always
  starts `StageRunner` at stage 1 (`currentIndex = 0` in its own `Awake`), so there is nothing
  stage-specific to do here. Its Quit button is the one place `QuitCommand` (actually exits the
  app / stops Play Mode) is still used directly -- every in-run Quit button (pause, end screens)
  instead calls `MainMenuCommand.ReturnToMenu()`, which resets `Time.timeScale` to 1 (in case the
  game was paused or mid hit-stop) and loads `MainMenu` by name. `SceneNames` (internal) holds
  both scene names once, shared by `MainMenuController` and `MainMenuCommand`.

## Tests

- Unity Test Framework (NUnit). Run them with `Tools/unity/unity.sh test` or the Test Runner
  window (**Window > General > Test Runner**).
- **EditMode** (`Assets/Tests/EditMode/`): pure logic, no frames. Create only the objects you
  need, destroy them in `TearDown`, restore any static state. `OnEnable`/`Awake` do not run on
  plain components in Edit Mode, so register objects by hand if needed
  (see `World/ArenaSurfaceTests.cs`).
- **PlayMode** (`Assets/Tests/PlayMode/`): load `WizardMovement` (it is in Build Settings) or
  build a tiny test rig, wait frames with `yield return`, then assert. Any error or exception
  logged during a test fails it (see `SceneSmokeTests.cs`).
- Name tests `Subject_Result_Condition`, e.g. `Support_ReturnsNull_WhenFeetAreAboveOrBesideTheSurface`.
- Every ticket that adds behaviour adds tests for it.

## Known gaps in the current code

The prototype predates these rules. Tickets fix them; don't copy these patterns.

- The "BUMP!" hit-reaction text from the old prototype HUD is gone and not brought back; [14]
  (feedback) covers hit feel with hit-stop, camera shake, hit flash and particles instead.
- A `StageDefinition` with zero enemies would leave its portal closed forever (`StageManager`
  only re-checks `AliveCount` on an enemy `Died`, and one with nothing to kill never dies). Not
  an issue for the current three stages (1/3/5 bats); a future all-hazard stage would need
  `StageManager` (or `StageRunner`) to re-check after spawning.
- `Tools/unity/unity.sh screenshot` renders `Camera.main` only: it does not capture UI Toolkit
  overlays (HUD, screens, the main menu), since they are not drawn through a camera.
- Tunable numbers now live in ScriptableObject config assets for every area, under
  `Assets/Config/{Combat,Player,Enemies,Stage,UI}/`.
- The Standalone scripting backend is Mono, not IL2CPP ([15]): the IL2CPP module is not
  installed on this machine, so a plain build fails without this. `ProjectSettings.asset`
  carries the choice; `Assets/Editor/MacBuild.cs` (`Tools/unity/unity.sh build-macos`) also
  re-asserts it in code before building, so a headless build never depends on the Editor's
  cached setting. Switch it back to IL2CPP (`PlayerSettings.SetScriptingBackend`) once that
  module is installed.
