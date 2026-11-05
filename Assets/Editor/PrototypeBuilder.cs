using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using UnityEngine.UI;

// Batch-mode builder for plan steps 6-9. Each entry point exits the editor itself.
// SetupBatch:    Tuanjie.exe -batchmode -nographics -projectPath <project> -executeMethod PrototypeBuilder.SetupBatch
// BuildApkBatch: Tuanjie.exe -batchmode -nographics -projectPath <project> -executeMethod PrototypeBuilder.BuildApkBatch
public static class PrototypeBuilder
{
    const string ArtDir = "Assets/Art/Generated";
    const string TileDir = "Assets/Art/Tiles";
    const string PrefabDir = "Assets/Prefabs";
    const string DataDir = "Assets/Data";
    const string ScenePath = "Assets/Scenes/Stage01.unity";
    const string ApkRelativePath = "Builds/android/Prototype.apk";

    public static void SetupBatch()
    {
        try
        {
            ConfigureProject();
            BuildScene();
            Debug.Log("[PrototypeBuilder] SetupBatch finished");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[PrototypeBuilder] SetupBatch failed: " + e);
            EditorApplication.Exit(1);
        }
    }

    public static void BuildApkBatch()
    {
        try
        {
            BuildApk();
            Debug.Log("[PrototypeBuilder] BuildApkBatch finished");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[PrototypeBuilder] BuildApkBatch failed: " + e);
            EditorApplication.Exit(1);
        }
    }

    // Plan step 8: Android player settings for a landscape APK on mid-range phones (64-bit only).
    static void ConfigureProject()
    {
        AddTagIfMissing("Ground");

        PlayerSettings.productName = "Contra Prototype";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.wjp.contra.prototype");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.gcIncremental = true;

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
    }

    // Plan steps 6-7: placeholder art, data asset, prefabs, stage and touch UI.
    static void BuildScene()
    {
        Directory.CreateDirectory(ArtDir);
        Directory.CreateDirectory(TileDir);
        Directory.CreateDirectory(PrefabDir);
        Directory.CreateDirectory(DataDir);

        Sprite groundSprite = MakeSprite("tile_ground", 16, 16, new Color(0.35f, 0.62f, 0.25f), new Color(0.18f, 0.36f, 0.12f));
        Sprite platformSprite = MakeSprite("tile_platform", 16, 16, new Color(0.62f, 0.42f, 0.2f), new Color(0.32f, 0.2f, 0.08f));
        Sprite playerSprite = MakeSprite("player", 16, 24, new Color(0.25f, 0.5f, 0.95f), new Color(0.1f, 0.2f, 0.5f));
        Sprite enemySprite = MakeSprite("enemy", 16, 16, new Color(0.9f, 0.3f, 0.25f), new Color(0.45f, 0.1f, 0.08f));
        Sprite bulletSprite = MakeSprite("bullet", 6, 3, new Color(1f, 0.92f, 0.35f), new Color(1f, 0.55f, 0f));

        Tile groundTile = CreateTile(groundSprite, "Ground");
        Tile platformTile = CreateTile(platformSprite, "Platform");
        WeaponData weapon = CreateWeapon();
        Bullet bulletPrefab = CreateBulletPrefab(bulletSprite);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera: orthographic, pixel perfect (16 PPU, 384x216 reference), follows the player.
        Camera cam = UnityEngine.Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            GameObject camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            cam = camGo.GetComponent<Camera>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 6.75f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.45f, 0.7f, 0.95f);
        // The camera must sit in front of the z=0 world. A camera created at z=0 is clipped by its near plane.
        cam.transform.position = new Vector3(0f, 2f, -10f);
        cam.transform.rotation = Quaternion.identity;
        PixelPerfectCamera ppc = cam.gameObject.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU = 16;
        ppc.refResolutionX = 384;
        ppc.refResolutionY = 216;
        ppc.cropFrameX = true;

        // URP 2D: lit sprites render invisible without a 2D light (the 2D template ships with a Global Light 2D).
        GameObject lightGo = new GameObject("Global Light 2D");
        Light2D globalLight = lightGo.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.intensity = 1f;

        // Level: Grid > Ground tilemap with a composite collider.
        GameObject gridGo = new GameObject("Grid");
        gridGo.AddComponent<Grid>();
        GameObject groundGo = new GameObject("Ground");
        groundGo.transform.SetParent(gridGo.transform, false);
        groundGo.tag = "Ground";
        Tilemap tilemap = groundGo.AddComponent<Tilemap>();
        TilemapRenderer tilemapRenderer = groundGo.AddComponent<TilemapRenderer>();
        tilemapRenderer.sortingOrder = 0;
        Rigidbody2D groundBody = groundGo.AddComponent<Rigidbody2D>();
        groundBody.bodyType = RigidbodyType2D.Static;
        // Plain tilemap colliders: the composite path did not produce working colliders on the phone build.
        groundGo.AddComponent<TilemapCollider2D>();

        for (int x = -4; x < 90; x++)
        {
            bool pit = (x >= 22 && x <= 24) || (x >= 56 && x <= 59);
            if (pit) continue;
            tilemap.SetTile(new Vector3Int(x, 0, 0), groundTile);
            tilemap.SetTile(new Vector3Int(x, -1, 0), groundTile);
            tilemap.SetTile(new Vector3Int(x, -2, 0), groundTile);
        }
        PaintRow(tilemap, platformTile, 8, 12, 3);
        PaintRow(tilemap, platformTile, 30, 34, 4);
        PaintRow(tilemap, platformTile, 42, 46, 2);

        // Player: run, jump, gun with muzzle and ground check.
        GameObject playerGo = new GameObject("Player");
        playerGo.transform.position = new Vector3(0f, 2f, 0f);
        SpriteRenderer playerRenderer = playerGo.AddComponent<SpriteRenderer>();
        playerRenderer.sprite = playerSprite;
        playerRenderer.sortingOrder = 10;
        Rigidbody2D playerBody = playerGo.AddComponent<Rigidbody2D>();
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D playerCollider = playerGo.AddComponent<BoxCollider2D>();
        playerCollider.size = new Vector2(0.8f, 1.4f);
        PlayerController controller = playerGo.AddComponent<PlayerController>();

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(playerGo.transform, false);
        groundCheck.transform.localPosition = new Vector3(0f, -0.75f, 0f);
        controller.groundCheck = groundCheck.transform;

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(playerGo.transform, false);
        muzzle.transform.localPosition = new Vector3(0.6f, 0.25f, 0f);

        Gun gun = playerGo.AddComponent<Gun>();
        gun.weapon = weapon;
        gun.bulletPrefab = bulletPrefab;
        gun.muzzle = muzzle.transform;
        controller.gun = gun;

        CreateEnemy(enemySprite, new Vector3(14f, 1f, 0f), 2f);
        CreateEnemy(enemySprite, new Vector3(28f, 1f, 0f), 3f);
        CreateEnemy(enemySprite, new Vector3(40f, 1f, 0f), 2f);
        CreateEnemy(enemySprite, new Vector3(62f, 1f, 0f), 3f);
        CreateEnemy(enemySprite, new Vector3(76f, 1f, 0f), 2f);
        CreateEnemy(enemySprite, new Vector3(10f, 4f, 0f), 1.5f);

        CameraFollow2D follow = cam.gameObject.AddComponent<CameraFollow2D>();
        follow.target = playerGo.transform;

        // Touch UI: Screen Space Overlay canvas, safe-area panel, joystick, two buttons, FPS label.
        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        GameObject safeGo = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
        safeGo.transform.SetParent(canvasGo.transform, false);
        StretchFull((RectTransform)safeGo.transform);

        GameObject fpsGo = new GameObject("FPS", typeof(RectTransform), typeof(Text), typeof(FpsCounter));
        fpsGo.transform.SetParent(safeGo.transform, false);
        RectTransform fpsRect = (RectTransform)fpsGo.transform;
        fpsRect.anchorMin = new Vector2(0f, 1f);
        fpsRect.anchorMax = new Vector2(0f, 1f);
        fpsRect.pivot = new Vector2(0f, 1f);
        fpsRect.anchoredPosition = new Vector2(24f, -24f);
        fpsRect.sizeDelta = new Vector2(900f, 260f);
        Text fpsText = fpsGo.GetComponent<Text>();
        fpsText.font = font;
        fpsText.fontSize = 30;
        fpsText.color = Color.yellow;
        fpsText.raycastTarget = false;
        FpsCounter fpsCounter = fpsGo.GetComponent<FpsCounter>();
        fpsCounter.worldCamera = cam;
        fpsCounter.player = playerGo.transform;

        GameObject stickGo = new GameObject("Stick", typeof(RectTransform), typeof(Image), typeof(OnScreenStick), typeof(DpSize));
        stickGo.transform.SetParent(safeGo.transform, false);
        RectTransform stickRect = (RectTransform)stickGo.transform;
        stickRect.anchorMin = new Vector2(0f, 0f);
        stickRect.anchorMax = new Vector2(0f, 0f);
        stickRect.pivot = new Vector2(0.5f, 0.5f);
        stickRect.anchoredPosition = Vector2.zero;
        Image stickImage = stickGo.GetComponent<Image>();
        stickImage.sprite = uiSprite;
        stickImage.color = new Color(1f, 1f, 1f, 0.25f);
        DpSize stickSize = stickGo.GetComponent<DpSize>();
        stickSize.canvas = canvas;
        stickSize.widthDp = 150f;
        stickSize.heightDp = 150f;
        stickSize.offsetDp = new Vector2(99f, 99f);

        GameObject knobGo = new GameObject("Knob", typeof(RectTransform), typeof(Image), typeof(DpSize));
        knobGo.transform.SetParent(stickGo.transform, false);
        RectTransform knobRect = (RectTransform)knobGo.transform;
        knobRect.anchorMin = new Vector2(0.5f, 0.5f);
        knobRect.anchorMax = new Vector2(0.5f, 0.5f);
        knobRect.pivot = new Vector2(0.5f, 0.5f);
        knobRect.anchoredPosition = Vector2.zero;
        Image knobImage = knobGo.GetComponent<Image>();
        knobImage.sprite = uiSprite;
        knobImage.color = new Color(1f, 1f, 1f, 0.6f);
        knobImage.raycastTarget = false;
        DpSize knobSize = knobGo.GetComponent<DpSize>();
        knobSize.canvas = canvas;
        knobSize.widthDp = 64f;
        knobSize.heightDp = 64f;
        stickGo.GetComponent<OnScreenStick>().knob = knobRect;

        // Action buttons, bottom-right, 84 dp each with 12 dp gap (offsets are measured from the bottom-right anchor).
        CreateButton(safeGo.transform, canvas, font, uiSprite, "JumpButton", OnScreenButtonType.Jump, "JUMP", new Vector2(-58f, 58f), 84f);
        CreateButton(safeGo.transform, canvas, font, uiSprite, "FireButton", OnScreenButtonType.Fire, "FIRE", new Vector2(-154f, 58f), 84f);

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        new GameObject("GameSetup", typeof(FrameRateSetup));

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }

    static void BuildApk()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ApkRelativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log("[PrototypeBuilder] APK result=" + summary.result + " size=" + summary.totalSize + " output=" + outputPath);
        if (summary.result != BuildResult.Succeeded)
        {
            throw new Exception("APK build failed: " + summary.result);
        }
    }

    static void CreateButton(Transform parent, Canvas canvas, Font font, Sprite sprite, string name, OnScreenButtonType type, string label, Vector2 offsetDp, float sizeDp)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(OnScreenButton), typeof(DpSize));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = new Color(1f, 1f, 1f, 0.35f);
        go.GetComponent<OnScreenButton>().type = type;

        DpSize dp = go.GetComponent<DpSize>();
        dp.canvas = canvas;
        dp.widthDp = sizeDp;
        dp.heightDp = sizeDp;
        dp.offsetDp = offsetDp;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(go.transform, false);
        StretchFull((RectTransform)labelGo.transform);
        Text text = labelGo.GetComponent<Text>();
        text.font = font;
        text.text = label;
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    static void CreateEnemy(Sprite sprite, Vector3 position, float patrol)
    {
        GameObject go = new GameObject("Enemy");
        go.transform.position = position;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 9;
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.9f, 0.9f);
        Enemy enemy = go.AddComponent<Enemy>();
        enemy.patrolDistance = patrol;
    }

    static Bullet CreateBulletPrefab(Sprite sprite)
    {
        GameObject go = new GameObject("Bullet");
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 11;
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.375f, 0.1875f);
        collider.isTrigger = true;
        go.AddComponent<Bullet>();

        string path = PrefabDir + "/Bullet.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        return prefab.GetComponent<Bullet>();
    }

    // Read-only diagnostic: open the stage and report every sprite reference and the GUID the editor assigns to its PNG.
    public static void DiagnoseBatch()
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath);
            foreach (SpriteRenderer sr in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())
            {
                string assetPath = sr.sprite == null ? "NULL" : AssetDatabase.GetAssetPath(sr.sprite);
                Debug.Log("[Diag] " + sr.name + " sprite=" + (sr.sprite == null ? "NULL" : sr.sprite.name) + " path=" + assetPath);
            }
            Debug.Log("[Diag] guid(player.png)=" + AssetDatabase.AssetPathToGUID(ArtDir + "/player.png"));
            Debug.Log("[Diag] guid(tile_ground.png)=" + AssetDatabase.AssetPathToGUID(ArtDir + "/tile_ground.png"));
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[Diag] failed: " + e);
            EditorApplication.Exit(1);
        }
    }

    static WeaponData CreateWeapon()
    {
        string path = DataDir + "/DefaultWeapon.asset";
        WeaponData weapon = ScriptableObject.CreateInstance<WeaponData>();
        weapon.fireRate = 8f;
        weapon.bulletSpeed = 12f;
        weapon.bulletLifetime = 1.2f;
        weapon.damage = 1;
        AssetDatabase.CreateAsset(weapon, path);
        return AssetDatabase.LoadAssetAtPath<WeaponData>(path);
    }

    static Tile CreateTile(Sprite sprite, string name)
    {
        string path = TileDir + "/" + name + ".asset";
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.Grid;
        AssetDatabase.CreateAsset(tile, path);
        return AssetDatabase.LoadAssetAtPath<Tile>(path);
    }

    static void PaintRow(Tilemap map, Tile tile, int fromX, int toX, int y)
    {
        for (int x = fromX; x <= toX; x++)
        {
            map.SetTile(new Vector3Int(x, y, 0), tile);
        }
    }

    // Generates a bordered solid-colour PNG and imports it as a pixel-art sprite (16 PPU, point filter, uncompressed).
    static Sprite MakeSprite(string name, int width, int height, Color fill, Color border)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool edge = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                tex.SetPixel(x, y, edge ? border : fill);
            }
        }
        tex.Apply();

        string path = ArtDir + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void AddTagIfMissing(string tag)
    {
        string[] tags = UnityEditorInternal.InternalEditorUtility.tags;
        if (Array.IndexOf(tags, tag) < 0)
        {
            UnityEditorInternal.InternalEditorUtility.AddTag(tag);
        }
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
