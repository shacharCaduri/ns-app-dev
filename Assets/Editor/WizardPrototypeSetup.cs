using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class WizardPrototypeSetup
{
    private const string ScenePath = "Assets/Scenes/WizardMovement.unity";
    private const string WizardRoot = "Assets/Art/Characters/Wizard";
    private const string BatRoot = "Assets/Art/Enemies/cave_bat";

    static WizardPrototypeSetup()
    {
        EditorApplication.delayCall += CreatePrototypeIfNeeded;
        EditorApplication.delayCall += UpdateCombatAssets;
    }

    [MenuItem("Wizard Prototype/Rebuild Demo Scene")]
    public static void RebuildPrototype()
    {
        CreatePrototype(true);
    }

    private static void CreatePrototypeIfNeeded()
    {
        if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            CreatePrototype(true);
        }
    }

    [MenuItem("Wizard Prototype/Update Combat Assets")]
    public static void UpdateCombatAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) return;
        WizardController wizard = Object.FindFirstObjectByType<WizardController>();
        BatEnemyController bat = Object.FindFirstObjectByType<BatEnemyController>();
        if (wizard == null || bat == null) return;
        bool wasDirty = scene.isDirty;
        ConfigureSpriteTextures(WizardRoot, new Vector2(0.5f, 0.015625f));
        ConfigureSpriteTextures(BatRoot, new Vector2(0.5f, 0.5f));
        ConfigureSpriteTextures("Assets/Art/Effects/Projectiles", new Vector2(0.5f, 0.5f));
        ConfigureSpriteTextures("Assets/Art/Environments/Portals", new Vector2(0.5f, 0f));
        BindWizardCombat(wizard);
        SerializedObject data = new SerializedObject(bat);
        data.FindProperty("deadLeft").objectReferenceValue = LoadSprite(BatRoot + "/death/cave_bat_death_left_01.png");
        data.FindProperty("deadRight").objectReferenceValue = LoadSprite(BatRoot + "/death/cave_bat_death_right_01.png");
        data.FindProperty("hurtLeft").objectReferenceValue = LoadSprite(BatRoot + "/hurt/cave_bat_hurt_left_01.png");
        data.FindProperty("hurtRight").objectReferenceValue = LoadSprite(BatRoot + "/hurt/cave_bat_hurt_right_01.png");
        if (data.FindProperty("exitPortal").objectReferenceValue == null)
        {
            GameObject portal = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "Stage Exit Portal") portal = root;
            data.FindProperty("exitPortal").objectReferenceValue = portal != null ? portal : CreatePortal();
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!wasDirty) EditorSceneManager.SaveScene(scene);
    }

    private static void BindWizardCombat(WizardController controller)
    {
        SerializedObject data = new SerializedObject(controller);
        AssignSprites(data.FindProperty("attackRightFrames"), WizardRoot + "/attack/wizard_attack_right_0{0}.png");
        AssignSprites(data.FindProperty("attackLeftFrames"), WizardRoot + "/attack/wizard_attack_left_0{0}.png");
        data.FindProperty("projectileRight").objectReferenceValue = LoadSprite("Assets/Art/Effects/Projectiles/arcane_projectile_right_01.png");
        data.FindProperty("projectileLeft").objectReferenceValue = LoadSprite("Assets/Art/Effects/Projectiles/arcane_projectile_left_01.png");
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreatePrototype(bool force)
    {
        if (!force && File.Exists(ScenePath))
        {
            return;
        }

        Directory.CreateDirectory("Assets/Scenes");
        ConfigureSpriteTextures(WizardRoot, new Vector2(0.5f, 0.015625f));
        ConfigureSpriteTextures(BatRoot, new Vector2(0.5f, 0.5f));
        ConfigureSpriteTextures("Assets/Art/Environments/Portals", new Vector2(0.5f, 0f));
        ConfigureSpriteTextures("Assets/Art/Effects/Projectiles", new Vector2(0.5f, 0.5f));

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "WizardMovement";

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.075f, 0.12f, 1f);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        CreateBackdrop();

        GameObject wizard = new GameObject("Wizard");
        wizard.transform.position = new Vector3(0f, -2.4f, 0f);
        SpriteRenderer renderer = wizard.AddComponent<SpriteRenderer>();
        Sprite idleSprite = LoadSprite(WizardRoot + "/idle/wizard_idle_down_01.png");
        renderer.sprite = idleSprite;
        renderer.sortingOrder = 10;
        WizardController controller = wizard.AddComponent<WizardController>();
        CircleCollider2D wizardCollider = wizard.AddComponent<CircleCollider2D>();
        wizardCollider.radius = 0.55f;
        wizardCollider.offset = new Vector2(0f, 0.8f);

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("idleSprite").objectReferenceValue = idleSprite;
        AssignSprites(serializedController.FindProperty("walkRightFrames"), WizardRoot + "/walk/wizard_walk_right_0{0}.png");
        AssignSprites(serializedController.FindProperty("walkLeftFrames"), WizardRoot + "/walk/wizard_walk_left_0{0}.png");
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        BindWizardCombat(controller);

        CreateBatEnemy();

        EditorSceneManager.SaveScene(scene, ScenePath);
        ArenaSurfaceSetup.AddSurfaces();
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Selection.activeGameObject = wizard;
        AssetDatabase.SaveAssets();

        Debug.Log("Wizard prototype is ready. Press Play and use the left/right arrow keys.");
    }

    private static void ConfigureSpriteTextures(string assetRoot, Vector2 pivot)
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { assetRoot });
        foreach (string guid in textureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void AssignSprites(SerializedProperty property, string pathPattern)
    {
        property.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite(string.Format(pathPattern, i + 1));
        }
    }

    private static void CreateBackdrop()
    {
        const string backgroundPath = "Assets/Art/Environments/arena-background.png";
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(backgroundPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();

        GameObject backdrop = new GameObject("Arena Background");
        SpriteRenderer renderer = backdrop.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(backgroundPath);
        // The floor is painted at 80% of the image height. Match its top
        // to the wizard's -2.4 standing height, preserving the image aspect.
        float scale = 11.2f / renderer.sprite.bounds.size.y;
        backdrop.transform.localScale = Vector3.one * scale;
        backdrop.transform.position = new Vector3(0f, 0.96f, 0f);
        renderer.sortingOrder = -10;
    }

    private static void CreateBatEnemy()
    {
        GameObject bat = new GameObject("Bat Enemy");
        bat.transform.position = new Vector3(4f, 1.5f, 0f);

        SpriteRenderer renderer = bat.AddComponent<SpriteRenderer>();
        Sprite firstFrame = LoadSprite(BatRoot + "/fly/cave_bat_fly_left_01.png");
        renderer.sprite = firstFrame;
        renderer.sortingOrder = 10;

        CircleCollider2D collider = bat.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.45f;

        Rigidbody2D rigidbody = bat.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.gravityScale = 0f;

        BatEnemyController controller = bat.AddComponent<BatEnemyController>();
        SerializedObject serializedController = new SerializedObject(controller);
        AssignSprites(serializedController.FindProperty("flyRightFrames"), BatRoot + "/fly/cave_bat_fly_right_0{0}.png", 2);
        AssignSprites(serializedController.FindProperty("flyLeftFrames"), BatRoot + "/fly/cave_bat_fly_left_0{0}.png", 2);
        serializedController.FindProperty("deadLeft").objectReferenceValue = LoadSprite(BatRoot + "/death/cave_bat_death_left_01.png");
        serializedController.FindProperty("deadRight").objectReferenceValue = LoadSprite(BatRoot + "/death/cave_bat_death_right_01.png");
        serializedController.FindProperty("hurtLeft").objectReferenceValue = LoadSprite(BatRoot + "/hurt/cave_bat_hurt_left_01.png");
        serializedController.FindProperty("hurtRight").objectReferenceValue = LoadSprite(BatRoot + "/hurt/cave_bat_hurt_right_01.png");
        serializedController.FindProperty("exitPortal").objectReferenceValue = CreatePortal();
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreatePortal()
    {
        GameObject portal = new GameObject("Stage Exit Portal");
        portal.transform.position = new Vector3(5.5f, -2.4f, 0f);
        SpriteRenderer portalRenderer = portal.AddComponent<SpriteRenderer>();
        portalRenderer.sprite = LoadSprite("Assets/Art/Environments/Portals/portal_active.png");
        portalRenderer.sortingOrder = 5;
        float portalScale = 2.8f / portalRenderer.sprite.bounds.size.y;
        portal.transform.localScale = Vector3.one * portalScale;
        portal.SetActive(false);
        return portal;
    }

    private static void AssignSprites(SerializedProperty property, string pathPattern, int frameCount)
    {
        property.arraySize = frameCount;
        for (int i = 0; i < frameCount; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = LoadSprite(string.Format(pathPattern, i + 1));
        }
    }

}
