# Continuous integration (GitHub Actions)

`.github/workflows/tests.yml` runs the Unity **EditMode and PlayMode tests** with
[GameCI](https://game.ci/docs/github/test-runner) (`game-ci/unity-test-runner@v4`, Unity `6000.3.7f1`).

## When it runs

- On every pull request (any base branch).
- On every push to `master`.
- By hand: **Actions > Unity tests > Run workflow**.

## What it does

1. **Check Unity license secrets**: looks for the repo secrets below. If they are missing
   (for example before you add them, or on a PR from a fork) it prints a
   "Unity tests skipped" notice and the test job shows as **skipped**, not failed.
2. **EditMode + PlayMode tests**: checks out the repo, restores the cached `Library/` folder
   (keyed on `Assets/`, `Packages/` and `ProjectSettings/`), runs all tests, publishes a
   "Unity Test Results" check on the PR, and uploads the NUnit XML results as the
   `unity-test-results` artifact (on the run's summary page), even when tests fail.

The first run with a cold cache imports every asset and takes longer; later runs reuse `Library/`.

## Adding the secrets (Unity Personal)

You do this yourself; never paste these values anywhere else. GameCI uses them only to
activate Unity inside the job.

1. **Get the license file.** Open Unity Hub and sign in. Go to **Preferences > Licenses**,
   click **Add** and pick **Get a free personal license**. (If a license is already listed,
   still click Add once so the file is written.) The file is saved at:
   - macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`

   On macOS you can copy it with: `pbcopy < "/Library/Application Support/Unity/Unity_lic.ulf"`
2. **Open the repo secrets page.** On GitHub: **Settings > Secrets and variables > Actions >
   New repository secret**.
3. **Add three secrets:**

   | Name | Value |
   |---|---|
   | `UNITY_LICENSE` | the whole contents of `Unity_lic.ulf` |
   | `UNITY_EMAIL` | the email of your Unity account |
   | `UNITY_PASSWORD` | the password of your Unity account |

4. **Re-run the workflow** (open the PR's checks and click **Re-run**, or push a commit).
   The test job should now run instead of being skipped.

Unity Pro / Plus instead: add `UNITY_SERIAL` (your serial key) in place of `UNITY_LICENSE`,
plus `UNITY_EMAIL` and `UNITY_PASSWORD`.

If Unity Hub does not write a `.ulf` file, see GameCI's
[activation guide](https://game.ci/docs/github/activation) for the current workaround.

## Running the same tests locally

`Tools/unity/unity.sh test` (see `UNITY_SETUP.md`).
