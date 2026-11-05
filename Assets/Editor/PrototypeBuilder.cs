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

        Sprite[] playerFrames = PixelArt.PlayerFrames();
        Sprite[] enemyFrames = PixelArt.EnemyFrames();
        Sprite bulletSprite = PixelArt.BulletSprite();

        Sprite[] groundTopSprites = PixelArt.GroundTops();
        Sprite[] dirtSprites = PixelArt.DirtFills();
        Sprite platformSprite = PixelArt.PlatformSprite();

        Tile[] groundTiles = new Tile[groundTopSprites.Length];
        for (int i = 0; i < groundTiles.Length; i++) groundTiles[i] = CreateTile(groundTopSprites[i], "GroundTop" + i);
        Tile[] dirtTiles = new Tile[dirtSprites.Length];
        for (int i = 0; i < dirtTiles.Length; i++) dirtTiles[i] = CreateTile(dirtSprites[i], "DirtFill" + i);
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
        // Sky, clouds, mountains and vignette are children/parallax layers created below.
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

        CreateBackground(cam);

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

        for (int x = -40; x < 96; x++)
        {
            bool pit = (x >= 22 && x <= 24) || (x >= 56 && x <= 59);
            if (pit) continue;
            int variant = (x * 92821 + 7) & 0x7fffffff;
            tilemap.SetTile(new Vector3Int(x, 0, 0), groundTiles[variant % groundTiles.Length]);
            // Fill well below the camera's bottom edge so no sky shows underneath the ground.
            for (int y = -1; y >= -5; y--)
            {
                int hash = variant + (y + 8) * 977;
                tilemap.SetTile(new Vector3Int(x, y, 0), dirtTiles[hash % dirtTiles.Length]);
            }
        }
        PaintRow(tilemap, platformTile, 8, 12, 3);
        PaintRow(tilemap, platformTile, 30, 34, 4);
        PaintRow(tilemap, platformTile, 42, 46, 2);

        CreateDecorations();
        CreateDepthShade();

        // Player: run, jump, gun with muzzle and ground check.
        GameObject playerGo = new GameObject("Player");
        playerGo.transform.position = new Vector3(0f, 2f, 0f);
        SpriteRenderer playerRenderer = playerGo.AddComponent<SpriteRenderer>();
        playerRenderer.sprite = playerFrames[0];
        playerRenderer.sortingOrder = 10;
        Rigidbody2D playerBody = playerGo.AddComponent<Rigidbody2D>();
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D playerCollider = playerGo.AddComponent<BoxCollider2D>();
        playerCollider.size = new Vector2(0.8f, 1.4f);
        PlayerController controller = playerGo.AddComponent<PlayerController>();
        controller.jumpSpeed = 14f;
        CharacterAnimator playerAnimator = playerGo.AddComponent<CharacterAnimator>();
        playerAnimator.frames = playerFrames;
        playerAnimator.fps = 8f;

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(playerGo.transform, false);
        groundCheck.transform.localPosition = new Vector3(0f, -0.75f, 0f);
        controller.groundCheck = groundCheck.transform;

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(playerGo.transform, false);
        // Matches the gun tip in the 16x24 sprite (see PixelArt.PlayerUpper).
        muzzle.transform.localPosition = new Vector3(0.42f, -0.08f, 0f);

        Gun gun = playerGo.AddComponent<Gun>();
        gun.weapon = weapon;
        gun.bulletPrefab = bulletPrefab;
        gun.muzzle = muzzle.transform;
        controller.gun = gun;
        controller.muzzle = muzzle.transform;

        // Ground top is y=1.0; enemy sprite is 22 px (1.375 units) tall, so its centre sits 0.6875 above the floor.
        // Platform top is y=4.0 for the first platform.
        CreateEnemy(enemyFrames, new Vector3(14f, 1.69f, 0f), 2f);
        CreateEnemy(enemyFrames, new Vector3(28f, 1.69f, 0f), 3f);
        CreateEnemy(enemyFrames, new Vector3(40f, 1.69f, 0f), 2f);
        CreateEnemy(enemyFrames, new Vector3(62f, 1.69f, 0f), 3f);
        CreateEnemy(enemyFrames, new Vector3(76f, 1.69f, 0f), 2f);
        CreateEnemy(enemyFrames, new Vector3(10f, 4.69f, 0f), 1.5f);

        CameraFollow2D follow = cam.gameObject.AddComponent<CameraFollow2D>();
        follow.target = playerGo.transform;
        follow.minX = -20f;
        follow.maxX = 80f;
        follow.maxY = 5f;

        // Invisible walls keep the player inside the painted level.
        CreateInvisibleWall(-19f, 8f);
        CreateInvisibleWall(88f, 8f);

        // Touch UI: Screen Space Overlay canvas, safe-area panel, joystick, two buttons, FPS label.
        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Sprite uiCircle = PixelArt.UiCircle("ui_circle", 256);
        Sprite uiRing = PixelArt.UiRing("ui_ring", 256, 7f);
        Sprite uiKnob = PixelArt.UiCircle("ui_knob", 128);
        Sprite uiButton = PixelArt.UiRoundedRect("ui_button", 256, 60f);
        Sprite uiArrow = PixelArt.UiArrow();
        Sprite uiBulletIcon = PixelArt.UiBulletIcon();

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
        fpsCounter.showDebugInfo = false;

        GameObject stickGo = new GameObject("Stick", typeof(RectTransform), typeof(Image), typeof(OnScreenStick), typeof(DpSize));
        stickGo.transform.SetParent(safeGo.transform, false);
        RectTransform stickRect = (RectTransform)stickGo.transform;
        stickRect.anchorMin = new Vector2(0f, 0f);
        stickRect.anchorMax = new Vector2(0f, 0f);
        stickRect.pivot = new Vector2(0.5f, 0.5f);
        stickRect.anchoredPosition = Vector2.zero;
        Image stickImage = stickGo.GetComponent<Image>();
        stickImage.sprite = uiCircle;
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
        ringImage.sprite = uiRing;
        ringImage.color = new Color(1f, 1f, 1f, 0.34f);
        ringImage.raycastTarget = false;
        DpSize ringSize = ringGo.GetComponent<DpSize>();
        ringSize.canvas = canvas;
        ringSize.widthDp = 150f;
        ringSize.heightDp = 150f;

        Image knobImage = knobGo.GetComponent<Image>();
        knobImage.sprite = uiKnob;
        knobImage.color = new Color(1f, 1f, 1f, 0.72f);
        knobImage.raycastTarget = false;
        DpSize knobSize = knobGo.GetComponent<DpSize>();
        knobSize.canvas = canvas;
        knobSize.widthDp = 60f;
        knobSize.heightDp = 60f;
        stickGo.GetComponent<OnScreenStick>().knob = knobRect;

        // Action buttons, bottom-right, 92 dp each with 10 dp gap (offsets are measured from the bottom-right anchor).
        CreateButton(safeGo.transform, canvas, font, uiCircle, uiArrow, "JumpButton", OnScreenButtonType.Jump, "JUMP",
            new Color(0.16f, 0.42f, 0.24f, 0.72f), new Color(0.32f, 0.62f, 0.36f, 0.42f),
            new Vector2(-68f, 68f), 92f, 36f, 30f, 12f);
        CreateButton(safeGo.transform, canvas, font, uiCircle, uiBulletIcon, "FireButton", OnScreenButtonType.Fire, "FIRE",
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
        autoImage.sprite = uiButton;
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

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        new GameObject("GameSetup", typeof(FrameRateSetup), typeof(PhysicsSetup));

        PixelArt.WritePreview();

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

    static void CreateEnemy(Sprite[] frames, Vector3 position, float patrol)
    {
        GameObject go = new GameObject("Enemy");
        go.transform.position = position;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[0];
        renderer.sortingOrder = 9;
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        // Covers the torso; the muzzle sits around y=1.6 when the player stands on the ground.
        collider.size = new Vector2(0.9f, 1.1f);
        collider.offset = new Vector2(0f, 0.05f);
        Enemy enemy = go.AddComponent<Enemy>();
        enemy.patrolDistance = patrol;
        CharacterAnimator animator = go.AddComponent<CharacterAnimator>();
        animator.frames = frames;
        animator.fps = 6f;
    }

    // Static collider with no renderer: blocks the player at the ends of the level.
    static void CreateInvisibleWall(float x, float centerY)
    {
        GameObject go = new GameObject("Wall");
        go.transform.position = new Vector3(x, centerY, 0f);
        BoxCollider2D wall = go.AddComponent<BoxCollider2D>();
        wall.size = new Vector2(1f, 24f);
    }

    // Sky, clouds, mountain range, jungle treeline and a soft vignette.
    static void CreateBackground(Camera cam)
    {
        Sprite skySprite = PixelArt.SkySprite();
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

        Sprite cloudSprite = PixelArt.CloudSprite(5001);
        GameObject cloudsGo = new GameObject("Clouds");
        ParallaxLayer cloudParallax = cloudsGo.AddComponent<ParallaxLayer>();
        cloudParallax.cameraTransform = cam.transform;
        cloudParallax.factor = 0.92f;
        for (int i = 0; i < 10; i++)
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

        Sprite mountainSprite = PixelArt.MountainSprite();
        GameObject mountainsGo = new GameObject("Mountains");
        mountainsGo.transform.position = new Vector3(40f, 3f, 0f);
        SpriteRenderer mountainRenderer = mountainsGo.AddComponent<SpriteRenderer>();
        mountainRenderer.sprite = mountainSprite;
        mountainRenderer.sortingOrder = -960;
        ParallaxLayer mountainParallax = mountainsGo.AddComponent<ParallaxLayer>();
        mountainParallax.cameraTransform = cam.transform;
        mountainParallax.factor = 0.85f;

        Sprite treelineSprite = PixelArt.TreelineSprite();
        GameObject treelineGo = new GameObject("Treeline");
        treelineGo.transform.position = new Vector3(40f, 3.3f, 0f);
        SpriteRenderer treelineRenderer = treelineGo.AddComponent<SpriteRenderer>();
        treelineRenderer.sprite = treelineSprite;
        treelineRenderer.sortingOrder = -940;
        ParallaxLayer treelineParallax = treelineGo.AddComponent<ParallaxLayer>();
        treelineParallax.cameraTransform = cam.transform;
        treelineParallax.factor = 0.6f;

        Sprite vignetteSprite = PixelArt.VignetteSprite();
        GameObject vignetteGo = new GameObject("Vignette");
        vignetteGo.transform.SetParent(cam.transform, false);
        vignetteGo.transform.localPosition = new Vector3(0f, 0f, 4f);
        SpriteRenderer vignetteRenderer = vignetteGo.AddComponent<SpriteRenderer>();
        vignetteRenderer.sprite = vignetteSprite;
        vignetteRenderer.sortingOrder = 1000;
        vignetteGo.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
    }

    // Darkens the dirt towards the bottom of the screen; one gradient quad avoids per-tile banding.
    static void CreateDepthShade()
    {
        Sprite shade = PixelArt.DepthShadeSprite();
        GameObject go = new GameObject("DepthShade");
        go.transform.position = new Vector3(28f, -1.5f, 0f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = shade;
        renderer.sortingOrder = 1;
        go.transform.localScale = new Vector3(
            136f / (shade.rect.width / 16f),
            5f / (shade.rect.height / 16f),
            1f);
    }

    // Grass tufts, bushes and rocks along the ground line. Purely decorative, no colliders.
    static void CreateDecorations()
    {
        Sprite[] tufts = PixelArt.TuftSprites();
        Sprite[] bushes = PixelArt.BushSprites();
        Sprite rock = PixelArt.RockSprite();

        GameObject root = new GameObject("Decorations");
        System.Random rng = new System.Random(2024);

        float x = -38f;
        while (x < 94f)
        {
            bool pit = (x >= 21.5f && x <= 25.5f) || (x >= 55.5f && x <= 60.5f);
            if (!pit)
            {
                int roll = rng.Next(100);
                if (roll < 55)
                {
                    Sprite sprite = tufts[rng.Next(tufts.Length)];
                    float scale = 0.9f + (float)rng.NextDouble() * 0.6f;
                    PlaceDecoration(root, sprite, new Vector3(x, 1f + sprite.bounds.extents.y * scale, 0f), 2, scale);
                }
                else if (roll < 78)
                {
                    Sprite sprite = bushes[rng.Next(bushes.Length)];
                    float scale = 0.9f + (float)rng.NextDouble() * 0.5f;
                    PlaceDecoration(root, sprite, new Vector3(x, 1f + sprite.bounds.extents.y * scale, 0f), 3, scale);
                }
                else if (roll < 92)
                {
                    float scale = 0.8f + (float)rng.NextDouble() * 0.7f;
                    PlaceDecoration(root, rock, new Vector3(x, 1f + rock.bounds.extents.y * scale, 0f), 2, scale);
                }
            }
            x += 1.4f + (float)rng.NextDouble() * 2.2f;
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
