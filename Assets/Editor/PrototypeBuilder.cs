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
    const string Stage1Path = "Assets/Scenes/Stage01.unity";
    const string Stage2Path = "Assets/Scenes/Stage02.unity";
    const string Stage3Path = "Assets/Scenes/Stage03.unity";
    const string ApkRelativePath = "Builds/android/Prototype.apk";

    // Art, prefabs and UI sprites every stage shares; generated once per run.
    sealed class Shared
    {
        public Sprite[] playerFrames;
        public Bullet bulletPrefab;
        public EnemyBullet enemyBulletPrefab;
        public WeaponData weapon;
        public Sprite enemyBarSprite;
        public Sprite cloudSprite;
        public Sprite vignetteSprite;
        public Sprite goalFlagSprite;
        public Sprite muzzleFlashSprite;
        public SpriteBurst deathBurstPrefab;
        public Font font;
        public Sprite uiCircle;
        public Sprite uiRing;
        public Sprite uiKnob;
        public Sprite uiButton;
        public Sprite uiArrow;
        public Sprite uiBulletIcon;
    }

    // Everything that varies between stages: theme art, palette, layout and spawns.
    sealed class LevelSpec
    {
        public string scenePath;
        public string stageLabel;

        public string groundTileName;
        public string fillTileName;
        public string platformTileName;
        public Sprite[] groundTops;
        public Sprite[] fillTiles;
        public Sprite platformSprite;
        public Sprite pitBackingSprite;
        public Sprite depthShadeSprite;

        public Sprite[] enemyFrames;
        public Sprite skySprite;
        public Sprite mountainSprite;
        public Sprite treelineSprite;
        public Color cameraBackground;
        public Color lightTint;

        public int paintFromX;
        public int paintToX;
        public Vector2Int[] pits;
        public int[][] platforms;
        public Vector3[] enemies; // x, y, patrol distance in z

        public float playerX;
        public float playerY;
        public float wallLeftX;
        public float wallRightX;
        public float camMinX;
        public float camMaxX;
        public float camMaxY;

        public Sprite goalSprite;
        public Vector3 goalPosition;
        public string nextScene;
        public bool stageClearLabel;

        // Difficulty tuning: later stages fire faster and from further away.
        public float enemyFireInterval = 2.6f;
        public float enemyFireRange = 10f;
        public int enemyHealth = 4;

        public Sprite[] decoSmall;
        public Sprite[] decoBush;
        public Sprite decoRock;
        public Sprite decoPine;
        public int decoSeed;
        public float decoFromX;
        public float decoToX;

        public bool ambientParticles;
        public Sprite ambientSprite;
        public int ambientCount = 90;
        public float ambientSpeedMin = 0.5f;
        public float ambientSpeedMax = 1.5f;
        public float ambientWindX;
    }

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
            BuildApk(true, ApkRelativePath);
            Debug.Log("[PrototypeBuilder] BuildApkBatch finished");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[PrototypeBuilder] BuildApkBatch failed: " + e);
            EditorApplication.Exit(1);
        }
    }

    // Release build: no Development Build overlay (the on-screen watermark) and no debugger.
    public static void BuildReleaseApkBatch()
    {
        try
        {
            BuildApk(false, "Builds/android/Prototype-release.apk");
            Debug.Log("[PrototypeBuilder] BuildReleaseApkBatch finished");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[PrototypeBuilder] BuildReleaseApkBatch failed: " + e);
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

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(Stage1Path, true),
            new EditorBuildSettingsScene(Stage2Path, true),
            new EditorBuildSettingsScene(Stage3Path, true)
        };
        AssetDatabase.SaveAssets();
    }

    // Plan steps 6-7: placeholder art, data asset, prefabs, both stages and touch UI.
    static void BuildScene()
    {
        Directory.CreateDirectory(ArtDir);
        Directory.CreateDirectory(TileDir);
        Directory.CreateDirectory(PrefabDir);
        Directory.CreateDirectory(DataDir);

        Shared shared = new Shared
        {
            playerFrames = PixelArt.PlayerFrames(),
            bulletPrefab = CreateBulletPrefab(PixelArt.BulletSprite()),
            enemyBulletPrefab = CreateEnemyBulletPrefab(PixelArt.EnemyBulletSprite()),
            weapon = CreateWeapon(),
            enemyBarSprite = PixelArt.SolidSprite("enemy_bar", Color.white),
            cloudSprite = PixelArt.CloudSprite(5001),
            vignetteSprite = PixelArt.VignetteSprite(),
            goalFlagSprite = PixelArt.GoalFlagSprite(),
            muzzleFlashSprite = PixelArt.MuzzleFlashSprite(),
            deathBurstPrefab = CreateBurstPrefab(),
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
            uiCircle = PixelArt.UiCircle("ui_circle", 256),
            uiRing = PixelArt.UiRing("ui_ring", 256, 7f),
            uiKnob = PixelArt.UiCircle("ui_knob", 128),
            uiButton = PixelArt.UiRoundedRect("ui_button", 256, 60f),
            uiArrow = PixelArt.UiArrow(),
            uiBulletIcon = PixelArt.UiBulletIcon()
        };

        BuildLevel(Stage1Spec(shared), shared);
        BuildLevel(Stage2Spec(shared), shared);
        BuildLevel(Stage3Spec(shared), shared);

        PixelArt.WritePreview();
    }

    // Level 1: jungle grassland, unchanged layout, now ends with a flag that loads level 2.
    static LevelSpec Stage1Spec(Shared shared)
    {
        return new LevelSpec
        {
            scenePath = Stage1Path,
            stageLabel = "STAGE 1",

            groundTileName = "GroundTop",
            fillTileName = "DirtFill",
            platformTileName = "Platform",
            groundTops = PixelArt.GroundTops(),
            fillTiles = PixelArt.DirtFills(),
            platformSprite = PixelArt.PlatformSprite(),
            pitBackingSprite = PixelArt.SolidSprite("pit_backing", new Color(0.10f, 0.07f, 0.05f, 1f)),
            depthShadeSprite = PixelArt.DepthShadeSprite(),

            enemyFrames = PixelArt.EnemyFrames(),
            skySprite = PixelArt.SkySprite(),
            mountainSprite = PixelArt.MountainSprite(),
            treelineSprite = PixelArt.TreelineSprite(),
            cameraBackground = new Color(0.45f, 0.7f, 0.95f),
            lightTint = Color.white,

            paintFromX = -40,
            paintToX = 199,
            pits = new[]
            {
                new Vector2Int(22, 24),
                new Vector2Int(56, 58),
                new Vector2Int(92, 94),
                new Vector2Int(124, 126),
                new Vector2Int(158, 160)
            },
            platforms = new[]
            {
                new[] { 8, 12, 3 },
                new[] { 30, 34, 4 },
                new[] { 42, 46, 2 },
                new[] { 64, 68, 3 },
                new[] { 84, 88, 2 },
                new[] { 100, 104, 3 },
                new[] { 132, 136, 2 },
                new[] { 136, 140, 3 },
                new[] { 170, 174, 3 },
                new[] { 176, 180, 2 }
            },
            enemies = new[]
            {
                E(14f, 1.69f, 2f), E(28f, 1.69f, 2f), E(40f, 1.69f, 2f), E(50f, 1.69f, 2f),
                E(62f, 1.69f, 2f), E(76f, 1.69f, 2f), E(88f, 1.69f, 2f), E(100f, 1.69f, 3f),
                E(108f, 1.69f, 2f), E(114f, 1.69f, 2f), E(130f, 1.69f, 2f), E(144f, 1.69f, 2f),
                E(152f, 1.69f, 2f), E(164f, 1.69f, 2f), E(180f, 1.69f, 2f), E(184f, 1.69f, 2f),
                E(10f, 4.69f, 1.5f), E(66f, 4.69f, 1.5f), E(102f, 4.69f, 1.5f), E(138f, 4.69f, 1.5f),
                E(172f, 4.69f, 1.5f),
                E(44f, 3.69f, 1.5f), E(86f, 3.69f, 1.5f), E(134f, 3.69f, 1.5f), E(178f, 3.69f, 1.5f)
            },

            playerX = 0f,
            playerY = 2f,
            wallLeftX = -19f,
            wallRightX = 188f,
            camMinX = -20f,
            camMaxX = 180f,
            camMaxY = 5f,

            enemyFireInterval = 2.6f,
            enemyFireRange = 10f,
            enemyHealth = 4,

            goalSprite = shared.goalFlagSprite,
            goalPosition = new Vector3(185f, 2f, 0f),
            nextScene = "Stage02",
            stageClearLabel = false,

            decoSmall = PixelArt.TuftSprites(),
            decoBush = PixelArt.BushSprites(),
            decoRock = PixelArt.RockSprite(),
            decoSeed = 2024,
            decoFromX = -38f,
            decoToX = 194f
        };
    }

    // Level 2: snowfield. Snow-capped ground, ice ledges, snowy pines, snowfall and blue-grey troops.
    static LevelSpec Stage2Spec(Shared shared)
    {
        return new LevelSpec
        {
            scenePath = Stage2Path,
            stageLabel = "STAGE 2",

            groundTileName = "SnowGroundTop",
            fillTileName = "SnowFill",
            platformTileName = "IcePlatform",
            groundTops = PixelArt.SnowGroundTops(),
            fillTiles = PixelArt.SnowFills(),
            platformSprite = PixelArt.SnowPlatformSprite(),
            pitBackingSprite = PixelArt.SolidSprite("pit_backing_snow", new Color(0.04f, 0.07f, 0.11f, 1f)),
            depthShadeSprite = PixelArt.SnowDepthShadeSprite(),

            enemyFrames = PixelArt.SnowEnemyFrames(),
            skySprite = PixelArt.SnowSkySprite(),
            mountainSprite = PixelArt.SnowMountainSprite(),
            treelineSprite = PixelArt.PineTreelineSprite(),
            cameraBackground = new Color(0.56f, 0.72f, 0.88f),
            lightTint = new Color(0.88f, 0.94f, 1f),

            paintFromX = -40,
            paintToX = 199,
            pits = new[]
            {
                new Vector2Int(24, 26),
                new Vector2Int(50, 52),
                new Vector2Int(76, 78),
                new Vector2Int(102, 104),
                new Vector2Int(128, 130),
                new Vector2Int(154, 156)
            },
            platforms = new[]
            {
                new[] { 6, 10, 3 },
                new[] { 11, 15, 4 },
                new[] { 30, 34, 2 },
                new[] { 36, 40, 4 },
                new[] { 58, 62, 3 },
                new[] { 63, 67, 4 },
                new[] { 68, 72, 2 },
                new[] { 94, 98, 3 },
                new[] { 110, 114, 2 },
                new[] { 116, 120, 4 },
                new[] { 136, 140, 3 },
                new[] { 146, 150, 2 },
                new[] { 162, 166, 3 },
                new[] { 167, 171, 4 },
                new[] { 180, 184, 2 }
            },
            enemies = new[]
            {
                E(12f, 1.69f, 2f), E(20f, 1.69f, 2f), E(36f, 1.69f, 2f), E(46f, 1.69f, 2f),
                E(56f, 1.69f, 2f), E(66f, 1.69f, 2f), E(82f, 1.69f, 3f), E(92f, 1.69f, 2f),
                E(108f, 1.69f, 2f), E(118f, 1.69f, 2f), E(134f, 1.69f, 2f), E(148f, 1.69f, 2f),
                E(160f, 1.69f, 2f), E(172f, 1.69f, 2f), E(182f, 1.69f, 2f),
                E(8f, 4.69f, 1.5f), E(60f, 4.69f, 1.5f), E(96f, 4.69f, 1.5f), E(138f, 4.69f, 1.5f),
                E(164f, 4.69f, 1.5f),
                E(13f, 5.69f, 1.5f), E(38f, 5.69f, 1.5f), E(65f, 5.69f, 1.5f), E(118f, 5.69f, 1.5f),
                E(169f, 5.69f, 1.5f),
                E(32f, 3.69f, 1.5f), E(70f, 3.69f, 1.5f), E(112f, 3.69f, 1.5f), E(148f, 3.69f, 1.5f),
                E(182f, 3.69f, 1.5f)
            },

            playerX = 0f,
            playerY = 2f,
            wallLeftX = -19f,
            wallRightX = 188f,
            camMinX = -20f,
            camMaxX = 180f,
            camMaxY = 5f,

            enemyFireInterval = 2.45f,
            enemyFireRange = 11f,
            enemyHealth = 4,

            goalSprite = shared.goalFlagSprite,
            goalPosition = new Vector3(185f, 2f, 0f),
            nextScene = "Stage03",
            stageClearLabel = false,

            decoSmall = PixelArt.SnowDriftSprites(),
            decoBush = PixelArt.SnowBushSprites(),
            decoRock = PixelArt.IceRockSprite(),
            decoPine = PixelArt.PineSprite(),
            decoSeed = 3117,
            decoFromX = -38f,
            decoToX = 194f,

            ambientParticles = true,
            ambientSprite = PixelArt.SnowflakeSprite(),
            ambientCount = 70,
            ambientSpeedMin = 0.5f,
            ambientSpeedMax = 1.5f,
            ambientWindX = 0f
        };
    }

    // Level 3: desert. Sandstone mesas, dunes with cacti, warm palette and drifting dust.
    static LevelSpec Stage3Spec(Shared shared)
    {
        return new LevelSpec
        {
            scenePath = Stage3Path,
            stageLabel = "STAGE 3",

            groundTileName = "SandGroundTop",
            fillTileName = "SandFill",
            platformTileName = "SandstonePlatform",
            groundTops = PixelArt.SandGroundTops(),
            fillTiles = PixelArt.SandFills(),
            platformSprite = PixelArt.SandstonePlatformSprite(),
            pitBackingSprite = PixelArt.SolidSprite("pit_backing_desert", new Color(0.12f, 0.07f, 0.03f, 1f)),
            depthShadeSprite = PixelArt.DesertDepthShadeSprite(),

            enemyFrames = PixelArt.DesertEnemyFrames(),
            skySprite = PixelArt.DesertSkySprite(),
            mountainSprite = PixelArt.DesertMesaSprite(),
            treelineSprite = PixelArt.DesertTreelineSprite(),
            cameraBackground = new Color(0.80f, 0.72f, 0.55f),
            lightTint = new Color(1f, 0.96f, 0.88f),

            paintFromX = -40,
            paintToX = 199,
            pits = new[]
            {
                new Vector2Int(20, 22),
                new Vector2Int(44, 46),
                new Vector2Int(70, 72),
                new Vector2Int(96, 98),
                new Vector2Int(124, 126),
                new Vector2Int(152, 154)
            },
            platforms = new[]
            {
                new[] { 6, 10, 3 },
                new[] { 12, 16, 4 },
                new[] { 26, 30, 2 },
                new[] { 32, 36, 3 },
                new[] { 50, 54, 2 },
                new[] { 56, 60, 4 },
                new[] { 64, 68, 3 },
                new[] { 78, 82, 2 },
                new[] { 84, 88, 4 },
                new[] { 100, 104, 3 },
                new[] { 106, 110, 2 },
                new[] { 116, 120, 4 },
                new[] { 130, 134, 2 },
                new[] { 136, 140, 3 },
                new[] { 142, 146, 4 },
                new[] { 158, 162, 3 },
                new[] { 168, 172, 2 },
                new[] { 174, 178, 4 }
            },
            enemies = new[]
            {
                E(10f, 1.69f, 2f), E(16f, 1.69f, 2f), E(32f, 1.69f, 2f), E(42f, 1.69f, 2f),
                E(56f, 1.69f, 2f), E(62f, 1.69f, 2f), E(78f, 1.69f, 3f), E(90f, 1.69f, 2f),
                E(104f, 1.69f, 2f), E(114f, 1.69f, 2f), E(130f, 1.69f, 2f), E(144f, 1.69f, 2f),
                E(158f, 1.69f, 2f), E(168f, 1.69f, 2f), E(178f, 1.69f, 2f), E(184f, 1.69f, 2f),
                E(8f, 4.69f, 1.5f), E(34f, 4.69f, 1.5f), E(66f, 4.69f, 1.5f), E(102f, 4.69f, 1.5f),
                E(138f, 4.69f, 1.5f), E(160f, 4.69f, 1.5f),
                E(14f, 5.69f, 1.5f), E(58f, 5.69f, 1.5f), E(86f, 5.69f, 1.5f), E(118f, 5.69f, 1.5f),
                E(144f, 5.69f, 1.5f), E(176f, 5.69f, 1.5f),
                E(28f, 3.69f, 1.5f), E(52f, 3.69f, 1.5f), E(80f, 3.69f, 1.5f), E(108f, 3.69f, 1.5f),
                E(132f, 3.69f, 1.5f), E(170f, 3.69f, 1.5f)
            },

            playerX = 0f,
            playerY = 2f,
            wallLeftX = -19f,
            wallRightX = 188f,
            camMinX = -20f,
            camMaxX = 180f,
            camMaxY = 5f,

            enemyFireInterval = 2.3f,
            enemyFireRange = 12f,
            enemyHealth = 4,

            goalSprite = shared.goalFlagSprite,
            goalPosition = new Vector3(185f, 2f, 0f),
            nextScene = "",
            stageClearLabel = true,

            decoSmall = PixelArt.DesertTuftSprites(),
            decoBush = PixelArt.DesertBushSprites(),
            decoRock = PixelArt.SandRockSprite(),
            decoPine = PixelArt.CactusSprite(),
            decoSeed = 4211,
            decoFromX = -38f,
            decoToX = 194f,

            ambientParticles = true,
            ambientSprite = PixelArt.DustSprite(),
            ambientCount = 40,
            ambientSpeedMin = 0.15f,
            ambientSpeedMax = 0.45f,
            ambientWindX = 0.35f
        };
    }

    static Vector3 E(float x, float y, float patrol)
    {
        return new Vector3(x, y, patrol);
    }

    static void BuildLevel(LevelSpec spec, Shared shared)
    {
        Tile[] groundTiles = new Tile[spec.groundTops.Length];
        for (int i = 0; i < groundTiles.Length; i++) groundTiles[i] = CreateTile(spec.groundTops[i], spec.groundTileName + i);
        Tile[] fillTiles = new Tile[spec.fillTiles.Length];
        for (int i = 0; i < fillTiles.Length; i++) fillTiles[i] = CreateTile(spec.fillTiles[i], spec.fillTileName + i);
        Tile platformTile = CreateTile(spec.platformSprite, spec.platformTileName);

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
        cam.backgroundColor = spec.cameraBackground;
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
        globalLight.color = spec.lightTint;

        CreateBackground(cam, shared, spec);

        // Level: Grid > Ground tilemap.
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

        for (int x = spec.paintFromX; x <= spec.paintToX; x++)
        {
            if (IsPit(spec, x)) continue;
            int variant = (x * 92821 + 7) & 0x7fffffff;
            tilemap.SetTile(new Vector3Int(x, 0, 0), groundTiles[variant % groundTiles.Length]);
            // Fill well below the camera's bottom edge so no sky shows underneath the ground.
            for (int y = -1; y >= -5; y--)
            {
                int hash = variant + (y + 8) * 977;
                tilemap.SetTile(new Vector3Int(x, y, 0), fillTiles[hash % fillTiles.Length]);
            }
        }
        foreach (int[] row in spec.platforms) PaintRow(tilemap, platformTile, row[0], row[1], row[2]);

        CreateDecorations(spec);
        CreateDepthShade(spec);
        CreatePitBacking(spec);

        // Player: run, jump, gun with muzzle and ground check.
        GameObject playerGo = new GameObject("Player");
        playerGo.transform.position = new Vector3(spec.playerX, spec.playerY, 0f);
        SpriteRenderer playerRenderer = playerGo.AddComponent<SpriteRenderer>();
        playerRenderer.sprite = shared.playerFrames[0];
        playerRenderer.sortingOrder = 10;
        Rigidbody2D playerBody = playerGo.AddComponent<Rigidbody2D>();
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D playerCollider = playerGo.AddComponent<BoxCollider2D>();
        playerCollider.size = new Vector2(0.8f, 1.4f);
        PlayerController controller = playerGo.AddComponent<PlayerController>();
        controller.jumpSpeed = 14f;
        controller.maxHealth = 5;
        controller.invulnerableTime = 1.2f;
        CharacterAnimator playerAnimator = playerGo.AddComponent<CharacterAnimator>();
        playerAnimator.frames = shared.playerFrames;
        playerAnimator.fps = 8f;
        // Frames 0..2 are the walk cycle; frame 3 is the airborne pose.
        playerAnimator.walkFrameCount = 3;
        playerAnimator.airborneFrame = 3;

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(playerGo.transform, false);
        groundCheck.transform.localPosition = new Vector3(0f, -0.75f, 0f);
        controller.groundCheck = groundCheck.transform;

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(playerGo.transform, false);
        // Matches the gun tip in the 16x24 sprite (see PixelArt.PlayerUpper).
        muzzle.transform.localPosition = new Vector3(0.42f, -0.08f, 0f);

        // Muzzle flash lives at the muzzle tip (the muzzle itself mirrors when the player turns).
        GameObject flashGo = new GameObject("MuzzleFlash");
        flashGo.transform.SetParent(muzzle.transform, false);
        flashGo.transform.localPosition = Vector3.zero;
        SpriteRenderer flashRenderer = flashGo.AddComponent<SpriteRenderer>();
        flashRenderer.sprite = shared.muzzleFlashSprite;
        flashRenderer.sortingOrder = 12;
        flashRenderer.enabled = false;
        MuzzleFlash flash = flashGo.AddComponent<MuzzleFlash>();
        flash.flash = flashRenderer;

        Gun gun = playerGo.AddComponent<Gun>();
        gun.weapon = shared.weapon;
        gun.bulletPrefab = shared.bulletPrefab;
        gun.muzzle = muzzle.transform;
        gun.muzzleFlash = flash;
        controller.gun = gun;
        controller.muzzle = muzzle.transform;

        foreach (Vector3 spawn in spec.enemies)
        {
            CreateEnemy(spec.enemyFrames, new Vector3(spawn.x, spawn.y, 0f), spawn.z,
                shared.enemyBulletPrefab, shared.enemyBarSprite, shared.deathBurstPrefab,
                spec.enemyFireInterval, spec.enemyFireRange, spec.enemyHealth);
        }

        CameraFollow2D follow = cam.gameObject.AddComponent<CameraFollow2D>();
        follow.target = playerGo.transform;
        follow.minX = spec.camMinX;
        follow.maxX = spec.camMaxX;
        follow.maxY = spec.camMaxY;

        // Invisible walls keep the player inside the painted level.
        CreateInvisibleWall(spec.wallLeftX, 8f);
        CreateInvisibleWall(spec.wallRightX, 8f);

        GameObject safeGo = SetupTouchUI(shared, cam, controller, shared.playerFrames[0], spec.stageLabel, playerGo.transform);
        CreateGoal(spec, safeGo.transform, shared);

        if (spec.ambientParticles && spec.ambientSprite != null)
        {
            GameObject ambientGo = new GameObject("AmbientParticles");
            Snowfall snow = ambientGo.AddComponent<Snowfall>();
            snow.flakeSprite = spec.ambientSprite;
            snow.count = spec.ambientCount;
            snow.speedMin = spec.ambientSpeedMin;
            snow.speedMax = spec.ambientSpeedMax;
            snow.windX = spec.ambientWindX;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        new GameObject("GameSetup", typeof(FrameRateSetup), typeof(PhysicsSetup));

        EditorSceneManager.SaveScene(scene, spec.scenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[PrototypeBuilder] built " + spec.scenePath);
    }

    static void BuildApk(bool development, string relativePath)
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { Stage1Path, Stage2Path, Stage3Path },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = development
                ? BuildOptions.Development | BuildOptions.AllowDebugging
                : BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log("[PrototypeBuilder] APK development=" + development + " result=" + summary.result
            + " size=" + summary.totalSize + " output=" + outputPath);
        if (summary.result != BuildResult.Succeeded)
        {
            throw new Exception("APK build failed: " + summary.result);
        }
    }

    // Touch UI: Screen Space Overlay canvas, safe-area panel, joystick, two buttons, stage label, FPS label.
    static GameObject SetupTouchUI(Shared shared, Camera cam, PlayerController controller, Sprite portrait,
        string stageLabel, Transform player)
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = shared.font;

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
        fpsCounter.player = player;
        fpsCounter.showDebugInfo = false;

        // Stage label, top centre.
        GameObject stageGo = new GameObject("StageLabel", typeof(RectTransform), typeof(Text), typeof(Shadow));
        stageGo.transform.SetParent(safeGo.transform, false);
        RectTransform stageRect = (RectTransform)stageGo.transform;
        stageRect.anchorMin = new Vector2(0.5f, 1f);
        stageRect.anchorMax = new Vector2(0.5f, 1f);
        stageRect.pivot = new Vector2(0.5f, 1f);
        stageRect.anchoredPosition = new Vector2(0f, -24f);
        stageRect.sizeDelta = new Vector2(600f, 60f);
        Text stageText = stageGo.GetComponent<Text>();
        stageText.font = font;
        stageText.text = stageLabel;
        stageText.fontSize = 34;
        stageText.alignment = TextAnchor.MiddleCenter;
        stageText.color = new Color(1f, 1f, 1f, 0.9f);
        stageText.raycastTarget = false;
        Shadow stageShadow = stageGo.GetComponent<Shadow>();
        stageShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        stageShadow.effectDistance = new Vector2(2f, -2f);

        // Player health bar, top-left under the FPS readout: portrait plus a red fill that shrinks left to right.
        GameObject healthGo = new GameObject("HealthBar", typeof(RectTransform), typeof(PlayerHealthBar));
        healthGo.transform.SetParent(safeGo.transform, false);
        RectTransform healthRect = (RectTransform)healthGo.transform;
        healthRect.anchorMin = new Vector2(0f, 1f);
        healthRect.anchorMax = new Vector2(0f, 1f);
        healthRect.pivot = new Vector2(0f, 1f);
        healthRect.anchoredPosition = new Vector2(24f, -86f);
        healthRect.sizeDelta = new Vector2(300f, 44f);

        GameObject portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitGo.transform.SetParent(healthGo.transform, false);
        RectTransform portraitRect = (RectTransform)portraitGo.transform;
        portraitRect.anchorMin = Vector2.zero;
        portraitRect.anchorMax = Vector2.zero;
        portraitRect.pivot = Vector2.zero;
        portraitRect.anchoredPosition = Vector2.zero;
        portraitRect.sizeDelta = new Vector2(36f, 44f);
        Image portraitImage = portraitGo.GetComponent<Image>();
        portraitImage.sprite = portrait;
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;

        GameObject barBgGo = new GameObject("BarBackground", typeof(RectTransform), typeof(Image));
        barBgGo.transform.SetParent(healthGo.transform, false);
        RectTransform barBgRect = (RectTransform)barBgGo.transform;
        barBgRect.anchorMin = new Vector2(0f, 0f);
        barBgRect.anchorMax = new Vector2(0f, 1f);
        barBgRect.pivot = new Vector2(0f, 0.5f);
        barBgRect.anchoredPosition = new Vector2(44f, 0f);
        barBgRect.sizeDelta = new Vector2(248f, -14f);
        Image barBgImage = barBgGo.GetComponent<Image>();
        barBgImage.sprite = shared.uiButton;
        barBgImage.color = new Color(0.05f, 0.04f, 0.08f, 0.6f);
        barBgImage.raycastTarget = false;

        GameObject healthFillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        healthFillGo.transform.SetParent(barBgGo.transform, false);
        RectTransform healthFillRect = (RectTransform)healthFillGo.transform;
        healthFillRect.anchorMin = Vector2.zero;
        healthFillRect.anchorMax = Vector2.one;
        healthFillRect.offsetMin = new Vector2(5f, 5f);
        healthFillRect.offsetMax = new Vector2(-5f, -5f);
        Image healthFillImage = healthFillGo.GetComponent<Image>();
        healthFillImage.sprite = shared.uiButton;
        healthFillImage.color = new Color(0.92f, 0.28f, 0.20f, 1f);
        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Horizontal;
        healthFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthFillImage.fillAmount = 1f;
        healthFillImage.raycastTarget = false;

        PlayerHealthBar healthBar = healthGo.GetComponent<PlayerHealthBar>();
        healthBar.player = controller;
        healthBar.fill = healthFillImage;

        GameObject stickGo = new GameObject("Stick", typeof(RectTransform), typeof(Image), typeof(OnScreenStick), typeof(DpSize));
        stickGo.transform.SetParent(safeGo.transform, false);
        RectTransform stickRect = (RectTransform)stickGo.transform;
        stickRect.anchorMin = new Vector2(0f, 0f);
        stickRect.anchorMax = new Vector2(0f, 0f);
        stickRect.pivot = new Vector2(0.5f, 0.5f);
        stickRect.anchoredPosition = Vector2.zero;
        Image stickImage = stickGo.GetComponent<Image>();
        stickImage.sprite = shared.uiCircle;
        stickImage.color = new Color(1f, 1f, 1f, 0.12f);
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
        // Thin outer ring so the stick stays readable over busy ground textures.
        GameObject ringGo = new GameObject("Ring", typeof(RectTransform), typeof(Image), typeof(DpSize));
        ringGo.transform.SetParent(stickGo.transform, false);
        RectTransform ringRect = (RectTransform)ringGo.transform;
        ringRect.anchorMin = new Vector2(0.5f, 0.5f);
        ringRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringRect.pivot = new Vector2(0.5f, 0.5f);
        Image ringImage = ringGo.GetComponent<Image>();
        ringImage.sprite = shared.uiRing;
        ringImage.color = new Color(1f, 1f, 1f, 0.34f);
        ringImage.raycastTarget = false;
        DpSize ringSize = ringGo.GetComponent<DpSize>();
        ringSize.canvas = canvas;
        ringSize.widthDp = 150f;
        ringSize.heightDp = 150f;

        Image knobImage = knobGo.GetComponent<Image>();
        knobImage.sprite = shared.uiKnob;
        knobImage.color = new Color(1f, 1f, 1f, 0.72f);
        knobImage.raycastTarget = false;
        DpSize knobSize = knobGo.GetComponent<DpSize>();
        knobSize.canvas = canvas;
        knobSize.widthDp = 60f;
        knobSize.heightDp = 60f;
        stickGo.GetComponent<OnScreenStick>().knob = knobRect;

        // Action buttons, bottom-right, 92 dp each with 10 dp gap (offsets are measured from the bottom-right anchor).
        CreateButton(safeGo.transform, canvas, font, shared.uiCircle, shared.uiArrow, "JumpButton", OnScreenButtonType.Jump, "JUMP",
            new Color(0.16f, 0.42f, 0.24f, 0.72f), new Color(0.32f, 0.62f, 0.36f, 0.42f),
            new Vector2(-68f, 68f), 92f, 36f, 30f, 12f);
        CreateButton(safeGo.transform, canvas, font, shared.uiCircle, shared.uiBulletIcon, "FireButton", OnScreenButtonType.Fire, "FIRE",
            new Color(0.66f, 0.19f, 0.14f, 0.72f), new Color(0.86f, 0.34f, 0.23f, 0.42f),
            new Vector2(-170f, 68f), 92f, 46f, 19f, 14f);

        // AUTO toggle, top-right: tap to switch auto-fire on or off.
        GameObject autoGo = new GameObject("AutoFireToggle", typeof(RectTransform), typeof(Image), typeof(OnScreenAutoFireToggle), typeof(DpSize));
        autoGo.transform.SetParent(safeGo.transform, false);
        RectTransform autoRect = (RectTransform)autoGo.transform;
        autoRect.anchorMin = new Vector2(1f, 1f);
        autoRect.anchorMax = new Vector2(1f, 1f);
        autoRect.pivot = new Vector2(0.5f, 0.5f);
        autoRect.anchoredPosition = Vector2.zero;
        Image autoImage = autoGo.GetComponent<Image>();
        autoImage.sprite = shared.uiButton;
        autoImage.color = new Color(1f, 1f, 1f, 0.28f);
        DpSize autoSize = autoGo.GetComponent<DpSize>();
        autoSize.canvas = canvas;
        autoSize.widthDp = 122f;
        autoSize.heightDp = 58f;
        autoSize.offsetDp = new Vector2(-78f, -50f);

        GameObject autoLabelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        autoLabelGo.transform.SetParent(autoGo.transform, false);
        StretchFull((RectTransform)autoLabelGo.transform);
        Text autoText = autoLabelGo.GetComponent<Text>();
        autoText.font = font;
        autoText.fontSize = 25;
        autoText.alignment = TextAnchor.MiddleCenter;
        autoText.color = Color.white;
        autoText.raycastTarget = false;
        OnScreenAutoFireToggle autoToggle = autoGo.GetComponent<OnScreenAutoFireToggle>();
        autoToggle.label = autoText;
        autoToggle.background = autoImage;

        return safeGo;
    }

    static void CreateGoal(LevelSpec spec, Transform uiParent, Shared shared)
    {
        if (spec.goalSprite == null) return;

        GameObject go = new GameObject("Goal");
        go.transform.position = spec.goalPosition;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = spec.goalSprite;
        renderer.sortingOrder = 5;

        BoxCollider2D trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.4f, 3f);

        LevelGoal goal = go.AddComponent<LevelGoal>();
        goal.nextScene = spec.nextScene;
        if (string.IsNullOrEmpty(spec.nextScene) && spec.stageClearLabel)
        {
            goal.clearMessage = CreateClearLabel(uiParent, shared.font);
        }
    }

    // Hidden overlay shown by the final stage's goal.
    static GameObject CreateClearLabel(Transform parent, Font font)
    {
        GameObject go = new GameObject("ClearMessage", typeof(RectTransform), typeof(Text), typeof(Shadow));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1400f, 220f);

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = "STAGE CLEAR";
        text.fontSize = 96;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.9f, 0.4f);
        text.raycastTarget = false;

        Shadow shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(4f, -4f);

        go.SetActive(false);
        return go;
    }

    static void CreateButton(Transform parent, Canvas canvas, Font font, Sprite circle, Sprite icon, string name,
        OnScreenButtonType type, string label, Color baseColor, Color innerColor,
        Vector2 offsetDp, float sizeDp, float iconWidthDp, float iconHeightDp, float iconOffsetYDp)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(OnScreenButton), typeof(DpSize));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image image = go.GetComponent<Image>();
        image.sprite = circle;
        image.color = baseColor;

        DpSize dp = go.GetComponent<DpSize>();
        dp.canvas = canvas;
        dp.widthDp = sizeDp;
        dp.heightDp = sizeDp;
        dp.offsetDp = offsetDp;

        OnScreenButton button = go.GetComponent<OnScreenButton>();
        button.type = type;
        button.image = image;

        // Lighter inner disc gives the button a raised face.
        GameObject innerGo = new GameObject("Inner", typeof(RectTransform), typeof(Image), typeof(DpSize));
        innerGo.transform.SetParent(go.transform, false);
        RectTransform innerRect = (RectTransform)innerGo.transform;
        innerRect.anchorMin = new Vector2(0.5f, 0.5f);
        innerRect.anchorMax = new Vector2(0.5f, 0.5f);
        innerRect.pivot = new Vector2(0.5f, 0.5f);
        Image innerImage = innerGo.GetComponent<Image>();
        innerImage.sprite = circle;
        innerImage.color = innerColor;
        innerImage.raycastTarget = false;
        DpSize innerSize = innerGo.GetComponent<DpSize>();
        innerSize.canvas = canvas;
        innerSize.widthDp = sizeDp - 12f;
        innerSize.heightDp = sizeDp - 12f;

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(DpSize));
        iconGo.transform.SetParent(go.transform, false);
        RectTransform iconRect = (RectTransform)iconGo.transform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        Image iconImage = iconGo.GetComponent<Image>();
        iconImage.sprite = icon;
        iconImage.color = new Color(1f, 1f, 1f, 0.95f);
        iconImage.raycastTarget = false;
        DpSize iconSize = iconGo.GetComponent<DpSize>();
        iconSize.canvas = canvas;
        iconSize.widthDp = iconWidthDp;
        iconSize.heightDp = iconHeightDp;
        iconSize.offsetDp = new Vector2(0f, iconOffsetYDp);

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(DpSize));
        labelGo.transform.SetParent(go.transform, false);
        RectTransform labelRect = (RectTransform)labelGo.transform;
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        Text text = labelGo.GetComponent<Text>();
        text.font = font;
        text.text = label;
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 1f, 1f, 0.92f);
        text.raycastTarget = false;
        DpSize labelSize = labelGo.GetComponent<DpSize>();
        labelSize.canvas = canvas;
        labelSize.widthDp = sizeDp;
        labelSize.heightDp = 26f;
        labelSize.offsetDp = new Vector2(0f, 14f);
    }

    static void CreateEnemy(Sprite[] frames, Vector3 position, float patrol,
        EnemyBullet bulletPrefab, Sprite barSprite, SpriteBurst deathBurst,
        float fireInterval, float fireRange, int health)
    {
        GameObject go = new GameObject("Enemy");
        go.transform.position = position;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[0];
        renderer.sortingOrder = 9;
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        // Covers the torso; the sprite is 22 px tall, so the head is at y=0.6875.
        collider.size = new Vector2(0.9f, 1.1f);
        collider.offset = new Vector2(0f, 0.05f);
        Enemy enemy = go.AddComponent<Enemy>();
        enemy.patrolDistance = patrol;
        enemy.maxHealth = health;
        enemy.bulletPrefab = bulletPrefab;
        enemy.deathBurst = deathBurst;
        enemy.fireRange = fireRange;
        enemy.fireInterval = fireInterval;
        CharacterAnimator animator = go.AddComponent<CharacterAnimator>();
        animator.frames = frames;
        animator.fps = 6f;

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(go.transform, false);
        muzzle.transform.localPosition = new Vector3(0.4f, -0.02f, 0f);
        enemy.muzzle = muzzle.transform;

        // Health bar above the head. The solid sprite is 0.5 units, hence the 2x scales.
        GameObject barBg = new GameObject("HealthBarBg");
        barBg.transform.SetParent(go.transform, false);
        barBg.transform.localPosition = new Vector3(0f, 0.82f, 0f);
        barBg.transform.localScale = new Vector3(1.96f, 0.26f, 1f);
        SpriteRenderer barBgRenderer = barBg.AddComponent<SpriteRenderer>();
        barBgRenderer.sprite = barSprite;
        barBgRenderer.color = new Color(0.05f, 0.04f, 0.06f, 0.75f);
        barBgRenderer.sortingOrder = 12;

        GameObject barFill = new GameObject("HealthBarFill");
        barFill.transform.SetParent(go.transform, false);
        barFill.transform.localPosition = new Vector3(0f, 0.82f, 0f);
        barFill.transform.localScale = new Vector3(1.8f, 0.18f, 1f);
        SpriteRenderer barFillRenderer = barFill.AddComponent<SpriteRenderer>();
        barFillRenderer.sprite = barSprite;
        barFillRenderer.color = new Color(0.93f, 0.30f, 0.22f, 1f);
        barFillRenderer.sortingOrder = 13;
        enemy.healthFill = barFill.transform;
    }

    // Static collider with no renderer: blocks the player at the ends of the level.
    static void CreateInvisibleWall(float x, float centerY)
    {
        GameObject go = new GameObject("Wall");
        go.transform.position = new Vector3(x, centerY, 0f);
        BoxCollider2D wall = go.AddComponent<BoxCollider2D>();
        wall.size = new Vector2(1f, 24f);
    }

    // Sky, clouds, mountain range, treeline and a soft vignette.
    static void CreateBackground(Camera cam, Shared shared, LevelSpec spec)
    {
        Sprite skySprite = spec.skySprite;
        GameObject skyGo = new GameObject("Sky");
        skyGo.transform.SetParent(cam.transform, false);
        skyGo.transform.localPosition = new Vector3(0f, 0f, 5f);
        SpriteRenderer skyRenderer = skyGo.AddComponent<SpriteRenderer>();
        skyRenderer.sprite = skySprite;
        skyRenderer.sortingOrder = -1000;
        skyGo.transform.localScale = new Vector3(
            44f / (skySprite.rect.width / 16f),
            15f / (skySprite.rect.height / 16f),
            1f);

        Sprite cloudSprite = shared.cloudSprite;
        GameObject cloudsGo = new GameObject("Clouds");
        ParallaxLayer cloudParallax = cloudsGo.AddComponent<ParallaxLayer>();
        cloudParallax.cameraTransform = cam.transform;
        cloudParallax.factor = 0.92f;
        for (int i = 0; i < 18; i++)
        {
            GameObject cloud = new GameObject("Cloud" + i);
            cloud.transform.SetParent(cloudsGo.transform, false);
            cloud.transform.position = new Vector3(
                -28f + i * 15f + (i * 37) % 7,
                4.6f + (i * 53) % 4 * 0.6f,
                0f);
            cloud.transform.localScale = Vector3.one * (0.9f + (i * 29) % 3 * 0.2f);
            SpriteRenderer cloudRenderer = cloud.AddComponent<SpriteRenderer>();
            cloudRenderer.sprite = cloudSprite;
            cloudRenderer.sortingOrder = -980;
        }

        Sprite mountainSprite = spec.mountainSprite;
        GameObject mountainsGo = new GameObject("Mountains");
        mountainsGo.transform.position = new Vector3(20f, 3f, 0f);
        SpriteRenderer mountainRenderer = mountainsGo.AddComponent<SpriteRenderer>();
        mountainRenderer.sprite = mountainSprite;
        mountainRenderer.sortingOrder = -960;
        ParallaxLayer mountainParallax = mountainsGo.AddComponent<ParallaxLayer>();
        mountainParallax.cameraTransform = cam.transform;
        mountainParallax.factor = 0.85f;

        Sprite treelineSprite = spec.treelineSprite;
        GameObject treelineGo = new GameObject("Treeline");
        treelineGo.transform.position = new Vector3(20f, 3.3f, 0f);
        SpriteRenderer treelineRenderer = treelineGo.AddComponent<SpriteRenderer>();
        treelineRenderer.sprite = treelineSprite;
        treelineRenderer.sortingOrder = -940;
        ParallaxLayer treelineParallax = treelineGo.AddComponent<ParallaxLayer>();
        treelineParallax.cameraTransform = cam.transform;
        treelineParallax.factor = 0.6f;

        Sprite vignetteSprite = shared.vignetteSprite;
        GameObject vignetteGo = new GameObject("Vignette");
        vignetteGo.transform.SetParent(cam.transform, false);
        vignetteGo.transform.localPosition = new Vector3(0f, 0f, 4f);
        SpriteRenderer vignetteRenderer = vignetteGo.AddComponent<SpriteRenderer>();
        vignetteRenderer.sprite = vignetteSprite;
        vignetteRenderer.sortingOrder = 1000;
        vignetteGo.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
    }

    // Dark quad behind the tilemap: pit shafts otherwise show the camera-space sky as a bright column.
    static void CreatePitBacking(LevelSpec spec)
    {
        Sprite solid = spec.pitBackingSprite;
        GameObject go = new GameObject("PitBacking");
        go.transform.position = new Vector3(80f, -2.5f, 0f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = solid;
        renderer.sortingOrder = -5;
        go.transform.localScale = new Vector3(
            240f / (solid.rect.width / 16f),
            7f / (solid.rect.height / 16f),
            1f);
    }

    // Darkens the dirt towards the bottom of the screen; one gradient quad avoids per-tile banding.
    static void CreateDepthShade(LevelSpec spec)
    {
        Sprite shade = spec.depthShadeSprite;
        GameObject go = new GameObject("DepthShade");
        go.transform.position = new Vector3(80f, -1.5f, 0f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = shade;
        renderer.sortingOrder = 1;
        go.transform.localScale = new Vector3(
            240f / (shade.rect.width / 16f),
            5f / (shade.rect.height / 16f),
            1f);
    }

    // Drifts, bushes, rocks and (on the snow stage) pines along the ground line. No colliders.
    static void CreateDecorations(LevelSpec spec)
    {
        GameObject root = new GameObject("Decorations");
        System.Random rng = new System.Random(spec.decoSeed);

        float x = spec.decoFromX;
        while (x < spec.decoToX)
        {
            if (!IsNearPit(spec, x))
            {
                int roll = rng.Next(100);
                if (roll < 55 && spec.decoSmall.Length > 0)
                {
                    Sprite sprite = spec.decoSmall[rng.Next(spec.decoSmall.Length)];
                    float scale = 0.9f + (float)rng.NextDouble() * 0.6f;
                    PlaceDecoration(root, sprite, new Vector3(x, 1f + sprite.bounds.extents.y * scale, 0f), 2, scale);
                }
                else if (roll < 78 && spec.decoBush.Length > 0)
                {
                    Sprite sprite = spec.decoBush[rng.Next(spec.decoBush.Length)];
                    float scale = 0.9f + (float)rng.NextDouble() * 0.5f;
                    PlaceDecoration(root, sprite, new Vector3(x, 1f + sprite.bounds.extents.y * scale, 0f), 3, scale);
                }
                else if (roll < 92 && spec.decoRock != null)
                {
                    float scale = 0.8f + (float)rng.NextDouble() * 0.7f;
                    PlaceDecoration(root, spec.decoRock, new Vector3(x, 1f + spec.decoRock.bounds.extents.y * scale, 0f), 2, scale);
                }
            }
            x += 1.4f + (float)rng.NextDouble() * 2.2f;
        }

        if (spec.decoPine != null)
        {
            float px = spec.decoFromX + 6f;
            while (px < spec.decoToX)
            {
                if (!IsNearPit(spec, px) && rng.NextDouble() < 0.55)
                {
                    float scale = 0.9f + (float)rng.NextDouble() * 0.5f;
                    PlaceDecoration(root, spec.decoPine,
                        new Vector3(px, 1f + spec.decoPine.bounds.extents.y * scale, 0f), 4, scale);
                }
                px += 10f + (float)rng.NextDouble() * 10f;
            }
        }
    }

    static void PlaceDecoration(GameObject root, Sprite sprite, Vector3 position, int sortingOrder, float scale)
    {
        GameObject go = new GameObject("Decor");
        go.transform.SetParent(root.transform, true);
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
    }

    static bool IsPit(LevelSpec spec, int x)
    {
        foreach (Vector2Int pit in spec.pits)
        {
            if (x >= pit.x && x <= pit.y) return true;
        }
        return false;
    }

    // Pits plus a small margin, so decorations never float over a hole.
    static bool IsNearPit(LevelSpec spec, float x)
    {
        foreach (Vector2Int pit in spec.pits)
        {
            if (x >= pit.x - 1.5f && x <= pit.y + 1.5f) return true;
        }
        return false;
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

    static EnemyBullet CreateEnemyBulletPrefab(Sprite sprite)
    {
        GameObject go = new GameObject("EnemyBullet");
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 11;
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.3f, 0.15f);
        collider.isTrigger = true;
        go.AddComponent<EnemyBullet>();

        string path = PrefabDir + "/EnemyBullet.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        return prefab.GetComponent<EnemyBullet>();
    }

    // Prefab with a short sprite-debris burst, spawned when an enemy dies.
    static SpriteBurst CreateBurstPrefab()
    {
        GameObject go = new GameObject("EnemyDeathBurst");
        SpriteBurst burst = go.AddComponent<SpriteBurst>();
        burst.sprite = PixelArt.SolidSprite("spark", Color.white);
        burst.count = 9;
        burst.speedMin = 2.5f;
        burst.speedMax = 5.5f;
        burst.lifetime = 0.5f;
        burst.palette = new[] { Color.white, new Color(1f, 0.85f, 0.55f), new Color(0.85f, 0.85f, 0.85f) };

        string path = PrefabDir + "/EnemyDeathBurst.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        return prefab.GetComponent<SpriteBurst>();
    }

    // Read-only diagnostic: open both stages and report tilemap, sprite and goal wiring.
    public static void VerifyBatch()
    {
        try
        {
            VerifyLevel(Stage1Path);
            VerifyLevel(Stage2Path);
            VerifyLevel(Stage3Path);
            Debug.Log("[Verify] done");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[Verify] failed: " + e);
            EditorApplication.Exit(1);
        }
    }

    static void VerifyLevel(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path);

        int spriteCount = 0;
        int missingSprites = 0;
        foreach (SpriteRenderer sr in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())
        {
            spriteCount++;
            if (sr.sprite == null) missingSprites++;
        }

        Tilemap tilemap = UnityEngine.Object.FindObjectOfType<Tilemap>();
        int tileCount = 0;
        int missingTiles = 0;
        string firstTile = "none";
        if (tilemap != null)
        {
            foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
            {
                TileBase tile = tilemap.GetTile(pos);
                if (tile == null) continue;
                tileCount++;
                Tile t = tile as Tile;
                if (t == null || t.sprite == null)
                {
                    missingTiles++;
                }
                else if (firstTile == "none")
                {
                    firstTile = t.sprite.name;
                }
            }
        }

        LevelGoal goal = UnityEngine.Object.FindObjectOfType<LevelGoal>();
        Snowfall snow = UnityEngine.Object.FindObjectOfType<Snowfall>();

        Debug.Log("[Verify] " + path
            + " sprites=" + spriteCount
            + " missingSprites=" + missingSprites
            + " tiles=" + tileCount
            + " missingTiles=" + missingTiles
            + " firstTile=" + firstTile
            + " goal=" + (goal != null ? (goal.nextScene.Length > 0 ? goal.nextScene : "clear") : "none")
            + " snowfall=" + (snow != null ? (snow.flakeSprite != null ? snow.flakeSprite.name : "NULL") : "none"));
    }

    // Read-only diagnostic: open the first stage and report every sprite reference and the GUID the editor assigns to its PNG.
    public static void DiagnoseBatch()
    {
        try
        {
            EditorSceneManager.OpenScene(Stage1Path);
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
        WeaponData weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
        if (weapon == null)
        {
            weapon = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(weapon, path);
        }
        weapon.fireRate = 8f;
        weapon.bulletSpeed = 12f;
        weapon.bulletLifetime = 1.2f;
        weapon.damage = 1;
        EditorUtility.SetDirty(weapon);
        return weapon;
    }

    static Tile CreateTile(Sprite sprite, string name)
    {
        string path = TileDir + "/" + name + ".asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    static void PaintRow(Tilemap map, Tile tile, int fromX, int toX, int y)
    {
        for (int x = fromX; x <= toX; x++)
        {
            map.SetTile(new Vector3Int(x, y, 0), tile);
        }
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
