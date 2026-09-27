# Architecture

A Unity 6 (6000.3.7f1) 2D arena game. This page says where code goes and which way
dependencies may point. Keep it short; update it when a rule changes.

## Layout

| Folder | Namespace | Assembly | What lives there |
| --- | --- | --- | --- |
| `Assets/Scripts/World/` | `WizardArena.World` | `WizardArena.Runtime` | Arena geometry (`ArenaSurface`), later hazards |
| `Assets/Scripts/Combat/` | `WizardArena.Combat` | `WizardArena.Runtime` | Health, damage, projectiles (`ArcaneBolt`) |
| `Assets/Scripts/Player/` | `WizardArena.Player` | `WizardArena.Runtime` | The wizard (`WizardController`) |
| `Assets/Scripts/Enemies/` | `WizardArena.Enemies` | `WizardArena.Runtime` | Enemies (`BatEnemyController`) |
| `Assets/Scripts/Stage/` | `WizardArena.Stage` | `WizardArena.Runtime` | Stage flow, portal, game state (`StagePortalGate`) |
| `Assets/Scripts/UI/` | `WizardArena.UI` | `WizardArena.Runtime` | HUD and screens (added in [09]) |
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

- `ArcaneBolt` (Combat) targets `BatEnemyController` directly and finds bats with
  `FindObjectsByType` ([05]).
- `WizardController` is one large class that also draws the HUD and end screen in `OnGUI`
  ([06], [09]).
- `BatEnemyController` finds the wizard with `FindFirstObjectByType` and draws its health bar
  in `OnGUI` ([07], [09]).
- `StagePortalGate` polls `FindObjectsByType` every frame and is added by the wizard ([08]).
- Tunable numbers are still serialized fields or constants in the behaviours ([05]-[08]).
