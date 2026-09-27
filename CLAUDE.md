# Working in this repo (AI agents)

Unity 6 (6000.3.7f1) 2D wizard arena game. Read `ARCHITECTURE.md` before changing code.
Game rules: `GAME_RULES.md`. Arena surfaces: `ARENA_OBJECTS.md`.

## Rules

1. **Never merge a PR** and never push to `master`. The user merges.
2. **One squashed commit per PR**, message in plain English: a short title line, then a few
   lines on what changed and why.
3. **PR titles start with the ticket number**, e.g. `[05] Combat core: health and damage`.
4. **Run `Tools/unity/unity.sh compile` and `Tools/unity/unity.sh test` before opening a PR.**
   Both must pass, and `git status` must show no unintended changes afterwards.
5. **Never hand-edit scene YAML.** Change the builder in `Assets/Editor/`, run
   `Tools/unity/unity.sh rebuild-scene`, and commit the regenerated scene.
6. **Namespaces `WizardArena.<Area>`** in the matching `Assets/Scripts/<Area>/` folder.
   Move a script's `.meta` with it (`git mv` both) so scene references survive.
7. **SOLID and KISS.** Small classes with one job, composition over inheritance, a short public
   API per area (interfaces, C# events, a few methods), everything else private/internal.
   Follow the dependency directions in `ARCHITECTURE.md`.
8. **No `FindObjectsByType` / `FindFirstObjectByType` / `GameObject.Find` in gameplay code.**
   Use serialized references, events or registries. The scene builder and tests may use them.
9. **Tunable numbers go in ScriptableObject configs**, not magic numbers.
10. **Add tests** for new behaviour (`Assets/Tests/EditMode` or `Assets/Tests/PlayMode`).
11. Keep the game feeling the same unless the task says to change it.
12. Match the existing style (`.editorconfig`): 4 spaces, Allman braces, `camelCase` private
    fields, `PascalCase` constants and public members, short comments only where the why
    is not obvious.

## Tools/unity/unity.sh

Runs Unity in batch mode on the checkout that contains the script (works in any git
worktree). Logs and NUnit XML results go to `Logs/`. Exits non-zero on failure.

```
Tools/unity/unity.sh compile         # compile errors, if any
Tools/unity/unity.sh test-editmode   # EditMode tests
Tools/unity/unity.sh test-playmode   # PlayMode tests
Tools/unity/unity.sh test            # both
Tools/unity/unity.sh rebuild-scene   # regenerate Assets/Scenes/WizardMovement.unity
Tools/unity/unity.sh screenshot      # render Camera.main to Logs/screenshot.png (visual check)
Tools/unity/unity.sh build-macos     # stub until ticket [15]
```

- Unity path defaults to `/Applications/Unity/Hub/Editor/6000.3.7f1/Unity.app/Contents/MacOS/Unity`;
  override with `UNITY_PATH=...`.
- It refuses to run while the same project folder is open in the Unity editor. Work in your
  own git worktree instead of the user's checkout.
- The first run in a fresh worktree imports every asset and takes a few minutes.
- On failure, read the log it prints (compile errors and failed test names are summarised).
