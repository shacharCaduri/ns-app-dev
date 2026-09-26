using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ArenaSurfaceSetup
{
    static ArenaSurfaceSetup() { EditorApplication.delayCall += AddSurfaces; }

    [MenuItem("Wizard Prototype/Add Walkable Arena")]
    public static void AddSurfaces()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/WizardMovement.unity") return;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "Arena Surfaces") return;
        GameObject backdrop = GameObject.Find("Arena Background");
        if (backdrop == null) return;
        bool wasDirty = scene.isDirty;
        Bounds art = backdrop.GetComponent<SpriteRenderer>().bounds;
        GameObject group = new GameObject("Arena Surfaces");
        // Coordinates measured against the existing 1672 x 941 arena artwork.
        Add(group, art, "Arena Floor", 0, 1672, 753, 940, false);
        Add(group, art, "Left Ruin Ledge", 0, 282, 540, 753, false);
        Add(group, art, "Left Stair 1", 282, 420, 565, 753, false);
        Add(group, art, "Left Stair 2", 420, 493, 590, 753, false);
        Add(group, art, "Left Stair 3", 493, 525, 617, 753, false);
        Add(group, art, "Left Stair 4", 525, 560, 642, 753, false);
        Add(group, art, "Left Stair 5", 560, 590, 668, 753, false);
        Add(group, art, "Left Stair 6", 590, 622, 692, 753, false);
        Add(group, art, "Left Stair 7", 622, 652, 719, 753, false);
        Add(group, art, "Right Stair 1", 1230, 1265, 724, 753, false);
        Add(group, art, "Right Stair 2", 1265, 1300, 697, 753, false);
        Add(group, art, "Right Stair 3", 1300, 1340, 670, 753, false);
        Add(group, art, "Right Stair 4", 1340, 1380, 641, 753, false);
        Add(group, art, "Right Stair 5", 1380, 1420, 615, 753, false);
        Add(group, art, "Right Stair 6", 1420, 1460, 589, 753, false);
        Add(group, art, "Right Stair 7", 1460, 1495, 563, 753, false);
        Add(group, art, "Right Stair 8", 1495, 1535, 534, 753, false);
        Add(group, art, "Right Ruin Ledge", 1438, 1672, 501, 531, true);
        Add(group, art, "Central Floating Platform", 951, 1225, 535, 570, true);
        Add(group, art, "Upper Floating Platform", 1250, 1457, 377, 410, true);
        WizardController wizard = Object.FindFirstObjectByType<WizardController>();
        if (wizard != null)
        {
            SerializedObject data = new SerializedObject(wizard);
            data.FindProperty("jumpSpeed").floatValue = 10.5f;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        // Keep the stage exit on the unobstructed floor, away from the stairs.
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == "Stage Exit Portal") root.transform.position = new Vector3(0.5f, art.max.y - 753f / 941f * art.size.y, 0f);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!wasDirty) EditorSceneManager.SaveScene(scene);
    }

    private static void Add(GameObject parent, Bounds art, string name, float left, float right, float top, float bottom, bool jumpThrough)
    {
        float x1 = art.min.x + left / 1672f * art.size.x;
        float x2 = art.min.x + right / 1672f * art.size.x;
        float y1 = art.max.y - top / 941f * art.size.y;
        float y2 = art.max.y - bottom / 941f * art.size.y;
        GameObject item = new GameObject(name);
        item.transform.SetParent(parent.transform);
        item.transform.position = new Vector3((x1 + x2) * 0.5f, (y1 + y2) * 0.5f, 0f);
        BoxCollider2D box = item.AddComponent<BoxCollider2D>();
        box.size = new Vector2(x2 - x1, y1 - y2);
        ArenaSurface surface = item.AddComponent<ArenaSurface>();
        surface.jumpThrough = jumpThrough;
    }
}
