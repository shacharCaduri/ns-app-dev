# Unity setup

1. Install Unity Hub from Unity's official download page.
2. In Unity Hub, install Unity **6.3 LTS**. The prototype targets `6000.3.7f1`.
3. In Unity Hub, choose **Add > Add project from disk** and select this repository folder.
4. Open the project and wait for Unity to finish importing the assets. The demo scene is generated automatically.
5. Open `Assets/Scenes/WizardMovement.unity` if it is not already open, then press **Play**.
6. Use the **left** and **right arrow keys** in the Game view.

To recreate the scene, select **Wizard Prototype > Rebuild Demo Scene** from Unity's menu.

## Running tests

- **In the editor:** open **Window > General > Test Runner**. The **EditMode** tab runs the fast
  logic tests; the **PlayMode** tab runs tests that load and play the scene. Click **Run All**.
- **From a terminal** (close the project in Unity first, or use a separate git worktree):

  ```
  Tools/unity/unity.sh compile        # check for compile errors
  Tools/unity/unity.sh test           # EditMode + PlayMode tests
  Tools/unity/unity.sh rebuild-scene  # regenerate the scene headlessly
  ```

  Logs and test results are written to `Logs/`. Set `UNITY_PATH` if Unity is installed
  somewhere else. See `CLAUDE.md` and `ARCHITECTURE.md` for project conventions.
