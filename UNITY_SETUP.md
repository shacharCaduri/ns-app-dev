# Unity setup

1. Install Unity Hub from Unity's official download page.
2. In Unity Hub, install Unity **6.3 LTS**. The prototype targets `6000.3.7f1`.
3. In Unity Hub, choose **Add > Add project from disk** and select this repository folder.
4. Open the project and wait for Unity to finish importing the assets. The demo scene is generated automatically.
5. Open `Assets/Scenes/WizardMovement.unity` if it is not already open, then press **Play**.
6. Use the **left** and **right arrow keys** in the Game view.

To recreate the scene, select **Wizard Prototype > Rebuild Demo Scene** from Unity's menu.

## Git setup (once per clone)

Scenes, prefabs and assets are marked `merge=unityyamlmerge` in `.gitattributes`. Register Unity's
smart merge tool so git can merge them structurally instead of line by line (macOS path shown):

```sh
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver \
  '"/Applications/Unity/Hub/Editor/6000.3.7f1/Unity.app/Contents/Helpers/UnityYAMLMerge" merge -p %O %B %A %A'
git config merge.unityyamlmerge.recursive binary
```

Without this, git falls back to its normal text merge. Keep scene edits on one branch at a time
where possible — scene conflicts are the hardest to resolve.
