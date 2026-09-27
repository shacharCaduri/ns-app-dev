# Architecture

A Unity 6 (6000.3.7f1) 2D arena game. This page says where code goes and which way
dependencies may point. Keep it short; update it when a rule changes.

## Layout

| Folder | Namespace | Assembly | What lives there |
| --- | --- | --- | --- |
| `Assets/Scripts/World/` | `WizardArena.World` | `WizardArena.Runtime` | Arena geometry (`ArenaSurface`, `ArenaBounds`), later hazards |
| `Assets/Scripts/Combat/` | `WizardArena.Combat` | `WizardArena.Runtime` | `Health`, `IDamageable`, `DamageInfo`, `Team`, `Projectile` + `ProjectileLauncher` |
| `Assets/Scripts/Player/` | `WizardArena.Player` | `WizardArena.Runtime` | The wizard: `WizardController` composes `PlayerMotor`, `PlayerCaster`, `PlayerAnimator`, `PlayerHitFeedback`, `PlayerTransitions`; input via `IPlayerInput` |
| `Assets/Scripts/Enemies/` | `WizardArena.Enemies` | `WizardArena.Runtime` | Enemy framework (`Enemy`, `EnemyConfig`, `EnemyRegistry`, `ContactDamage`, states) and enemy types (`BatEnemyController`) |
| `Assets/Scripts/Stage/` | `WizardArena.Stage` | `WizardArena.Runtime` | Stage flow, portal, game state and progression (`GameSession`, `StageManager`, `StagePortal`, `StageRunner`, `StageDefinition`, `StageSequence`) |
| `Assets/Scripts/UI/` | `WizardArena.UI` | `WizardArena.Runtime` | HUD, enemy HP bars and screens (UI Toolkit); UXML/USS under `Assets/UI/` |
| `Assets/Scripts/Feedback/` | `WizardArena.Feedback` | `WizardArena.Runtime` | Hit-stop, shake, particles, sound (added in [14]) |
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
               Stage                  (game flow: listens to Player/Enemies events)
              /     \
         Player    Enemies            (never reference each other's concrete classes)
              \     /
              Combat                  (health, damage, projectiles)
                 |
               World                  (surfaces, hazards)
```

- A layer may use the layers **below** it, never the ones above.
- **Combat** knows nothing about Player or Enemies. It defines small interfaces
  (e.g. something that can take damage) that Player and Enemies implement.
- **Player and Enemies** do not reference each other. An enemy that needs a target gets it
  through a Combat interface or a serialized reference set by the scene builder.
- **Stage** reacts to events (enemy defeated, player defeated) instead of polling.
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
- **Scenes are generated.** `Assets/Scenes/WizardMovement.unity` is built by
  `WizardArena.EditorTools.SceneBuilder` (menu **Wizard Prototype > Rebuild Demo Scene**, or
  `Tools/unity/unity.sh rebuild-scene`). To change the scene, change the builder
  (`Assets/Editor/`), regenerate, and commit the scene. Never hand-edit scene YAML.
  The builder's automatic on-load hooks are skipped in batch mode, so compile/test runs never
  change the scene.
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

## Stage

- `GameSession` (one per scene) holds `GameState` (`Playing`, `Defeated`, `StageCleared`,
  `Victory`) and raises `StateChanged` when it changes. Anyone may read `State` and call the
  commands `Retry()` and `Continue()`; only `StageManager` and `StagePortal` are allowed to end
  the run (`End(GameState)` is `internal`). Both commands delegate to `StageRunner` through the
  internal `IStageProgression` seam (`GameSession.Progression`, set by `StageRunner.Awake`): a
  scene with no `StageRunner` falls back to reloading itself. `Retry()` restarts the CURRENT
  stage in place (no scene reload); `Continue()` (only valid from `StageCleared`) advances to
  the next one. `GameSession.BeginStage()` (internal) flips the state back to `Playing` and
  raises `StateChanged(Playing)` so the end screen hides -- called by `StageRunner`, not raised
  for the very first stage.
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
  `PropSpawns` (`PropSpawn`: same shape -- a placement hook for hazards/pickups, [12]/[13],
  spawned the same way but nothing populates it yet). `StageSequence` (`Assets/Config/Stage/
  StageSequence.asset`) is the ordered list of stages played front to back.
- `StageRunner` (one per scene, on "Game Session") plays a `StageSequence` inside this one
  scene -- no scene reload for progression. On every stage start (`Awake` for the first stage;
  `RestartCurrentStage`/`AdvanceToNextStage` from `IStageProgression` after that) it destroys
  whatever the last stage spawned, `Instantiate`s the new stage's `EnemySpawns`/`PropSpawns` at
  their positions, moves the portal and sets its `ClearOutcome`, and (except for the very first
  stage, to avoid an Awake-ordering dependency on the wizard) calls
  `WizardController.Respawn(stage.PlayerSpawn)`. Public API: `CurrentStage`, `IsLastStage`,
  `Sequence`, and `event Action<StageDefinition> StageStarted` (raised on every stage start,
  including the first -- a late subscriber should also read `CurrentStage` once from its own
  `Start`, the same way `HudController` reads `Health`). No enemy exists in the scene file
  itself; `StageRunner` is the only thing that spawns one, always from a prefab
  (`EnemySetup.LoadOrCreateBatPrefab`/`LoadOrCreateBatSwiftPrefab`, `Assets/Prefabs/Enemies/`).

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
  Victory purely from `GameSession.StateChanged`; its button calls `GameSession.Retry()` or
  `Continue()` depending on which state it is showing.
- `PauseMenuController` (same `GameObject`, its own `UIDocument` sort order above the HUD) opens
  on Esc, sets `Time.timeScale = 0` and swaps the wizard's input for a no-op `IPlayerInput` via
  `WizardController.UseInput` so held keys have no effect while paused; Resume restores both.
- Both screens share one `PanelSettings` asset (`Assets/Config/UI/GamePanelSettings.asset`);
  `UIDocument.sortingOrder` (HUD 0, Screens 10) keeps the pause/end overlay above the HUD.
- `UISetup` (`Assets/Editor/`) builds and wires all of the above; it runs after `StageSetup.Bind`
  since it needs the scene's `GameSession`.

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

- The "BUMP!" hit-reaction text from the old prototype HUD is gone; [14] (feedback) is where
  hit feel like that belongs.
- A `StageDefinition` with zero enemies would leave its portal closed forever (`StageManager`
  only re-checks `AliveCount` on an enemy `Died`, and one with nothing to kill never dies). Not
  an issue for the current three stages (1/3/5 bats); a future all-hazard stage would need
  `StageManager` (or `StageRunner`) to re-check after spawning.
- `EndScreenController`'s Victory button ("Play Again") calls `GameSession.Retry()`, which
  restarts the CURRENT (last) stage, not the whole sequence from stage 1. Simple by design
  ([10]); revisit if "play again" should mean "start over".
- `Tools/unity/unity.sh screenshot` renders `Camera.main` only: it does not capture UI Toolkit
  overlays (HUD, screens), since they are not drawn through a camera.
- Tunable numbers now live in ScriptableObject config assets for every area, under
  `Assets/Config/{Combat,Player,Enemies,Stage,UI}/`.
