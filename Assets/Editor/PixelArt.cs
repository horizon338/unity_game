using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Procedurally authored pixel art: characters, terrain, sky, decorations and UI.
// Everything is generated into Assets/Art/Generated as PNGs so the sprites stay editable later.
public static class PixelArt
{
    public const string Dir = "Assets/Art/Generated";
    const int Ppu = 16;

    // PNG bytes are kept so WritePreview can compose a contact sheet after the scene is built.
    static readonly List<KeyValuePair<string, byte[]>> Generated = new List<KeyValuePair<string, byte[]>>();

    // ------------------------------------------------------------------ characters

    static readonly string[] PlayerUpper =
    {
        "................",
        ".....kkkkkk.....",
        "....krrrrrrk....",
        "...krrrrrrrrk...",
        "...krRrrrrRrk...",
        "...kssssskssk...",
        "...kssssssssk...",
        "....kSssssSk....",
        ".....kssssk.....",
        "...kkbbbbbbkk...",
        "..kbbbbbbbbbbk..",
        "..kbbbbbbbbbbk..",
        "..kbbbbbbssgGGgG",
        "..kbbbbbbssgGGgg",
        "..kbyyyyyyyybk..",
        "...kttttttttk..."
    };

    static readonly string[] PlayerLegsIdle =
    {
        "...kttttkttttk..",
        "....kttk.kttk...",
        "....kttk.kttk...",
        "....kTTk.kTTk...",
        "....koook.koook.",
        "....koook.koook.",
        "....kkkkk.kkkkk.",
        "................"
    };

    static readonly string[] PlayerLegsRunA =
    {
        "...kttttttttk...",
        "...kttk..kttk...",
        "...kttk..kttk...",
        "...kTTk..kTTk...",
        "...koook.koook..",
        "...koook.koook..",
        "..kOOOOk.kOOOk..",
        "................"
    };

    static readonly string[] PlayerLegsRunB =
    {
        "...kttttttttk...",
        "....kttttttk....",
        "....kttk.kttk...",
        ".....kttk.kttk..",
        "....koook.koook.",
        "....koook.koook.",
        "....kkkkk.kkkkk.",
        "................"
    };

    // Airborne pose: legs tucked, feet splayed forward and back.
    static readonly string[] PlayerLegsJump =
    {
        "...kttttttttk...",
        "...kttk..kttk...",
        "...ktTk...ktTk..",
        "..kook....koook.",
        "..kOOOk...kOOOk.",
        "................",
        "................",
        "................"
    };

    static readonly string[] EnemyUpper =
    {
        "................",
        "....kkkkkkkk....",
        "...kHHHHHHHHk...",
        "...kHHIIIIHHk...",
        "...kssssssssk...",
        "...kssssskssk...",
        "....kssssssk....",
        ".....kssssk.....",
        "...kkuuuuuukk...",
        "..kuuuuuuuuuuk..",
        "..kuuuuuuuuuuk..",
        "..kUuuuuuuuuUk..",
        "..kuuuuuussgGGgG",
        "..kUuuuuussgGGgg",
        "..kuuuuuuuuuuk..",
        "...kuuuuuuuuk..."
    };

    static readonly string[] EnemyLegsA =
    {
        "...kuuuk.kuuuk..",
        "...kuuuk.kuuuk..",
        "...kUUUk.kUUUk..",
        "...koook.koook..",
        "..kOOOOk.kOOOk..",
        "................"
    };

    static readonly string[] EnemyLegsB =
    {
        "....kuuuk.kuuuk.",
        "....kuuuk.kuuuk.",
        "....kUUUk.kUUUk.",
        "....koook.koook.",
        "...kOOOOk.kOOOk.",
        "................"
    };

    static Dictionary<char, Color> PlayerPalette()
    {
        return new Dictionary<char, Color>
        {
            { 'k', Hex("#16121C") },
            { 'r', Hex("#D23A2C") },
            { 'R', Hex("#8C1F18") },
            { 's', Hex("#F2C48E") },
            { 'S', Hex("#C98D5B") },
            { 'b', Hex("#2F6BD8") },
            { 'B', Hex("#1A4696") },
            { 'g', Hex("#4C5464") },
            { 'G', Hex("#9AA5B8") },
            { 'y', Hex("#E5B544") },
            { 't', Hex("#2A3654") },
            { 'T', Hex("#1A2338") },
            { 'o', Hex("#6A4426") },
            { 'O', Hex("#3E2614") }
        };
    }

    static Dictionary<char, Color> EnemyPalette()
    {
        return new Dictionary<char, Color>
        {
            { 'k', Hex("#141A12") },
            { 'H', Hex("#5F6B3C") },
            { 'I', Hex("#3C4626") },
            { 's', Hex("#E3AE78") },
            { 'S', Hex("#B57B4A") },
            { 'u', Hex("#7C6A3E") },
            { 'U', Hex("#544626") },
            { 'g', Hex("#3E4550") },
            { 'G', Hex("#8E97A8") },
            { 'o', Hex("#3A2A18") },
            { 'O', Hex("#241A0E") }
        };
    }

    // Cold-weather uniform for the snow stage: the same soldier silhouette in blue-grey.
    static Dictionary<char, Color> SnowEnemyPalette()
    {
        return new Dictionary<char, Color>
        {
            { 'k', Hex("#10141C") },
            { 'H', Hex("#5A7A94") },
            { 'I', Hex("#33495C") },
            { 's', Hex("#E3AE78") },
            { 'S', Hex("#B57B4A") },
            { 'u', Hex("#5B7A92") },
            { 'U', Hex("#3A4F63") },
            { 'g', Hex("#3E4550") },
            { 'G', Hex("#8E97A8") },
            { 'o', Hex("#2A2218") },
            { 'O', Hex("#18130C") }
        };
    }

    // Desert-camo uniform for the sand stage.
    static Dictionary<char, Color> DesertEnemyPalette()
    {
        return new Dictionary<char, Color>
        {
            { 'k', Hex("#171208") },
            { 'H', Hex("#A88F5E") },
            { 'I', Hex("#77643C") },
            { 's', Hex("#E3AE78") },
            { 'S', Hex("#B57B4A") },
            { 'u', Hex("#9A8354") },
            { 'U', Hex("#6F5C38") },
            { 'g', Hex("#3E4550") },
            { 'G', Hex("#8E97A8") },
            { 'o', Hex("#2A2114") },
            { 'O', Hex("#181109") }
        };
    }

    // Cave-guard uniform: dark leather and stone tones.
    static Dictionary<char, Color> CaveEnemyPalette()
    {
        return new Dictionary<char, Color>
        {
            { 'k', Hex("#0C0A08") },
            { 'H', Hex("#6E5A45") },
            { 'I', Hex("#4A3A2A") },
            { 's', Hex("#DFA97A") },
            { 'S', Hex("#B07A4E") },
            { 'u', Hex("#5E5145") },
            { 'U', Hex("#3D342B") },
            { 'g', Hex("#33383F") },
            { 'G', Hex("#7E8794") },
            { 'o', Hex("#221A12") },
            { 'O', Hex("#140F09") }
        };
    }

    // Factory-guard uniform: steel grey with dark accents.
    static Dictionary<char, Color> FactoryEnemyPalette()
    {
        return new Dictionary<char, Color>
        {
            { 'k', Hex("#0C0E12") },
            { 'H', Hex("#6A7078") },
            { 'I', Hex("#464C55") },
            { 's', Hex("#E3AE78") },
            { 'S', Hex("#B57B4A") },
            { 'u', Hex("#5C636C") },
            { 'U', Hex("#3E444C") },
            { 'g', Hex("#2E333A") },
            { 'G', Hex("#9AA3AE") },
            { 'o', Hex("#221A12") },
            { 'O', Hex("#140F09") }
        };
    }

    public static Sprite[] PlayerFrames()
    {
        Dictionary<char, Color> palette = PlayerPalette();
        return new[]
        {
            FromMap("player_idle", Combine(PlayerUpper, PlayerLegsIdle), palette),
            FromMap("player_run1", Combine(PlayerUpper, PlayerLegsRunA), palette),
            FromMap("player_run2", Combine(PlayerUpper, PlayerLegsRunB), palette),
            FromMap("player_jump", Combine(PlayerUpper, PlayerLegsJump), palette)
        };
    }

    public static Sprite[] EnemyFrames()
    {
        Dictionary<char, Color> palette = EnemyPalette();
        return new[]
        {
            FromMap("enemy_a", Combine(EnemyUpper, EnemyLegsA), palette),
            FromMap("enemy_b", Combine(EnemyUpper, EnemyLegsB), palette)
        };
    }

    public static Sprite[] SnowEnemyFrames()
    {
        Dictionary<char, Color> palette = SnowEnemyPalette();
        return new[]
        {
            FromMap("snow_enemy_a", Combine(EnemyUpper, EnemyLegsA), palette),
            FromMap("snow_enemy_b", Combine(EnemyUpper, EnemyLegsB), palette)
        };
    }

    public static Sprite[] DesertEnemyFrames()
    {
        Dictionary<char, Color> palette = DesertEnemyPalette();
        return new[]
        {
            FromMap("desert_enemy_a", Combine(EnemyUpper, EnemyLegsA), palette),
            FromMap("desert_enemy_b", Combine(EnemyUpper, EnemyLegsB), palette)
        };
    }

    public static Sprite[] CaveEnemyFrames()
    {
        Dictionary<char, Color> palette = CaveEnemyPalette();
        return new[]
        {
            FromMap("cave_enemy_a", Combine(EnemyUpper, EnemyLegsA), palette),
            FromMap("cave_enemy_b", Combine(EnemyUpper, EnemyLegsB), palette)
        };
    }

    public static Sprite[] FactoryEnemyFrames()
    {
        Dictionary<char, Color> palette = FactoryEnemyPalette();
        return new[]
        {
            FromMap("factory_enemy_a", Combine(EnemyUpper, EnemyLegsA), palette),
            FromMap("factory_enemy_b", Combine(EnemyUpper, EnemyLegsB), palette)
        };
    }

    public static Sprite BulletSprite()
    {
        string[] rows =
        {
            "..oooo..",
            ".oyyyyo.",
            ".oyWWyo.",
            "..oooo.."
        };
        Dictionary<char, Color> palette = new Dictionary<char, Color>
        {
            { 'o', Hex("#E8760E") },
            { 'y', Hex("#FFC21A") },
            { 'W', Hex("#FFF8D0") }
        };
        return FromMap("bullet", rows, palette);
    }

    // Enemy projectile: same silhouette as the player bullet, red palette so factions read at a glance.
    public static Sprite EnemyBulletSprite()
    {
        string[] rows =
        {
            "..oooo..",
            ".orrrro.",
            ".orWWro.",
            "..oooo.."
        };
        Dictionary<char, Color> palette = new Dictionary<char, Color>
        {
            { 'o', Hex("#7A1B10") },
            { 'r', Hex("#E8452C") },
            { 'W', Hex("#FFD9B0") }
        };
        return FromMap("enemy_bullet", rows, palette);
    }

    // Star-shaped flash shown at the muzzle for a few frames after each shot.
    public static Sprite MuzzleFlashSprite()
    {
        string[] rows =
        {
            ".....yy.....",
            ".....yy.....",
            "...W.yy.W...",
            "....WyyW....",
            "..WWyyyyWW..",
            "..WyyyyyyW..",
            "..WWyyyyWW..",
            "....WyyW....",
            "...W.yy.W...",
            ".....yy.....",
            ".....yy.....",
            "............"
        };
        Dictionary<char, Color> palette = new Dictionary<char, Color>
        {
            { 'y', new Color(1f, 0.78f, 0.12f, 0.95f) },
            { 'W', new Color(1f, 0.98f, 0.82f, 1f) }
        };
        return Import("muzzle_flash", FromMapRows(rows, palette), false, Ppu);
    }

    // ------------------------------------------------------------------ terrain

    public static Sprite[] GroundTops()
    {
        return new[]
        {
            FromTexture("ground_top0", BuildGroundTop(1101)),
            FromTexture("ground_top1", BuildGroundTop(1102)),
            FromTexture("ground_top2", BuildGroundTop(1103))
        };
    }

    public static Sprite[] DirtFills()
    {
        return new[]
        {
            FromTexture("dirt_fill0", BuildDirtFill(2101)),
            FromTexture("dirt_fill1", BuildDirtFill(2102))
        };
    }

    public static Sprite PlatformSprite()
    {
        return FromTexture("platform", BuildPlatform(3101));
    }

    // ------------------------------------------------------------------ snow terrain

    public static Sprite[] SnowGroundTops()
    {
        return new[]
        {
            FromTexture("snow_ground_top0", BuildSnowGroundTop(1201)),
            FromTexture("snow_ground_top1", BuildSnowGroundTop(1202)),
            FromTexture("snow_ground_top2", BuildSnowGroundTop(1203))
        };
    }

    public static Sprite[] SnowFills()
    {
        return new[]
        {
            FromTexture("snow_fill0", BuildSnowFill(2201)),
            FromTexture("snow_fill1", BuildSnowFill(2202))
        };
    }

    public static Sprite SnowPlatformSprite()
    {
        return FromTexture("ice_platform", BuildIcePlatform(3201));
    }

    static Texture2D BuildSnowGroundTop(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color earthBase = Hex("#4A423C");
        Color earthDark = Hex("#332D29");
        Color earthLight = Hex("#5C534B");
        Color stone = Hex("#6E6A66");
        Color stoneDark = Hex("#46423E");
        Color snowTop = Hex("#F6FAFF");
        Color snowMid = Hex("#E1ECF8");
        Color snowShade = Hex("#BFD3E6");

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float v = (float)rng.NextDouble();
                Color c = Color.Lerp(earthDark, earthBase, Mathf.Clamp01(y / 12f));
                if (v < 0.10f) c = Color.Lerp(c, earthDark, 0.5f);
                else if (v > 0.94f) c = Color.Lerp(c, earthLight, 0.5f);
                t.SetPixel(x, y, c);
            }
        }

        for (int i = 0; i < 3; i++)
        {
            int px = rng.Next(1, 14);
            int py = rng.Next(1, 8);
            t.SetPixel(px, py, stone);
            t.SetPixel(px + 1, py, stone);
            t.SetPixel(px, py + 1, Color.Lerp(stone, earthLight, 0.4f));
            t.SetPixel(px + 1, py + 1, stoneDark);
            t.SetPixel(px, py - 1, stoneDark);
        }

        int[] top = new int[16];
        for (int x = 0; x < 16; x++) top[x] = 10 + rng.Next(0, 5);

        for (int x = 0; x < 16; x++)
        {
            // Snow blanket: white crown, blue mid tone, shaded underside.
            t.SetPixel(x, top[x], snowTop);
            t.SetPixel(x, top[x] - 1, snowMid);
            t.SetPixel(x, top[x] - 2, rng.NextDouble() < 0.55f ? snowShade : snowMid);
            t.SetPixel(x, top[x] - 3, Color.Lerp(earthBase, snowShade, 0.30f));

            // Wind-blown overhang hanging one or two pixels below the blanket.
            if (rng.NextDouble() < 0.35)
            {
                int depth = rng.Next(1, 3);
                for (int i = 0; i < depth; i++) t.SetPixel(x, top[x] - 3 - i, snowShade);
            }
            if (rng.NextDouble() < 0.25) t.SetPixel(x, top[x] - 3, snowMid);
        }

        if (rng.NextDouble() < 0.45)
        {
            int fx = rng.Next(0, 16);
            t.SetPixel(fx, top[fx], Color.white);
        }
        if (rng.NextDouble() < 0.5)
        {
            int ix = rng.Next(0, 16);
            int iy = rng.Next(1, 8);
            t.SetPixel(ix, iy, Color.Lerp(stone, Hex("#7E9DB5"), 0.5f));
        }

        return t;
    }

    static Texture2D BuildSnowFill(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color baseCol = Hex("#3B3A42");
        Color dark = Hex("#2A2930");
        Color light = Hex("#4E4D57");
        Color stone = Hex("#5E5C66");
        Color ice = Hex("#7C9CB4");

        // Flat per-pixel noise only: stacked rows must not show block seams.
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float v = (float)rng.NextDouble();
                Color c = baseCol;
                if (v < 0.07f) c = Color.Lerp(c, dark, 0.45f);
                else if (v > 0.95f) c = Color.Lerp(c, light, 0.40f);
                t.SetPixel(x, y, c);
            }
        }

        for (int band = 0; band < 3; band++)
        {
            int by = rng.Next(2, 14);
            for (int x = 0; x < 16; x++)
            {
                if (rng.NextDouble() < 0.6)
                    t.SetPixel(x, by, Color.Lerp(t.GetPixel(x, by), ice, 0.25f));
            }
        }

        for (int i = 0; i < 3; i++)
        {
            int px = rng.Next(1, 15);
            int py = rng.Next(1, 15);
            t.SetPixel(px, py, stone);
            t.SetPixel(px, py + 1, Color.Lerp(stone, dark, 0.4f));
            t.SetPixel(px + 1, py, Color.Lerp(stone, dark, 0.3f));
        }

        return t;
    }

    static Texture2D BuildIcePlatform(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color topSnow = Hex("#F4F9FF");
        Color topIce = Hex("#D2E4F4");
        Color upper = Hex("#9FC2DC");
        Color body = Hex("#7195B2");
        Color bodyDark = Hex("#4C6B88");
        Color bottom = Hex("#34506B");

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                Color c;
                if (y == 15) c = topSnow;
                else if (y == 14) c = topIce;
                else if (y == 13) c = Color.Lerp(topIce, upper, 0.6f);
                else if (y <= 2) c = Color.Lerp(bottom, bodyDark, y / 2f);
                else c = Color.Lerp(bodyDark, body, Mathf.Clamp01((y - 2) / 11f));
                t.SetPixel(x, y, c);
            }
        }

        for (int y = 3; y <= 13; y++)
        {
            t.SetPixel(0, y, bodyDark);
            t.SetPixel(15, y, bodyDark);
        }

        // Vertical cracks in the ice.
        for (int i = 0; i < 3; i++)
        {
            int cx = rng.Next(2, 14);
            int start = rng.Next(3, 9);
            int len = rng.Next(3, 8);
            for (int y = start; y < start + len && y < 14; y++)
            {
                t.SetPixel(cx, y, Color.Lerp(bodyDark, Color.white, 0.10f));
            }
        }

        // Hanging icicles at the underside.
        for (int i = 0; i < 4; i++)
        {
            int ix = rng.Next(1, 15);
            int len = rng.Next(1, 3);
            for (int y = 0; y < len; y++) t.SetPixel(ix, y, Hex("#BFD9EC"));
        }

        // A dusting of snow on the top edge.
        for (int x = 0; x < 16; x++)
        {
            if (rng.NextDouble() < 0.4) t.SetPixel(x, 15, Color.white);
            if (rng.NextDouble() < 0.25) t.SetPixel(x, 14, Hex("#EAF4FC"));
        }

        return t;
    }

    // ------------------------------------------------------------------ desert terrain

    public static Sprite[] SandGroundTops()
    {
        return new[]
        {
            FromTexture("sand_ground_top0", BuildSandGroundTop(1401)),
            FromTexture("sand_ground_top1", BuildSandGroundTop(1402)),
            FromTexture("sand_ground_top2", BuildSandGroundTop(1403))
        };
    }

    public static Sprite[] SandFills()
    {
        return new[]
        {
            FromTexture("sand_fill0", BuildSandFill(2401)),
            FromTexture("sand_fill1", BuildSandFill(2402))
        };
    }

    public static Sprite SandstonePlatformSprite()
    {
        return FromTexture("sandstone_platform", BuildSandstonePlatform(3401));
    }

    static Texture2D BuildSandGroundTop(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color rockBase = Hex("#8A6845");
        Color rockDark = Hex("#5E452C");
        Color rockLight = Hex("#A5825A");
        Color stone = Hex("#B49A72");
        Color stoneDark = Hex("#6E583C");
        Color sandTop = Hex("#F0D98A");
        Color sandMid = Hex("#E3C36C");
        Color sandShade = Hex("#C6A24E");

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float v = (float)rng.NextDouble();
                Color c = Color.Lerp(rockDark, rockBase, Mathf.Clamp01(y / 12f));
                if (v < 0.10f) c = Color.Lerp(c, rockDark, 0.5f);
                else if (v > 0.94f) c = Color.Lerp(c, rockLight, 0.5f);
                t.SetPixel(x, y, c);
            }
        }

        for (int i = 0; i < 3; i++)
        {
            int px = rng.Next(1, 14);
            int py = rng.Next(1, 8);
            t.SetPixel(px, py, stone);
            t.SetPixel(px + 1, py, stone);
            t.SetPixel(px, py + 1, Color.Lerp(stone, rockLight, 0.4f));
            t.SetPixel(px + 1, py + 1, stoneDark);
            t.SetPixel(px, py - 1, stoneDark);
        }

        int[] top = new int[16];
        for (int x = 0; x < 16; x++) top[x] = 10 + rng.Next(0, 5);

        for (int x = 0; x < 16; x++)
        {
            t.SetPixel(x, top[x], sandTop);
            t.SetPixel(x, top[x] - 1, sandMid);
            t.SetPixel(x, top[x] - 2, rng.NextDouble() < 0.5f ? sandShade : sandMid);
            t.SetPixel(x, top[x] - 3, Color.Lerp(rockBase, sandShade, 0.35f));

            // Wind ripples and loose grains.
            if (rng.NextDouble() < 0.30) t.SetPixel(x, top[x], sandShade);
            if (rng.NextDouble() < 0.22) t.SetPixel(x, top[x] - 1, sandShade);
            if (rng.NextDouble() < 0.25) t.SetPixel(x, top[x] - 3, sandMid);
        }

        if (rng.NextDouble() < 0.5)
        {
            int fx = rng.Next(0, 16);
            t.SetPixel(fx, top[fx], Hex("#FBF1C4"));
        }

        return t;
    }

    static Texture2D BuildSandFill(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color baseCol = Hex("#7E5C3B");
        Color dark = Hex("#543B24");
        Color light = Hex("#97744C");
        Color band = Hex("#A98A5F");
        Color stone = Hex("#9C8560");

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float v = (float)rng.NextDouble();
                Color c = baseCol;
                if (v < 0.07f) c = Color.Lerp(c, dark, 0.45f);
                else if (v > 0.95f) c = Color.Lerp(c, light, 0.40f);
                t.SetPixel(x, y, c);
            }
        }

        // Horizontal sandstone strata.
        for (int bandIndex = 0; bandIndex < 3; bandIndex++)
        {
            int by = rng.Next(2, 14);
            for (int x = 0; x < 16; x++)
            {
                if (rng.NextDouble() < 0.55)
                    t.SetPixel(x, by, Color.Lerp(t.GetPixel(x, by), band, 0.35f));
            }
        }

        for (int i = 0; i < 3; i++)
        {
            int px = rng.Next(1, 15);
            int py = rng.Next(1, 15);
            t.SetPixel(px, py, stone);
            t.SetPixel(px, py + 1, Color.Lerp(stone, dark, 0.4f));
            t.SetPixel(px + 1, py, Color.Lerp(stone, dark, 0.3f));
        }

        return t;
    }

    static Texture2D BuildSandstonePlatform(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color capSand = Hex("#F2DC92");
        Color capMid = Hex("#DFC070");
        Color upper = Hex("#C2A054");
        Color body = Hex("#9A7448");
        Color bodyDark = Hex("#6A4C2E");
        Color bottom = Hex("#4E3620");

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                Color c;
                if (y == 15) c = capSand;
                else if (y == 14) c = capMid;
                else if (y == 13) c = Color.Lerp(capMid, upper, 0.6f);
                else if (y <= 2) c = Color.Lerp(bottom, bodyDark, y / 2f);
                else c = Color.Lerp(bodyDark, body, Mathf.Clamp01((y - 2) / 11f));
                t.SetPixel(x, y, c);
            }
        }

        for (int y = 3; y <= 13; y++)
        {
            t.SetPixel(0, y, bodyDark);
            t.SetPixel(15, y, bodyDark);
        }

        // Vertical weathering cracks.
        for (int i = 0; i < 3; i++)
        {
            int cx = rng.Next(2, 14);
            int start = rng.Next(3, 8);
            int len = rng.Next(3, 7);
            for (int y = start; y < start + len && y < 14; y++)
                t.SetPixel(cx, y, Color.Lerp(bodyDark, Color.black, 0.15f));
        }

        // Sand spilled over the top edge.
        for (int x = 0; x < 16; x++)
        {
            if (rng.NextDouble() < 0.35) t.SetPixel(x, 15, Hex("#FBEFC6"));
            if (rng.NextDouble() < 0.20) t.SetPixel(x, 14, Hex("#E8CE84"));
        }

        return t;
    }

    static Texture2D BuildGroundTop(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color dirtBase = Hex("#6E4C2D");
        Color dirtDark = Hex("#4B3420");
        Color dirtLight = Hex("#84603A");
        Color grassBase = Hex("#4C8A33");
        Color grassLight = Hex("#79B84E");
        Color grassDark = Hex("#2C5A1E");
        Color stone = Hex("#8C7E68");
        Color stoneDark = Hex("#5E5444");

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float v = (float)rng.NextDouble();
                Color c = Color.Lerp(dirtDark, dirtBase, Mathf.Clamp01(y / 12f));
                if (v < 0.10f) c = Color.Lerp(c, dirtDark, 0.5f);
                else if (v > 0.94f) c = Color.Lerp(c, dirtLight, 0.5f);
                t.SetPixel(x, y, c);
            }
        }

        for (int i = 0; i < 3; i++)
        {
            int px = rng.Next(1, 14);
            int py = rng.Next(1, 8);
            t.SetPixel(px, py, stone);
            t.SetPixel(px + 1, py, stone);
            t.SetPixel(px, py + 1, Color.Lerp(stone, dirtLight, 0.4f));
            t.SetPixel(px + 1, py + 1, stoneDark);
            t.SetPixel(px, py - 1, stoneDark);
        }

        int[] top = new int[16];
        for (int x = 0; x < 16; x++) top[x] = 11 + rng.Next(0, 5);

        for (int x = 0; x < 16; x++)
        {
            for (int y = 10; y <= top[x]; y++)
            {
                Color c = grassBase;
                if (y == top[x]) c = grassLight;
                else if (y <= 11) c = grassDark;
                else
                {
                    float v = (float)rng.NextDouble();
                    if (v < 0.14f) c = grassDark;
                    else if (v > 0.88f) c = grassLight;
                }
                t.SetPixel(x, y, c);
            }

            if (rng.NextDouble() < 0.45)
            {
                int depth = rng.Next(1, 3);
                for (int i = 1; i <= depth; i++) t.SetPixel(x, 10 - i, grassDark);
            }
        }

        if (rng.NextDouble() < 0.3)
        {
            int fx = rng.Next(0, 16);
            int fy = Mathf.Clamp(rng.Next(12, 16), 0, top[fx]);
            t.SetPixel(fx, fy, Hex("#F0D24C"));
        }

        return t;
    }

    static Texture2D BuildDirtFill(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color baseCol = Hex("#6A492B");
        Color dark = Hex("#46301D");
        Color light = Hex("#7E5A36");
        Color stone = Hex("#847660");

        // Keep each tile flat instead of gradient-shaded, so stacked rows do not show horizontal seams.
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float v = (float)rng.NextDouble();
                Color c = baseCol;
                if (v < 0.07f) c = Color.Lerp(c, dark, 0.45f);
                else if (v > 0.95f) c = Color.Lerp(c, light, 0.40f);
                t.SetPixel(x, y, c);
            }
        }

        for (int i = 0; i < 3; i++)
        {
            int px = rng.Next(1, 15);
            int py = rng.Next(1, 15);
            t.SetPixel(px, py, Color.Lerp(baseCol, dark, 0.5f));
            t.SetPixel(px + 1, py, Color.Lerp(baseCol, dark, 0.5f));
            t.SetPixel(px, py + 1, Color.Lerp(baseCol, dark, 0.3f));
        }

        int strata = rng.Next(4, 12);
        for (int x = 0; x < 16; x++)
        {
            if (rng.NextDouble() < 0.35)
            {
                t.SetPixel(x, strata, Color.Lerp(t.GetPixel(x, strata), dark, 0.3f));
            }
        }

        for (int i = 0; i < 2; i++)
        {
            int px = rng.Next(1, 13);
            int py = rng.Next(1, 12);
            t.SetPixel(px, py, stone);
            t.SetPixel(px + 1, py, stone);
            t.SetPixel(px, py + 1, Color.Lerp(stone, dark, 0.5f));
            t.SetPixel(px + 1, py + 1, dark);
        }

        return t;
    }

    static Texture2D BuildPlatform(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color topCol = Hex("#C7CCD5");
        Color topShade = Hex("#9BA2AE");
        Color body = Hex("#767D89");
        Color bodyDark = Hex("#565D68");
        Color bottom = Hex("#3E434C");

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                Color c;
                if (y == 15) c = topCol;
                else if (y == 14) c = topShade;
                else if (y <= 2) c = Color.Lerp(bottom, bodyDark, y / 2f);
                else c = Color.Lerp(bodyDark, body, Mathf.Clamp01((y - 2) / 12f));
                t.SetPixel(x, y, c);
            }
        }

        int[] bolts = { 2, 7, 13 };
        for (int i = 0; i < bolts.Length; i++)
        {
            int bx = bolts[i];
            t.SetPixel(bx, 11, Hex("#2E323A"));
            t.SetPixel(bx, 12, Hex("#A9AFB9"));
        }

        for (int y = 3; y <= 13; y++)
        {
            t.SetPixel(0, y, bodyDark);
            t.SetPixel(15, y, bodyDark);
        }

        for (int i = 0; i < 5; i++)
        {
            int mx = rng.Next(0, 16);
            int my = rng.Next(13, 15);
            t.SetPixel(mx, my, Hex("#4E7A34"));
        }

        return t;
    }

    // ------------------------------------------------------------------ background

    public static Sprite SkySprite()
    {
        return SkyGradient("sky",
            Hex("#F2E3BE"), Hex("#DCE9EA"), Hex("#8FBFE4"), Hex("#2E63B2"));
    }

    public static Sprite SnowSkySprite()
    {
        return SkyGradient("snow_sky",
            Hex("#F3E9EC"), Hex("#E2ECF5"), Hex("#A9C8E4"), Hex("#4C7CB3"));
    }

    public static Sprite DesertSkySprite()
    {
        return SkyGradient("desert_sky",
            Hex("#FBE7C2"), Hex("#F4DDBE"), Hex("#A9C8DE"), Hex("#3F79B4"));
    }

    static Sprite SkyGradient(string name, Color horizon, Color low, Color mid, Color top)
    {
        int w = 8;
        int h = 256;
        Texture2D t = NewTex(w, h);

        for (int y = 0; y < h; y++)
        {
            float tt = y / (float)(h - 1);
            Color c;
            if (tt < 0.22f) c = Color.Lerp(horizon, low, tt / 0.22f);
            else if (tt < 0.55f) c = Color.Lerp(low, mid, (tt - 0.22f) / 0.33f);
            else c = Color.Lerp(mid, top, (tt - 0.55f) / 0.45f);
            for (int x = 0; x < w; x++) t.SetPixel(x, y, c);
        }

        return Import(name, t, false, Ppu);
    }

    public static Sprite MountainSprite()
    {
        return BuildMountainSprite("mountains",
            Hex("#AEC6D9"), Hex("#7E9BBC"), Hex("#5E7C9A"), 11, false);
    }

    public static Sprite SnowMountainSprite()
    {
        return BuildMountainSprite("snow_mountains",
            Hex("#DEE9F2"), Hex("#B7CCDE"), Hex("#8FAAC4"), 61, true);
    }

    // Desert backdrop: flat-topped mesas with layered cliff strata.
    public static Sprite DesertMesaSprite()
    {
        return BuildMesaSprite("desert_mesas",
            Hex("#EAD8BE"), Hex("#C9AC87"), Hex("#A6845E"), 91);
    }

    static Sprite BuildMesaSprite(string name, Color haze, Color far, Color near, int seedBase)
    {
        int w = 4096;
        int h = 128;
        Texture2D t = NewTex(w, h);

        float[] ridgeFar = new float[w];
        float[] ridgeNear = new float[w];
        for (int x = 0; x < w; x++)
        {
            // Quantised noise gives flat tops and steep cliff sides instead of sharp peaks.
            float nf = 0.6f * Noise(x, 520f, seedBase) + 0.4f * Noise(x, 190f, seedBase + 1);
            ridgeFar[x] = 26f + Mathf.Floor(nf * 5f) / 4f * 74f;
            float nn = 0.6f * Noise(x, 410f, seedBase + 10) + 0.4f * Noise(x, 150f, seedBase + 11);
            ridgeNear[x] = 12f + Mathf.Floor(nn * 4f) / 3f * 58f;
        }

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Color c = Color.clear;
                if (y <= ridgeFar[x])
                {
                    c = Color.Lerp(haze, far, Mathf.Clamp01(y / Mathf.Max(1f, ridgeFar[x]) * 1.15f));
                    if (Mathf.Abs(y - ridgeFar[x]) < 1.5f) c = Color.Lerp(c, Color.white, 0.14f);
                }
                if (y <= ridgeNear[x])
                {
                    c = Color.Lerp(haze, near, Mathf.Clamp01(y / Mathf.Max(1f, ridgeNear[x]) * 1.15f));
                    if (Mathf.RoundToInt(ridgeNear[x] - y) % 14 == 0) c = Color.Lerp(c, Color.black, 0.10f);
                    if (Mathf.Abs(y - ridgeNear[x]) < 1.5f) c = Color.Lerp(c, Color.white, 0.18f);
                }
                if (c.a > 0f) t.SetPixel(x, y, c);
            }
        }

        return Import(name, t, true, Ppu);
    }

    static Sprite BuildMountainSprite(string name, Color haze, Color far, Color near, int seedBase, bool snowCaps)
    {
        int w = 4096;
        int h = 128;
        Texture2D t = NewTex(w, h);

        float[] ridgeFar = new float[w];
        float[] ridgeNear = new float[w];
        for (int x = 0; x < w; x++)
        {
            float nf = 0.55f * Ridged(x, 340f, seedBase) + 0.30f * Ridged(x, 130f, seedBase + 1) + 0.15f * Ridged(x, 48f, seedBase + 2);
            ridgeFar[x] = 42f + nf * 74f;
            float nn = 0.55f * Ridged(x, 280f, seedBase + 10) + 0.30f * Ridged(x, 100f, seedBase + 11) + 0.15f * Ridged(x, 36f, seedBase + 12);
            ridgeNear[x] = 16f + nn * 62f;
        }

        Color snowFar = Hex("#F3F8FF");
        Color snowNear = Hex("#FFFFFF");

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Color c = Color.clear;
                if (y <= ridgeFar[x])
                {
                    c = Color.Lerp(haze, far, Mathf.Clamp01(y / Mathf.Max(1f, ridgeFar[x]) * 1.15f));
                    if (snowCaps)
                    {
                        // The higher the peak, the deeper the snow cap reaches down.
                        float depth = Mathf.Lerp(3f, 16f, ridgeFar[x] / 116f);
                        float d = ridgeFar[x] - y;
                        if (d < depth) c = Color.Lerp(c, snowFar, 1f - d / depth);
                    }
                    if (Mathf.Abs(y - ridgeFar[x]) < 1.5f) c = Color.Lerp(c, Color.white, 0.18f);
                }
                if (y <= ridgeNear[x])
                {
                    c = Color.Lerp(haze, near, Mathf.Clamp01(y / Mathf.Max(1f, ridgeNear[x]) * 1.15f));
                    if (snowCaps)
                    {
                        float depth = Mathf.Lerp(4f, 22f, ridgeNear[x] / 78f);
                        float d = ridgeNear[x] - y;
                        if (d < depth) c = Color.Lerp(c, snowNear, 1f - d / depth);
                    }
                    if (Mathf.Abs(y - ridgeNear[x]) < 1.5f) c = Color.Lerp(c, Color.white, 0.22f);
                }

                if (c.a > 0f)
                {
                    // Flat shading only: any per-pixel or block noise turns into visible
                    // grain/squares once this texture is upscaled on the phone.
                    t.SetPixel(x, y, c);
                }
            }
        }

        return Import(name, t, true, Ppu);
    }

    public static Sprite TreelineSprite()
    {
        int w = 4096;
        int h = 80;
        Texture2D t = NewTex(w, h);
        System.Random rng = new System.Random(77);

        float[] canopy = new float[w];
        for (int i = 0; i < 720; i++)
        {
            int cx = rng.Next(-16, w + 16);
            float rx = 4f + (float)rng.NextDouble() * 7f;
            float ry = 3f + (float)rng.NextDouble() * 5f;
            float cy = 5f + (float)rng.NextDouble() * 14f;
            if (i % 9 == 0)
            {
                ry = 8f + (float)rng.NextDouble() * 6f;
                cy = 12f + (float)rng.NextDouble() * 10f;
            }
            int span = Mathf.CeilToInt(rx);
            for (int dx = -span; dx <= span; dx++)
            {
                int x = cx + dx;
                if (x < 0 || x >= w) continue;
                float k = 1f - (dx * dx) / (rx * rx);
                if (k <= 0f) continue;
                float top = cy + ry * Mathf.Sqrt(k);
                if (top > canopy[x]) canopy[x] = top;
            }
        }

        Color baseCol = Hex("#2A5731");
        Color midCol = Hex("#356B3C");
        Color rimCol = Hex("#40804A");
        Color darkCol = Hex("#1B3A22");

        for (int x = 0; x < w; x++)
        {
            int top = Mathf.Clamp(Mathf.RoundToInt(canopy[x]), 0, h - 1);
            for (int y = 0; y <= top; y++)
            {
                Color c = Color.Lerp(darkCol, baseCol, y / (float)h);
                if (y == top) c = rimCol;
                else if (y == top - 1) c = Color.Lerp(midCol, rimCol, 0.6f);
                float v = Hash(x * 13 + y * 3, 9) * 0.10f - 0.05f;
                c = new Color(c.r + v, c.g + v, c.b + v, 1f);
                t.SetPixel(x, y, c);
            }
        }

        return Import("treeline", t, true, Ppu);
    }

    // Snow stage treeline: two bands of jagged pines, hazy far band behind a darker near band.
    public static Sprite PineTreelineSprite()
    {
        int w = 4096;
        int h = 96;
        Texture2D t = NewTex(w, h);
        System.Random rng = new System.Random(919);

        DrawPineBand(t, rng, 0f, 14f, 26f, Hex("#8FA9BF"), Hex("#DCE9F4"));
        DrawPineBand(t, rng, 6f, 20f, 46f, Hex("#2F4E60"), Hex("#EFF7FF"));

        return Import("pine_treeline", t, true, Ppu);
    }

    static void DrawPineBand(Texture2D t, System.Random rng, float baseY, float minH, float maxH, Color body, Color snow)
    {
        int x = -8;
        while (x < t.width + 8)
        {
            float height = Rand(rng, minH, maxH);
            float halfW = height * Rand(rng, 0.28f, 0.40f);
            DrawPine(t, x, baseY, halfW, height, body, snow);
            x += Mathf.Max(5, Mathf.RoundToInt(halfW * Rand(rng, 1.5f, 2.2f)));
        }
    }

    static void DrawPine(Texture2D t, int cx, float baseY, float halfW, float height, Color body, Color snow)
    {
        int w = t.width;
        int h = t.height;
        Color dark = Color.Lerp(body, Color.black, 0.25f);
        Color light = Color.Lerp(body, Color.white, 0.10f);

        int baseYi = Mathf.RoundToInt(baseY);
        int tipYi = Mathf.Min(h - 1, baseYi + Mathf.RoundToInt(height));

        for (int py = baseYi; py <= tipYi; py++)
        {
            float k = (py - baseY) / height;
            // Serrated edge: the width pulses every few rows so the silhouette reads as branches.
            float serration = 0.80f + 0.20f * Mathf.Abs(Mathf.Sin((py - baseY) * 1.35f));
            int hw = Mathf.RoundToInt(halfW * (1f - k) * serration);
            if (hw < 1) hw = 1;

            bool snowRow = ((py - baseYi) % 6) == 0;

            for (int dx = -hw; dx <= hw; dx++)
            {
                int px = cx + dx;
                if (px < 0 || px >= w) continue;

                Color c = body;
                if (dx == -hw) c = light;
                else if (dx == hw) c = dark;
                if (snowRow && dx <= 0) c = snow;
                t.SetPixel(px, py, c);
            }
        }

        if (tipYi >= 0 && tipYi < h) t.SetPixel(cx, tipYi, snow);
    }

    // Desert treeline: two dune bands with scattered saguaro silhouettes.
    public static Sprite DesertTreelineSprite()
    {
        int w = 4096;
        int h = 96;
        Texture2D t = NewTex(w, h);
        System.Random rng = new System.Random(929);

        DrawDuneBand(t, rng, 24f, Hex("#D9BE94"));
        DrawDuneBand(t, rng, 32f, Hex("#B99668"));
        DrawCactiBand(t, rng, 5f);

        return Import("desert_treeline", t, true, Ppu);
    }

    static void DrawDuneBand(Texture2D t, System.Random rng, float amplitude, Color color)
    {
        float period1 = Rand(rng, 300f, 520f);
        float period2 = Rand(rng, 90f, 160f);
        float period3 = Rand(rng, 24f, 60f);
        int seed = rng.Next(1, 100000);

        for (int x = 0; x < t.width; x++)
        {
            float n = 0.6f * Noise(x, period1, seed)
                + 0.3f * Noise(x, period2, seed + 1)
                + 0.1f * Noise(x, period3, seed + 2);
            int top = Mathf.Clamp(Mathf.RoundToInt(2f + n * amplitude), 0, t.height - 1);
            for (int y = 0; y <= top; y++)
            {
                Color c = color;
                if (y == top) c = Color.Lerp(color, Color.white, 0.10f);
                t.SetPixel(x, y, c);
            }
        }
    }

    static void DrawCactiBand(Texture2D t, System.Random rng, float baseY)
    {
        Color body = Hex("#4E6B3B");
        Color light = Hex("#6E8A4D");

        int x = rng.Next(12, 60);
        while (x < t.width - 12)
        {
            int height = rng.Next(22, 40);
            int trunkW = rng.Next(4, 6);
            DrawCactus(t, x, baseY, trunkW, height, body, light, rng);
            x += rng.Next(26, 80);
        }
    }

    static void DrawCactus(Texture2D t, int cx, float baseY, int trunkW, int height, Color body, Color light, System.Random rng)
    {
        int baseYI = Mathf.RoundToInt(baseY);
        int half = trunkW / 2;

        for (int y = 0; y <= height; y++)
        {
            int py = baseYI + y;
            if (py < 0 || py >= t.height) continue;
            int shrink = y == height ? 1 : 0;
            for (int dx = -half + shrink; dx <= trunkW - 1 - half - shrink; dx++)
            {
                int px = cx + dx;
                if (px < 0 || px >= t.width) continue;
                t.SetPixel(px, py, dx == trunkW - 1 - half - shrink ? light : body);
            }
        }

        if (rng.NextDouble() < 0.8) DrawCactusArm(t, cx - half - 1, baseYI + Mathf.RoundToInt(height * 0.45f), -1, body, light);
        if (rng.NextDouble() < 0.7) DrawCactusArm(t, cx + trunkW - 1 - half + 1, baseYI + Mathf.RoundToInt(height * 0.62f), 1, body, light);
    }

    static void DrawCactusArm(Texture2D t, int startX, int startY, int dir, Color body, Color light)
    {
        const int armLen = 5;
        const int upLen = 6;

        for (int i = 1; i <= armLen; i++)
        {
            int px = startX + i * dir;
            for (int dy = 0; dy <= 1; dy++)
            {
                int py = startY + dy;
                if (px >= 0 && px < t.width && py >= 0 && py < t.height)
                    t.SetPixel(px, py, dy == 0 ? body : light);
            }
        }

        int tipX = startX + armLen * dir;
        for (int j = 1; j <= upLen; j++)
        {
            int py = startY + j;
            if (py < 0 || py >= t.height) continue;
            if (tipX >= 0 && tipX < t.width) t.SetPixel(tipX, py, body);
            if (tipX + dir >= 0 && tipX + dir < t.width) t.SetPixel(tipX + dir, py, light);
        }
    }

    public static Sprite CloudSprite(int seed)
    {
        int w = 128;
        int h = 48;
        const int ss = 6; // supersampling factor for smooth edges
        Texture2D t = NewTex(w, h);

        // Puffy silhouette: a wide base plus rounded bumps, rendered at 6x and downsampled.
        Vector4[] ellipses =
        {
            new Vector4(64f, 19f, 40f, 8f),
            new Vector4(50f, 23f, 26f, 8f),
            new Vector4(80f, 22f, 24f, 8f),
            new Vector4(34f, 24f, 11f, 9f),
            new Vector4(47f, 30f, 13f, 11f),
            new Vector4(63f, 32f, 15f, 12f),
            new Vector4(79f, 30f, 13f, 11f),
            new Vector4(94f, 24f, 10f, 9f),
            new Vector4(69f, 24f, 10f, 9f)
        };

        float[] mask = new float[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int inside = 0;
                for (int sy = 0; sy < ss; sy++)
                {
                    for (int sx = 0; sx < ss; sx++)
                    {
                        float px = x + (sx + 0.5f) / ss;
                        float py = y + (sy + 0.5f) / ss;
                        for (int i = 0; i < ellipses.Length; i++)
                        {
                            Vector4 e = ellipses[i];
                            float nx = (px - e.x) / e.z;
                            float ny = (py - e.y) / e.w;
                            if (nx * nx + ny * ny <= 1f)
                            {
                                inside++;
                                break;
                            }
                        }
                    }
                }
                mask[y * w + x] = inside / (float)(ss * ss);
            }
        }

        Color white = Hex("#F8FBFF");
        Color shade = Hex("#C6D7E8");

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float a = mask[y * w + x] * 0.96f;
                if (a <= 0f) continue;
                Color c = Color.Lerp(shade, white, Mathf.Clamp01((y - 4f) / 26f));
                c.a = a;
                t.SetPixel(x, y, c);
            }
        }

        return Import("cloud", t, false, Ppu);
    }

    public static Sprite VignetteSprite()
    {
        int w = 256;
        int h = 144;
        Texture2D t = NewTex(w, h);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = x / (w - 1f) * 2f - 1f;
                float v = y / (h - 1f) * 2f - 1f;
                float d = Mathf.Sqrt(u * u * 0.8f + v * v * 0.9f);
                float f = Mathf.Clamp01((d - 0.55f) / 0.7f);
                f = f * f * (3f - 2f * f);
                // Fade out at the texture border, otherwise the sprite edge shows as a hard line.
                float edge = Mathf.Min(Mathf.Min(u + 1f, 1f - u), Mathf.Min(v + 1f, 1f - v));
                f *= Mathf.Clamp01(edge / 0.12f);
                t.SetPixel(x, y, new Color(0.02f, 0.03f, 0.06f, f * 0.38f));
            }
        }
        return Import("vignette", t, false, Ppu);
    }

    // ------------------------------------------------------------------ decorations

    public static Sprite[] TuftSprites()
    {
        Dictionary<char, Color> palette = new Dictionary<char, Color>
        {
            { 'G', Hex("#7CBB50") },
            { 'g', Hex("#4C8A33") },
            { 'd', Hex("#2C5A1E") }
        };
        string[] rows =
        {
            "..G...G.",
            "..G..gG.",
            ".gG..gg.",
            ".gG.gg..",
            ".gggggG.",
            "..dggd..",
            "...dd..."
        };
        return new[] { FromMap("tuft_a", rows, palette) };
    }

    public static Sprite[] BushSprites()
    {
        return new[]
        {
            FromTexture("bush_a", BuildBush(4101)),
            FromTexture("bush_b", BuildBush(4102))
        };
    }

    static Texture2D BuildBush(int seed)
    {
        int w = 24;
        int h = 16;
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(w, h);

        float[] top = new float[w];
        for (int i = 0; i < 6; i++)
        {
            int cx = rng.Next(4, 21);
            float r = Rand(rng, 5f, 8f);
            int span = Mathf.CeilToInt(r);
            for (int dx = -span; dx <= span; dx++)
            {
                int x = cx + dx;
                if (x < 0 || x >= w) continue;
                float k = 1f - (dx * dx) / (r * r);
                if (k <= 0f) continue;
                float tp = 5f + r * Mathf.Sqrt(k);
                if (tp > top[x]) top[x] = tp;
            }
        }

        Color baseCol = Hex("#356B36");
        Color lightCol = Hex("#4C8C45");
        Color darkCol = Hex("#22491F");

        for (int x = 0; x < w; x++)
        {
            int tp = Mathf.Clamp(Mathf.RoundToInt(top[x]), 0, h - 1);
            if (tp <= 0) continue;
            for (int y = 0; y <= tp; y++)
            {
                Color c = Color.Lerp(darkCol, baseCol, y / (float)tp);
                if (y == tp) c = lightCol;
                float v = Hash(x * 31 + y * 7, 3) * 0.14f - 0.07f;
                c = new Color(c.r + v, c.g + v, c.b + v, 1f);
                t.SetPixel(x, y, c);
            }
        }

        return t;
    }

    public static Sprite RockSprite()
    {
        int w = 12;
        int h = 8;
        Texture2D t = NewTex(w, h);
        Color baseCol = Hex("#7D7466");
        Color light = Hex("#9A9080");
        Color dark = Hex("#4E463A");
        Color moss = Hex("#557A38");

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x - 5.5f) / 5.5f;
                float ny = (y - 3.5f) / 3.5f;
                if (nx * nx + ny * ny > 1f) continue;
                Color c = baseCol;
                if (nx + ny < -0.6f) c = light;
                else if (nx + ny > 0.7f) c = dark;
                if (y >= 5 && nx < 0.2f) c = moss;
                t.SetPixel(x, y, c);
            }
        }

        return FromTexture("rock", t);
    }

    // ------------------------------------------------------------------ snow decorations

    public static Sprite[] SnowDriftSprites()
    {
        return new[]
        {
            FromTexture("snow_drift_a", BuildSnowDrift(6101)),
            FromTexture("snow_drift_b", BuildSnowDrift(6102))
        };
    }

    public static Sprite[] SnowBushSprites()
    {
        return new[]
        {
            FromTexture("snow_bush_a", BuildSnowBush(6201)),
            FromTexture("snow_bush_b", BuildSnowBush(6202))
        };
    }

    static Texture2D BuildSnowDrift(int seed)
    {
        int w = 16;
        int h = 8;
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(w, h);

        Color snowTop = Hex("#F6FAFF");
        Color snowMid = Hex("#E2EDF8");
        Color shade = Hex("#BCD2E6");

        float[] top = new float[w];
        for (int i = 0; i < 4; i++)
        {
            int cx = rng.Next(2, 14);
            float r = Rand(rng, 3f, 6.5f);
            int span = Mathf.CeilToInt(r);
            for (int dx = -span; dx <= span; dx++)
            {
                int x = cx + dx;
                if (x < 0 || x >= w) continue;
                float k = 1f - (dx * dx) / (r * r);
                if (k <= 0f) continue;
                float tp = 1f + r * Mathf.Sqrt(k);
                if (tp > top[x]) top[x] = tp;
            }
        }

        for (int x = 0; x < w; x++)
        {
            int tp = Mathf.Clamp(Mathf.RoundToInt(top[x]), 0, h - 1);
            if (tp <= 0) continue;
            for (int y = 0; y <= tp; y++)
            {
                Color c = Color.Lerp(shade, snowTop, y / (float)tp);
                if (y == tp) c = snowTop;
                else if (y >= tp - 1) c = snowMid;
                t.SetPixel(x, y, c);
            }
        }

        return t;
    }

    static Texture2D BuildSnowBush(int seed)
    {
        int w = 24;
        int h = 16;
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(w, h);

        float[] top = new float[w];
        for (int i = 0; i < 6; i++)
        {
            int cx = rng.Next(4, 21);
            float r = Rand(rng, 5f, 8f);
            int span = Mathf.CeilToInt(r);
            for (int dx = -span; dx <= span; dx++)
            {
                int x = cx + dx;
                if (x < 0 || x >= w) continue;
                float k = 1f - (dx * dx) / (r * r);
                if (k <= 0f) continue;
                float tp = 4f + r * Mathf.Sqrt(k);
                if (tp > top[x]) top[x] = tp;
            }
        }

        Color baseCol = Hex("#3E5B52");
        Color darkCol = Hex("#2A4038");
        Color snowTop = Hex("#F2F8FF");
        Color snowShade = Hex("#C9DCEB");

        for (int x = 0; x < w; x++)
        {
            int tp = Mathf.Clamp(Mathf.RoundToInt(top[x]), 0, h - 1);
            if (tp <= 0) continue;
            for (int y = 0; y <= tp; y++)
            {
                Color c = Color.Lerp(darkCol, baseCol, y / (float)tp);
                if (y >= tp - 1) c = snowTop;
                else if (y >= tp - 3) c = snowShade;
                float v = Hash(x * 31 + y * 7, 3) * 0.10f - 0.05f;
                c = new Color(c.r + v, c.g + v, c.b + v, 1f);
                t.SetPixel(x, y, c);
            }
        }

        return t;
    }

    public static Sprite IceRockSprite()
    {
        int w = 12;
        int h = 8;
        Texture2D t = NewTex(w, h);
        Color baseCol = Hex("#7E93A6");
        Color light = Hex("#A8BDCE");
        Color dark = Hex("#4E6072");
        Color snow = Hex("#EEF6FD");

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x - 5.5f) / 5.5f;
                float ny = (y - 3.5f) / 3.5f;
                if (nx * nx + ny * ny > 1f) continue;
                Color c = baseCol;
                if (nx + ny < -0.6f) c = light;
                else if (nx + ny > 0.7f) c = dark;
                if (y >= 5) c = snow;
                t.SetPixel(x, y, c);
            }
        }

        return FromTexture("ice_rock", t);
    }

    // Small snow-covered pine used as a ground decoration on the winter stage.
    public static Sprite PineSprite()
    {
        int w = 24;
        int h = 40;
        Texture2D t = NewTex(w, h);

        Color trunk = Hex("#4A3A2A");
        Color trunkDark = Hex("#33281D");
        Color foliageDark = Hex("#1E3D38");
        Color foliage = Hex("#2E5A4C");
        Color foliageLight = Hex("#3E725F");
        Color snow = Hex("#F2F8FF");
        Color snowShade = Hex("#C7D9E8");

        for (int y = 0; y < 9; y++)
        {
            t.SetPixel(11, y, trunkDark);
            t.SetPixel(12, y, trunk);
        }

        for (int tier = 0; tier < 3; tier++)
        {
            int baseY = 6 + tier * 10;
            int tierH = 16;
            float halfW = 10f - tier * 2.4f;

            for (int y = 0; y < tierH; y++)
            {
                float k = y / (float)tierH;
                int hw = Mathf.RoundToInt(halfW * (1f - k));
                if (hw < 1) hw = 1;
                int py = baseY + y;
                if (py >= h) break;

                for (int dx = -hw; dx <= hw; dx++)
                {
                    int px = 12 + dx;
                    if (px < 0 || px >= w) continue;
                    Color c = foliage;
                    if (dx == hw) c = foliageDark;
                    else if (dx == -hw || y == tierH - 1) c = foliageLight;
                    t.SetPixel(px, py, c);
                }

                // Snow resting on the lower edge of each tier.
                if (y == 0)
                {
                    for (int dx = -hw; dx <= 0; dx++)
                    {
                        int px = 12 + dx;
                        if (px >= 0 && px < w) t.SetPixel(px, py, snow);
                    }
                }
            }

            int tipY = Mathf.Min(h - 1, baseY + tierH);
            for (int dx = -1; dx <= 1; dx++)
            {
                int px = 12 + dx;
                if (px >= 0 && px < w) t.SetPixel(px, tipY, tier == 2 ? snow : snowShade);
            }
        }

        return Import("pine", t, true, Ppu);
    }

    // ------------------------------------------------------------------ desert decorations

    public static Sprite[] DesertTuftSprites()
    {
        Dictionary<char, Color> palette = new Dictionary<char, Color>
        {
            { 'y', Hex("#C8B565") },
            { 'g', Hex("#8A8A4A") },
            { 'd', Hex("#5F6234") }
        };
        string[] rows =
        {
            "..y...y.",
            "..y..gy.",
            ".gy..gg.",
            ".gy.gg..",
            ".gggggy.",
            "..dggd..",
            "...dd..."
        };
        return new[] { FromMap("desert_tuft", rows, palette) };
    }

    public static Sprite[] DesertBushSprites()
    {
        return new[]
        {
            FromTexture("desert_bush_a", BuildDryBush(6301)),
            FromTexture("desert_bush_b", BuildDryBush(6302))
        };
    }

    static Texture2D BuildDryBush(int seed)
    {
        int w = 20;
        int h = 14;
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(w, h);

        float[] top = new float[w];
        for (int i = 0; i < 5; i++)
        {
            int cx = rng.Next(3, 18);
            float r = Rand(rng, 4f, 7f);
            int span = Mathf.CeilToInt(r);
            for (int dx = -span; dx <= span; dx++)
            {
                int x = cx + dx;
                if (x < 0 || x >= w) continue;
                float k = 1f - (dx * dx) / (r * r);
                if (k <= 0f) continue;
                float tp = 3f + r * Mathf.Sqrt(k);
                if (tp > top[x]) top[x] = tp;
            }
        }

        Color baseCol = Hex("#8A8A4A");
        Color lightCol = Hex("#A8A860");
        Color darkCol = Hex("#5F6234");

        for (int x = 0; x < w; x++)
        {
            int tp = Mathf.Clamp(Mathf.RoundToInt(top[x]), 0, h - 1);
            if (tp <= 0) continue;
            for (int y = 0; y <= tp; y++)
            {
                Color c = Color.Lerp(darkCol, baseCol, y / (float)tp);
                if (y == tp) c = lightCol;
                float v = Hash(x * 37 + y * 11, 5) * 0.12f - 0.06f;
                c = new Color(c.r + v, c.g + v, c.b + v, 1f);
                t.SetPixel(x, y, c);
            }
        }

        return t;
    }

    public static Sprite SandRockSprite()
    {
        int w = 14;
        int h = 10;
        Texture2D t = NewTex(w, h);
        Color baseCol = Hex("#B49A72");
        Color light = Hex("#C9B28C");
        Color dark = Hex("#7E6547");
        Color sand = Hex("#E8C978");

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x - 6.5f) / 6.5f;
                float ny = (y - 4.5f) / 4.5f;
                if (nx * nx + ny * ny > 1f) continue;
                Color c = baseCol;
                if (nx + ny < -0.5f) c = light;
                else if (nx + ny > 0.8f) c = dark;
                if (y >= 7) c = sand;
                t.SetPixel(x, y, c);
            }
        }

        return FromTexture("sand_rock", t);
    }

    // Tall saguaro used as a ground decoration on the desert stage.
    public static Sprite CactusSprite()
    {
        int w = 16;
        int h = 40;
        Texture2D t = NewTex(w, h);

        Color body = Hex("#4F7A3A");
        Color dark = Hex("#35592A");
        Color light = Hex("#6E9B4E");
        Color spine = Hex("#D8CFA0");
        Color sand = Hex("#DFC38F");

        for (int x = 3; x <= 12; x++)
        {
            t.SetPixel(x, 0, sand);
            if (x >= 4 && x <= 11) t.SetPixel(x, 1, sand);
        }

        for (int y = 2; y <= 36; y++)
        {
            for (int dx = 0; dx < 4; dx++)
            {
                Color c = dx == 0 ? dark : (dx == 3 ? light : body);
                t.SetPixel(6 + dx, y, c);
            }
        }
        for (int x = 7; x <= 8; x++) t.SetPixel(x, 37, body);

        DrawCactusSprout(t, 6, 14, -1, body, dark, light);
        DrawCactusSprout(t, 9, 21, 1, body, dark, light);

        for (int y = 4; y < 36; y += 4)
        {
            if (y % 8 == 0) t.SetPixel(5, y, spine);
            else t.SetPixel(10, y, spine);
        }

        return Import("cactus", t, true, Ppu);
    }

    static void DrawCactusSprout(Texture2D t, int startX, int startY, int dir, Color body, Color dark, Color light)
    {
        for (int i = 1; i <= 3; i++)
        {
            int px = startX + i * dir;
            t.SetPixel(px, startY, body);
            t.SetPixel(px, startY + 1, dark);
        }
        int tipX = startX + 3 * dir;
        for (int j = 2; j <= 6; j++) t.SetPixel(tipX, startY + j, body);
        t.SetPixel(tipX, startY + 7, light);
    }

    // ------------------------------------------------------------------ cave theme

    public static Sprite CaveSkySprite()
    {
        return SkyGradient("cave_sky",
            Hex("#2E241B"), Hex("#221B15"), Hex("#191410"), Hex("#0C0A08"));
    }

    // Far cave wall with jagged rock masses and occasional crystal glints.
    public static Sprite CaveWallSprite()
    {
        return BuildCaveWallSprite("cave_wall", Hex("#2B241E"), Hex("#3C332A"), Hex("#4F4538"), 131);
    }

    static Sprite BuildCaveWallSprite(string name, Color dark, Color mid, Color light, int seedBase)
    {
        int w = 4096;
        int h = 128;
        Texture2D t = NewTex(w, h);

        float[] ridgeFar = new float[w];
        float[] ridgeNear = new float[w];
        for (int x = 0; x < w; x++)
        {
            float nf = 0.55f * Ridged(x, 380f, seedBase) + 0.30f * Ridged(x, 140f, seedBase + 1) + 0.15f * Ridged(x, 50f, seedBase + 2);
            ridgeFar[x] = 26f + nf * 86f;
            float nn = 0.55f * Ridged(x, 300f, seedBase + 10) + 0.30f * Ridged(x, 110f, seedBase + 11) + 0.15f * Ridged(x, 44f, seedBase + 12);
            ridgeNear[x] = 12f + nn * 64f;
        }

        Color glint = Hex("#63D8E8");

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Color c = Color.clear;
                if (y <= ridgeFar[x])
                {
                    c = Color.Lerp(dark, mid, Mathf.Clamp01(y / Mathf.Max(1f, ridgeFar[x]) * 1.15f));
                    if (Mathf.Abs(y - ridgeFar[x]) < 1.5f) c = Color.Lerp(c, light, 0.35f);
                }
                if (y <= ridgeNear[x])
                {
                    c = Color.Lerp(mid, light, Mathf.Clamp01(y / Mathf.Max(1f, ridgeNear[x]) * 1.15f));
                    if (Mathf.Abs(y - ridgeNear[x]) < 1.5f) c = Color.Lerp(c, light, 0.5f);
                }

                if (c.a > 0f)
                {
                    if (Hash(x * 7 + y * 13, 3) > 0.99935f) c = glint;
                    t.SetPixel(x, y, c);
                }
            }
        }

        return Import(name, t, true, Ppu);
    }

    // Cave treeline: stalagmites rising from the floor, stalactites hanging from the ceiling.
    public static Sprite CaveTreelineSprite()
    {
        int w = 4096;
        int h = 96;
        Texture2D t = NewTex(w, h);
        System.Random rng = new System.Random(1421);

        Color body = Hex("#3D362E");
        Color light = Hex("#5A5044");
        Color dark = Hex("#241F1A");

        int x = rng.Next(4, 24);
        while (x < w - 4)
        {
            int height = rng.Next(14, 52);
            int halfW = rng.Next(3, 9);
            DrawSpike(t, x, 2, halfW, height, body, light, dark, true);
            x += rng.Next(20, 74);
        }

        x = rng.Next(10, 60);
        while (x < w - 10)
        {
            int height = rng.Next(8, 34);
            int halfW = rng.Next(2, 7);
            DrawSpike(t, x, h - 2, halfW, height, body, light, dark, false);
            x += rng.Next(46, 140);
        }

        return Import("cave_treeline", t, true, Ppu);
    }

    // Cone spike. upwards = stalagmite (base at baseY), otherwise a hanging stalactite (top at baseY).
    static void DrawSpike(Texture2D t, int cx, int baseY, int halfW, int height,
        Color body, Color light, Color dark, bool upwards)
    {
        for (int y = 0; y <= height; y++)
        {
            float k = y / (float)height;
            int hw = Mathf.RoundToInt(halfW * (1f - k));
            if (hw < 1) hw = 1;
            int py = upwards ? baseY + y : baseY - y;
            if (py < 0 || py >= t.height) continue;

            for (int dx = -hw; dx <= hw; dx++)
            {
                int px = cx + dx;
                if (px < 0 || px >= t.width) continue;
                Color c = body;
                if (dx == -hw) c = light;
                else if (dx == hw) c = dark;
                t.SetPixel(px, py, c);
            }
        }
    }

    public static Sprite[] CaveGroundTops()
    {
        return new[]
        {
            FromTexture("cave_ground_top0", BuildCaveGroundTop(1601)),
            FromTexture("cave_ground_top1", BuildCaveGroundTop(1602)),
            FromTexture("cave_ground_top2", BuildCaveGroundTop(1603))
        };
    }

    public static Sprite[] CaveFills()
    {
        return new[]
        {
            FromTexture("cave_fill0", BuildCaveFill(2601)),
            FromTexture("cave_fill1", BuildCaveFill(2602))
        };
    }

    public static Sprite CavePlatformSprite()
    {
        return FromTexture("mine_platform", BuildMinePlatform(3601));
    }

    static Texture2D BuildCaveGroundTop(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color rockBase = Hex("#4A443C");
        Color rockDark = Hex("#2E2A24");
        Color rockLight = Hex("#615A4F");
        Color stone = Hex("#6E675B");
        Color stoneDark = Hex("#3A352E");
        Color edgeLight = Hex("#8A8174");
        Color crystal = Hex("#59D0E0");

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float v = (float)rng.NextDouble();
                Color c = Color.Lerp(rockDark, rockBase, Mathf.Clamp01(y / 12f));
                if (v < 0.10f) c = Color.Lerp(c, rockDark, 0.5f);
                else if (v > 0.94f) c = Color.Lerp(c, rockLight, 0.5f);
                t.SetPixel(x, y, c);
            }
        }

        for (int i = 0; i < 3; i++)
        {
            int px = rng.Next(1, 14);
            int py = rng.Next(1, 8);
            t.SetPixel(px, py, stone);
            t.SetPixel(px + 1, py, stone);
            t.SetPixel(px, py + 1, Color.Lerp(stone, rockLight, 0.4f));
            t.SetPixel(px + 1, py + 1, stoneDark);
            t.SetPixel(px, py - 1, stoneDark);
        }

        int[] top = new int[16];
        for (int x = 0; x < 16; x++) top[x] = 10 + rng.Next(0, 5);

        for (int x = 0; x < 16; x++)
        {
            t.SetPixel(x, top[x], edgeLight);
            t.SetPixel(x, top[x] - 1, stone);
            t.SetPixel(x, top[x] - 2, rng.NextDouble() < 0.5f ? rockLight : rockBase);
            t.SetPixel(x, top[x] - 3, Color.Lerp(rockBase, rockDark, 0.4f));

            if (rng.NextDouble() < 0.30) t.SetPixel(x, top[x], stone);
            if (rng.NextDouble() < 0.12) t.SetPixel(x, top[x] - 1, crystal);
            if (rng.NextDouble() < 0.25) t.SetPixel(x, top[x] - 3, rockDark);
        }

        if (rng.NextDouble() < 0.4)
        {
            int fx = rng.Next(0, 16);
            t.SetPixel(fx, top[fx] - 1, Color.Lerp(crystal, Color.white, 0.4f));
        }

        return t;
    }

    static Texture2D BuildCaveFill(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color baseCol = Hex("#3A342C");
        Color dark = Hex("#241F1A");
        Color light = Hex("#4E463C");
        Color stone = Hex("#5A5145");

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float v = (float)rng.NextDouble();
                Color c = baseCol;
                if (v < 0.08f) c = Color.Lerp(c, dark, 0.45f);
                else if (v > 0.95f) c = Color.Lerp(c, light, 0.40f);
                t.SetPixel(x, y, c);
            }
        }

        // A couple of diagonal cracks keep the fill from reading as flat noise.
        for (int i = 0; i < 2; i++)
        {
            int cx = rng.Next(2, 12);
            int cy = rng.Next(2, 12);
            for (int step = 0; step < 8; step++)
            {
                int px = cx + step / 2;
                int py = cy + step;
                if (px >= 0 && px < 16 && py >= 0 && py < 16)
                    t.SetPixel(px, py, Color.Lerp(dark, Color.black, 0.2f));
            }
        }

        for (int i = 0; i < 3; i++)
        {
            int px = rng.Next(1, 15);
            int py = rng.Next(1, 15);
            t.SetPixel(px, py, stone);
            t.SetPixel(px, py + 1, Color.Lerp(stone, dark, 0.4f));
            t.SetPixel(px + 1, py, Color.Lerp(stone, dark, 0.3f));
        }

        return t;
    }

    static Texture2D BuildMinePlatform(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color plankTop = Hex("#C08B4C");
        Color plank = Hex("#9A6B36");
        Color plankDark = Hex("#6E4A22");
        Color beam = Hex("#5A3A1A");
        Color bottom = Hex("#33220F");
        Color bolt = Hex("#B9B4A8");

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                Color c;
                if (y == 15) c = plankTop;
                else if (y == 14) c = plank;
                else if (y == 13) c = plankDark;
                else if (y <= 2) c = Color.Lerp(bottom, beam, y / 2f);
                else c = Color.Lerp(beam, plankDark, Mathf.Clamp01((y - 2) / 11f));
                t.SetPixel(x, y, c);
            }
        }

        for (int sx = 2; sx < 16; sx += 4)
        {
            for (int y = 3; y <= 13; y++) t.SetPixel(sx, y, Color.Lerp(beam, Color.black, 0.2f));
        }

        int[] bolts = { 2, 7, 13 };
        for (int i = 0; i < bolts.Length; i++)
        {
            t.SetPixel(bolts[i], 11, Hex("#6E6658"));
            t.SetPixel(bolts[i], 12, bolt);
        }

        for (int i = 0; i < 4; i++)
        {
            int gx = rng.Next(3, 14);
            int gy = rng.Next(4, 12);
            t.SetPixel(gx, gy, plankDark);
        }

        return t;
    }

    public static Sprite CaveDepthShadeSprite()
    {
        return BuildDepthShade("depth_shade_cave", new Color(0.02f, 0.02f, 0.04f));
    }

    public static Sprite[] CaveSmallSprites()
    {
        return new[] { FromTexture("cave_stalagmite", BuildStalagmiteDecor(6401)) };
    }

    static Texture2D BuildStalagmiteDecor(int seed)
    {
        int w = 12;
        int h = 18;
        Texture2D t = NewTex(w, h);
        Color body = Hex("#514A40");
        Color light = Hex("#6E6557");
        Color dark = Hex("#332E27");

        for (int y = 0; y <= 15; y++)
        {
            float k = y / 15f;
            int hw = Mathf.RoundToInt(5f * (1f - k));
            if (hw < 1) hw = 1;
            for (int dx = -hw; dx <= hw; dx++)
            {
                int px = 6 + dx;
                if (px < 0 || px >= w) continue;
                Color c = body;
                if (dx == -hw) c = light;
                else if (dx == hw) c = dark;
                t.SetPixel(px, y, c);
            }
        }

        return t;
    }

    public static Sprite[] CaveCrystalSprites()
    {
        return new[] { FromTexture("cave_crystal", BuildCrystalCluster(6411)) };
    }

    static Texture2D BuildCrystalCluster(int seed)
    {
        int w = 16;
        int h = 16;
        Texture2D t = NewTex(w, h);
        Color rock = Hex("#3A342C");
        Color core = Hex("#8CF2FA");
        Color mid = Hex("#45C8DC");
        Color dark = Hex("#2A7E96");

        for (int x = 2; x <= 13; x++) t.SetPixel(x, 0, rock);
        for (int x = 4; x <= 11; x++) t.SetPixel(x, 1, Color.Lerp(rock, Color.black, 0.2f));

        // Central shard.
        for (int y = 2; y <= 13; y++)
        {
            t.SetPixel(8, y, core);
            t.SetPixel(9, y, mid);
            t.SetPixel(10, y, dark);
        }
        t.SetPixel(8, 14, Color.white);

        // Side shards.
        for (int y = 2; y <= 9; y++)
        {
            t.SetPixel(5, y, mid);
            t.SetPixel(6, y, dark);
        }
        t.SetPixel(5, 10, core);

        for (int y = 2; y <= 11; y++)
        {
            t.SetPixel(12, y, mid);
            t.SetPixel(13, y, dark);
        }
        t.SetPixel(12, 12, core);

        // Glow specks around the tips.
        t.SetPixel(9, 14, Color.Lerp(core, Color.white, 0.5f));
        t.SetPixel(6, 10, Color.Lerp(core, Color.white, 0.5f));
        t.SetPixel(13, 12, Color.Lerp(core, Color.white, 0.5f));

        return t;
    }

    public static Sprite CaveRockSprite()
    {
        int w = 12;
        int h = 8;
        Texture2D t = NewTex(w, h);
        Color baseCol = Hex("#5A5145");
        Color light = Hex("#746A5A");
        Color dark = Hex("#332E27");

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x - 5.5f) / 5.5f;
                float ny = (y - 3.5f) / 3.5f;
                if (nx * nx + ny * ny > 1f) continue;
                Color c = baseCol;
                if (nx + ny < -0.6f) c = light;
                else if (nx + ny > 0.7f) c = dark;
                t.SetPixel(x, y, c);
            }
        }

        return FromTexture("cave_rock", t);
    }

    public static Sprite CavePillarSprite()
    {
        int w = 16;
        int h = 40;
        Texture2D t = NewTex(w, h);
        Color body = Hex("#4A4339");
        Color light = Hex("#6A6153");
        Color dark = Hex("#2A2620");

        for (int y = 0; y <= 37; y++)
        {
            float k = y / 37f;
            int hw = Mathf.RoundToInt(7f * (1f - k));
            if (hw < 1) hw = 1;
            for (int dx = -hw; dx <= hw; dx++)
            {
                int px = 8 + dx;
                if (px < 0 || px >= w) continue;
                Color c = body;
                if (dx == -hw) c = light;
                else if (dx == hw) c = dark;
                if (y % 6 == 0 && dx > -hw) c = Color.Lerp(c, dark, 0.45f);
                t.SetPixel(px, y, c);
            }
        }

        return Import("cave_pillar", t, true, Ppu);
    }

    // ------------------------------------------------------------------ factory theme

    public static Sprite FactorySkySprite()
    {
        return SkyGradient("factory_sky",
            Hex("#2E3742"), Hex("#232B35"), Hex("#191F27"), Hex("#0E1116"));
    }

    // Industrial skyline: stepped block silhouettes with sparse lit windows.
    public static Sprite FactoryBackdropSprite()
    {
        return BuildFactoryBackdrop("factory_backdrop", Hex("#2A323C"), Hex("#1D242C"), Hex("#E8C25A"), 151);
    }

    static Sprite BuildFactoryBackdrop(string name, Color far, Color near, Color window, int seedBase)
    {
        int w = 4096;
        int h = 128;
        Texture2D t = NewTex(w, h);

        float[] ridgeFar = new float[w];
        float[] ridgeNear = new float[w];
        for (int x = 0; x < w; x++)
        {
            float nf = 0.55f * Noise(x, 260f, seedBase) + 0.30f * Noise(x, 90f, seedBase + 1) + 0.15f * Noise(x, 30f, seedBase + 2);
            ridgeFar[x] = 34f + Mathf.Floor(nf * 5f) / 4f * 70f;
            float nn = 0.55f * Noise(x, 220f, seedBase + 10) + 0.30f * Noise(x, 80f, seedBase + 11) + 0.15f * Noise(x, 26f, seedBase + 12);
            ridgeNear[x] = 18f + Mathf.Floor(nn * 4f) / 3f * 54f;
        }

        for (int x = 0; x < w; x++)
        {
            int farTop = Mathf.Clamp(Mathf.RoundToInt(ridgeFar[x]), 0, h - 1);
            for (int y = 0; y <= farTop; y++)
            {
                Color c = Color.Lerp(far, Color.Lerp(far, Color.black, 0.25f), 1f - y / (float)Mathf.Max(1, farTop));
                t.SetPixel(x, y, c);
            }
            if (farTop >= 4) t.SetPixel(x, farTop, Color.Lerp(far, Color.white, 0.12f));
        }

        for (int x = 0; x < w; x++)
        {
            int farTop = Mathf.RoundToInt(ridgeFar[x]);
            for (int y = 8; y < farTop - 4; y += 8)
            {
                if (Hash(x * 13 + y * 29, seedBase) > 0.9955f)
                {
                    t.SetPixel(x, y, window);
                    if (x + 1 < w) t.SetPixel(x + 1, y, window);
                }
            }
        }

        for (int x = 0; x < w; x++)
        {
            int nearTop = Mathf.Clamp(Mathf.RoundToInt(ridgeNear[x]), 0, h - 1);
            for (int y = 0; y <= nearTop; y++)
            {
                Color c = Color.Lerp(near, Color.Lerp(near, Color.black, 0.35f), 1f - y / (float)Mathf.Max(1, nearTop));
                t.SetPixel(x, y, c);
            }
            if (nearTop >= 4) t.SetPixel(x, nearTop, Color.Lerp(near, Color.white, 0.10f));
        }

        return Import(name, t, true, Ppu);
    }

    // Foreground factory pipes with warning lights.
    public static Sprite FactoryTreelineSprite()
    {
        int w = 4096;
        int h = 96;
        Texture2D t = NewTex(w, h);
        System.Random rng = new System.Random(1521);

        Color pipe = Hex("#2B3037");
        Color pipeLight = Hex("#454C55");
        Color dark = Hex("#1A1E24");
        Color warn = Hex("#E85A3C");

        for (int x = 0; x < w; x++)
        {
            for (int y = 16; y <= 19; y++) t.SetPixel(x, y, y == 19 ? pipeLight : pipe);
        }

        int px = rng.Next(10, 50);
        while (px < w - 10)
        {
            int height = rng.Next(20, 62);
            int width = rng.Next(3, 6);
            for (int y = 0; y <= height; y++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    Color c = dx == 0 ? pipeLight : (dx == width - 1 ? dark : pipe);
                    t.SetPixel(px + dx, y, c);
                }
            }

            for (int dx = -1; dx <= width; dx++)
            {
                int fx = px + dx;
                if (fx >= 0 && fx < w) t.SetPixel(fx, height + 1, pipeLight);
            }

            if (rng.NextDouble() < 0.4 && height + 2 < h)
            {
                t.SetPixel(px + width / 2, height + 2, warn);
            }

            px += rng.Next(46, 130);
        }

        return Import("factory_treeline", t, true, Ppu);
    }

    public static Sprite[] FactoryGroundTops()
    {
        return new[]
        {
            FromTexture("factory_ground_top0", BuildFactoryGroundTop(1801)),
            FromTexture("factory_ground_top1", BuildFactoryGroundTop(1802)),
            FromTexture("factory_ground_top2", BuildFactoryGroundTop(1803))
        };
    }

    public static Sprite[] FactoryFills()
    {
        return new[]
        {
            FromTexture("factory_fill0", BuildFactoryFill(2801)),
            FromTexture("factory_fill1", BuildFactoryFill(2802))
        };
    }

    public static Sprite FactoryPlatformSprite()
    {
        return FromTexture("factory_platform", BuildFactoryPlatform(3801));
    }

    static Texture2D BuildFactoryGroundTop(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color steel = Hex("#6E7681");
        Color steelLight = Hex("#9AA3AE");
        Color steelDark = Hex("#474E57");
        Color body = Hex("#3C424A");
        Color bottom = Hex("#23272C");
        Color hazard = Hex("#D8B44A");
        Color hazardDark = Hex("#2A2C30");
        Color rivet = Hex("#B9C0C8");

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                Color c;
                if (y == 15) c = (x / 2) % 2 == 0 ? hazard : hazardDark;
                else if (y == 14) c = steelLight;
                else if (y == 13) c = steel;
                else if (y <= 2) c = Color.Lerp(bottom, steelDark, y / 2f);
                else c = Color.Lerp(steelDark, body, Mathf.Clamp01((y - 2) / 11f));
                t.SetPixel(x, y, c);
            }
        }

        for (int sx = 0; sx < 16; sx += 8)
        {
            for (int y = 3; y <= 13; y++) t.SetPixel(sx, y, Color.Lerp(body, Color.black, 0.25f));
        }

        for (int rx = 3; rx < 16; rx += 5)
        {
            t.SetPixel(rx, 12, rivet);
            t.SetPixel(rx, 6, rivet);
        }

        for (int y = 3; y < 13; y++)
        {
            if (rng.NextDouble() < 0.10) t.SetPixel(rng.Next(0, 16), y, Color.Lerp(steelDark, Color.white, 0.15f));
        }

        return t;
    }

    static Texture2D BuildFactoryFill(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color baseCol = Hex("#3C424A");
        Color dark = Hex("#2A2F35");
        Color light = Hex("#4A515A");
        Color rivet = Hex("#8A939D");

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float v = (float)rng.NextDouble();
                Color c = baseCol;
                if (v < 0.08f) c = Color.Lerp(c, dark, 0.45f);
                else if (v > 0.95f) c = Color.Lerp(c, light, 0.40f);
                t.SetPixel(x, y, c);
            }
        }

        // Horizontal plate seam with rivets.
        int seam = rng.Next(4, 12);
        for (int x = 0; x < 16; x++) t.SetPixel(x, seam, Color.Lerp(baseCol, Color.black, 0.28f));
        for (int x = 2; x < 16; x += 5) t.SetPixel(x, seam + 1, rivet);

        // Diagonal scratches.
        for (int i = 0; i < 2; i++)
        {
            int cx = rng.Next(2, 12);
            int cy = rng.Next(2, 12);
            for (int step = 0; step < 5; step++)
            {
                int x = cx + step;
                int y = cy + step / 2;
                if (x < 16 && y < 16) t.SetPixel(x, y, Color.Lerp(light, Color.white, 0.2f));
            }
        }

        return t;
    }

    static Texture2D BuildFactoryPlatform(int seed)
    {
        System.Random rng = new System.Random(seed);
        Texture2D t = NewTex(16, 16);

        Color topLight = Hex("#B9C0C8");
        Color topMid = Hex("#8A939D");
        Color topDark = Hex("#5F6772");
        Color body = Hex("#4A515A");
        Color bodyDark = Hex("#343A42");
        Color bottom = Hex("#23272C");
        Color brace = Hex("#262B32");
        Color rivet = Hex("#A9B2BC");

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                Color c;
                if (y == 15) c = topLight;
                else if (y == 14) c = topMid;
                else if (y == 13) c = topDark;
                else if (y <= 2) c = Color.Lerp(bottom, bodyDark, y / 2f);
                else c = Color.Lerp(bodyDark, body, Mathf.Clamp01((y - 2) / 11f));
                t.SetPixel(x, y, c);
            }
        }

        for (int y = 3; y <= 13; y++)
        {
            t.SetPixel(0, y, bodyDark);
            t.SetPixel(15, y, bodyDark);
        }

        // Cross bracing.
        for (int i = 0; i < 16; i++)
        {
            int y1 = 3 + i / 2;
            int y2 = 13 - i / 2;
            if (y1 < 14) t.SetPixel(i, y1, brace);
            if (y2 > 2) t.SetPixel(i, y2, brace);
        }

        t.SetPixel(2, 12, rivet);
        t.SetPixel(13, 12, rivet);
        t.SetPixel(2, 4, rivet);
        t.SetPixel(13, 4, rivet);

        return t;
    }

    public static Sprite FactoryDepthShadeSprite()
    {
        return BuildDepthShade("depth_shade_factory", new Color(0.02f, 0.03f, 0.04f));
    }

    public static Sprite[] FactoryPlateSprites()
    {
        return new[] { FromTexture("factory_plate", BuildFactoryPlate(6451)) };
    }

    static Texture2D BuildFactoryPlate(int seed)
    {
        int w = 10;
        int h = 6;
        Texture2D t = NewTex(w, h);
        Color body = Hex("#5A626C");
        Color light = Hex("#8A939D");
        Color dark = Hex("#2E333A");

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = body;
                if (y == h - 1) c = light;
                if (x == 0 || y == 0) c = dark;
                t.SetPixel(x, y, c);
            }
        }
        t.SetPixel(2, 3, light);
        t.SetPixel(7, 3, light);

        return t;
    }

    public static Sprite[] FactoryBarrelSprites()
    {
        return new[] { FromTexture("factory_barrel", BuildBarrel(6511)) };
    }

    static Texture2D BuildBarrel(int seed)
    {
        int w = 12;
        int h = 16;
        Texture2D t = NewTex(w, h);
        Color body = Hex("#B4462E");
        Color dark = Hex("#6E2417");
        Color light = Hex("#D86A4A");
        Color band = Hex("#3A3F47");

        for (int y = 0; y <= 14; y++)
        {
            for (int x = 1; x <= 10; x++)
            {
                Color c = body;
                if (x == 1) c = dark;
                else if (x == 10) c = light;
                if (y == 3 || y == 4 || y == 11 || y == 12) c = band;
                t.SetPixel(x, y, c);
            }
        }
        for (int x = 2; x <= 9; x++) t.SetPixel(x, 15, dark);
        t.SetPixel(4, 15, Color.Lerp(body, Color.white, 0.2f));

        return t;
    }

    public static Sprite[] FactoryCrateSprites()
    {
        return new[] { FromTexture("factory_crate", BuildCrate(6501)) };
    }

    static Texture2D BuildCrate(int seed)
    {
        int w = 16;
        int h = 16;
        Texture2D t = NewTex(w, h);
        Color body = Hex("#5E6B4A");
        Color light = Hex("#7B8A61");
        Color dark = Hex("#2F3626");
        Color metal = Hex("#8A939D");

        for (int y = 1; y <= 15; y++)
        {
            for (int x = 1; x <= 14; x++)
            {
                Color c = body;
                if (y == 15) c = light;
                else if (y == 1) c = dark;
                t.SetPixel(x, y, c);
            }
        }

        for (int x = 1; x <= 14; x++)
        {
            t.SetPixel(x, 1, dark);
            t.SetPixel(x, 15, light);
        }
        for (int y = 1; y <= 15; y++)
        {
            t.SetPixel(1, y, Color.Lerp(body, dark, 0.4f));
            t.SetPixel(14, y, dark);
        }

        // X brace.
        for (int i = 2; i <= 13; i++)
        {
            t.SetPixel(i, i, Color.Lerp(body, dark, 0.5f));
            t.SetPixel(i, 16 - i, Color.Lerp(body, dark, 0.5f));
        }

        t.SetPixel(3, 3, metal);
        t.SetPixel(12, 12, metal);
        t.SetPixel(3, 12, metal);
        t.SetPixel(12, 3, metal);

        return t;
    }

    public static Sprite FactoryPipeSprite()
    {
        int w = 10;
        int h = 32;
        Texture2D t = NewTex(w, h);
        Color body = Hex("#6E7681");
        Color light = Hex("#9AA3AE");
        Color dark = Hex("#3A4048");
        Color flange = Hex("#4A515A");

        for (int y = 0; y < h; y++)
        {
            for (int x = 2; x <= 7; x++)
            {
                Color c = body;
                if (x == 2) c = light;
                else if (x == 7) c = dark;
                t.SetPixel(x, y, c);
            }
            t.SetPixel(1, y, dark);
            t.SetPixel(8, y, dark);
        }

        for (int y = 5; y <= 7; y++)
        {
            for (int x = 1; x <= 8; x++) t.SetPixel(x, y, flange);
        }
        for (int y = 24; y <= 26; y++)
        {
            for (int x = 1; x <= 8; x++) t.SetPixel(x, y, flange);
        }

        t.SetPixel(4, 6, Color.Lerp(flange, Color.white, 0.25f));

        return Import("factory_pipe", t, true, Ppu);
    }

    // ------------------------------------------------------------------ UI

    public static Sprite UiCircle(string name, int size, float feather = 2f)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = (size - 1) / 2f;
        float r = size / 2f - 1.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((r - d) / feather);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        return Import(name, t, false, 100);
    }

    public static Sprite UiRoundedRect(string name, int size, float radius, float feather = 2f)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = Mathf.Abs(x - (size - 1) / 2f) - (half - 1.5f - radius);
                float py = Mathf.Abs(y - (size - 1) / 2f) - (half - 1.5f - radius);
                float outside = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) + Mathf.Max(py, 0f) * Mathf.Max(py, 0f));
                float d = outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                float a = Mathf.Clamp01(-d / feather);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        return Import(name, t, false, 100);
    }

    public static Sprite UiRing(string name, int size, float thickness, float feather = 2f)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = (size - 1) / 2f;
        float r = size / 2f - 1.5f;
        float mid = r - thickness * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((thickness * 0.5f - Mathf.Abs(d - mid)) / feather);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        return Import(name, t, false, 100);
    }

    // Up arrow icon for the jump button (white, tinted by the UI Image).
    public static Sprite UiArrow()
    {
        string[] rows =
        {
            "......##......",
            ".....####.....",
            "....######....",
            "...########...",
            "..##########..",
            ".############.",
            "......##......",
            "......##......",
            "......##......",
            "......##......",
            "......##......"
        };
        Dictionary<char, Color> palette = new Dictionary<char, Color> { { '#', Color.white } };
        return Import("ui_arrow", FromMapRows(rows, palette), true, 100);
    }

    // Bullet icon for the fire button.
    public static Sprite UiBulletIcon()
    {
        string[] rows =
        {
            "........................",
            "...ooooooooooo..........",
            "..oyyyyyyyyyyyyo........",
            ".oyWWyyyyyyyyyyyyo......",
            ".oyWWyyyyyyyyyyyyyoo....",
            ".oyWWyyyyyyyyyyyyyyyo...",
            ".oyWWyyyyyyyyyyyyyoo....",
            "..oyyyyyyyyyyyyo........",
            "...ooooooooooo..........",
            "........................"
        };
        Dictionary<char, Color> palette = new Dictionary<char, Color>
        {
            { 'o', Hex("#7A4A18") },
            { 'y', Hex("#F2B93E") },
            { 'W', Hex("#FFF3C4") }
        };
        return Import("ui_bullet", FromMapRows(rows, palette), true, 100);
    }

    // Flat colour used behind the level; shows through pit shafts so they read as dark holes.
    public static Sprite SolidSprite(string name, Color color)
    {
        const int size = 8;
        Texture2D t = NewTex(size, size);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                t.SetPixel(x, y, color);
        return Import(name, t, true, Ppu);
    }

    // Vertical darkening laid over the underground strip so the dirt fades towards the bottom.
    public static Sprite DepthShadeSprite()
    {
        return BuildDepthShade("depth_shade", new Color(0.05f, 0.03f, 0.02f));
    }

    public static Sprite SnowDepthShadeSprite()
    {
        return BuildDepthShade("depth_shade_snow", new Color(0.02f, 0.05f, 0.10f));
    }

    public static Sprite DesertDepthShadeSprite()
    {
        return BuildDepthShade("depth_shade_desert", new Color(0.10f, 0.06f, 0.03f));
    }

    static Sprite BuildDepthShade(string name, Color tint)
    {
        int w = 32;
        int h = 64;
        Texture2D t = NewTex(w, h);
        for (int y = 0; y < h; y++)
        {
            float k = 1f - y / (h - 1f); // 1 at the bottom row, 0 at the top row
            float a = 0.40f * k * k;
            Color c = new Color(tint.r, tint.g, tint.b, a);
            for (int x = 0; x < w; x++) t.SetPixel(x, y, c);
        }
        return Import(name, t, true, Ppu);
    }

    // End-of-stage flag: grey pole with a red pennant pointing right.
    public static Sprite GoalFlagSprite()
    {
        int w = 16;
        int h = 32;
        Texture2D t = NewTex(w, h);

        Color pole = Hex("#B7BFC8");
        Color poleDark = Hex("#5E6670");
        Color flag = Hex("#E8452C");
        Color flagDark = Hex("#A82A18");
        Color flagLight = Hex("#FF6A4E");

        for (int y = 0; y < h; y++)
        {
            t.SetPixel(3, y, poleDark);
            t.SetPixel(4, y, pole);
        }
        t.SetPixel(2, h - 1, poleDark);
        t.SetPixel(5, h - 1, poleDark);
        for (int x = 2; x <= 5; x++) t.SetPixel(x, 0, poleDark);

        // Pennant: full width at the top, tapering down to the pole.
        for (int y = 14; y <= 30; y++)
        {
            int len = 4 + Mathf.RoundToInt(11f * (y - 14) / 16f);
            for (int x = 5; x <= len; x++)
            {
                Color c = flag;
                if (y == 14) c = flagDark;
                else if (x == len) c = flagDark;
                else if (y >= 28) c = flagLight;
                t.SetPixel(x, y, c);
            }
        }

        return Import("goal_flag", t, true, Ppu);
    }

    // Round soft flake for the snow particle field.
    public static Sprite SnowflakeSprite()
    {
        string[] rows =
        {
            ".WW.",
            "WWWW",
            "WWWW",
            ".WW."
        };
        Dictionary<char, Color> palette = new Dictionary<char, Color>
        {
            { 'W', new Color(1f, 1f, 1f, 0.85f) }
        };
        return Import("snowflake", FromMapRows(rows, palette), false, Ppu);
    }

    // Soft sand-coloured mote for the desert dust field.
    public static Sprite DustSprite()
    {
        string[] rows =
        {
            ".yy.",
            "yyyy",
            "yyyy",
            ".yy."
        };
        Dictionary<char, Color> palette = new Dictionary<char, Color>
        {
            { 'y', new Color(0.93f, 0.80f, 0.55f, 0.75f) }
        };
        return Import("dust", FromMapRows(rows, palette), false, Ppu);
    }

    // ------------------------------------------------------------------ level previews

    // Thumbnail card art for the level select menu: sky, hills, terrain and characters.
    public static Sprite LevelPreviewSprite(int level)
    {
        const int w = 384;
        const int h = 216;
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);

        Color horizon, low, mid, top, hillFar, hillNear;
        Texture2D groundTile, fillTile, platformTile, enemyTex, playerTex, decoTex;
        bool mesa;

        switch (level)
        {
            case 2:
                horizon = Hex("#F3E9EC");
                low = Hex("#E2ECF5");
                mid = Hex("#A9C8E4");
                top = Hex("#4C7CB3");
                hillFar = Hex("#C3D6E8");
                hillNear = Hex("#9FBAD3");
                groundTile = BuildSnowGroundTop(1202);
                fillTile = BuildSnowFill(2202);
                platformTile = BuildIcePlatform(3201);
                enemyTex = FromMapRows(Combine(EnemyUpper, EnemyLegsA), SnowEnemyPalette());
                playerTex = FromMapRows(Combine(PlayerUpper, PlayerLegsIdle), PlayerPalette());
                decoTex = BuildSnowBush(6202);
                mesa = false;
                break;

            case 3:
                horizon = Hex("#FBE7C2");
                low = Hex("#F4DDBE");
                mid = Hex("#A9C8DE");
                top = Hex("#3F79B4");
                hillFar = Hex("#D6BD9C");
                hillNear = Hex("#B8956D");
                groundTile = BuildSandGroundTop(1402);
                fillTile = BuildSandFill(2402);
                platformTile = BuildSandstonePlatform(3401);
                enemyTex = FromMapRows(Combine(EnemyUpper, EnemyLegsA), DesertEnemyPalette());
                playerTex = FromMapRows(Combine(PlayerUpper, PlayerLegsIdle), PlayerPalette());
                decoTex = BuildDryBush(6302);
                mesa = true;
                break;

            case 4:
                horizon = Hex("#332A22");
                low = Hex("#241D18");
                mid = Hex("#171310");
                top = Hex("#0B0908");
                hillFar = Hex("#3C332A");
                hillNear = Hex("#2B241E");
                groundTile = BuildCaveGroundTop(1602);
                fillTile = BuildCaveFill(2602);
                platformTile = BuildMinePlatform(3601);
                enemyTex = FromMapRows(Combine(EnemyUpper, EnemyLegsA), CaveEnemyPalette());
                playerTex = FromMapRows(Combine(PlayerUpper, PlayerLegsIdle), PlayerPalette());
                decoTex = BuildCrystalCluster(6411);
                mesa = false;
                break;

            case 5:
                horizon = Hex("#2E3742");
                low = Hex("#232B35");
                mid = Hex("#191F27");
                top = Hex("#0E1116");
                hillFar = Hex("#39424D");
                hillNear = Hex("#252B33");
                groundTile = BuildFactoryGroundTop(1802);
                fillTile = BuildFactoryFill(2802);
                platformTile = BuildFactoryPlatform(3801);
                enemyTex = FromMapRows(Combine(EnemyUpper, EnemyLegsA), FactoryEnemyPalette());
                playerTex = FromMapRows(Combine(PlayerUpper, PlayerLegsIdle), PlayerPalette());
                decoTex = BuildCrate(6502);
                mesa = true;
                break;

            default:
                horizon = Hex("#F2E3BE");
                low = Hex("#DCE9EA");
                mid = Hex("#8FBFE4");
                top = Hex("#2E63B2");
                hillFar = Hex("#9DB8D0");
                hillNear = Hex("#7897B5");
                groundTile = BuildGroundTop(1102);
                fillTile = BuildDirtFill(2102);
                platformTile = BuildPlatform(3101);
                enemyTex = FromMapRows(Combine(EnemyUpper, EnemyLegsA), EnemyPalette());
                playerTex = FromMapRows(Combine(PlayerUpper, PlayerLegsIdle), PlayerPalette());
                decoTex = BuildBush(4102);
                mesa = false;
                break;
        }

        int surfaceY = 44;
        const int scale = 2;

        // Sky gradient above the ground line.
        for (int y = surfaceY; y < h; y++)
        {
            float tt = (y - surfaceY) / (float)(h - surfaceY);
            Color c;
            if (tt < 0.25f) c = Color.Lerp(horizon, low, tt / 0.25f);
            else if (tt < 0.6f) c = Color.Lerp(low, mid, (tt - 0.25f) / 0.35f);
            else c = Color.Lerp(mid, top, (tt - 0.6f) / 0.4f);
            for (int x = 0; x < w; x++) t.SetPixel(x, y, c);
        }

        // Two parallax-like hill layers; deserts use flat-topped mesas instead.
        for (int x = 0; x < w; x++)
        {
            float nf = mesa
                ? Mathf.Floor(Noise(x * 2.2f, 90f, 31) * 4f) / 3f
                : Noise(x * 2.2f, 120f, 31) * 0.7f + Noise(x * 2.2f, 46f, 32) * 0.3f;
            int farTop = surfaceY + 30 + Mathf.RoundToInt(nf * 46f);

            float nn = mesa
                ? Mathf.Floor(Noise(x * 1.7f, 110f, 41) * 3f) / 2f
                : Noise(x * 1.7f, 90f, 41) * 0.7f + Noise(x * 1.7f, 34f, 42) * 0.3f;
            int nearTop = surfaceY + 12 + Mathf.RoundToInt(nn * 40f);

            int farLimit = Mathf.Min(farTop, h - 1);
            for (int y = surfaceY; y <= farLimit; y++) t.SetPixel(x, y, hillFar);

            int nearLimit = Mathf.Min(nearTop, h - 1);
            for (int y = surfaceY; y <= nearLimit; y++) t.SetPixel(x, y, hillNear);
            t.SetPixel(x, nearLimit, Color.Lerp(hillNear, Color.white, 0.18f));
        }

        // Underground fill first, then the decorated ground-top row over it.
        for (int gx = -16 * scale; gx < w; gx += 16 * scale)
        {
            BlitScaled(t, fillTile, gx, 0, scale);
        }
        for (int gx = -16 * scale; gx < w; gx += 16 * scale)
        {
            BlitScaled(t, groundTile, gx, surfaceY - 16 * scale, scale);
        }

        // A small floating ledge on the right with an enemy standing on it.
        BlitScaled(t, platformTile, 248, surfaceY + 18, scale);
        BlitScaled(t, platformTile, 280, surfaceY + 18, scale);

        BlitScaled(t, playerTex, 56, surfaceY, 3);
        BlitScaled(t, decoTex, 150, surfaceY, 3);
        BlitScaled(t, enemyTex, 286, surfaceY + 50, 3);

        // Soft vignette so the card reads as a framed scene.
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = x / (w - 1f) * 2f - 1f;
                float v = y / (h - 1f) * 2f - 1f;
                float d = Mathf.Sqrt(u * u * 0.6f + v * v * 0.8f);
                float k = Mathf.Clamp01((d - 0.75f) / 0.55f);
                if (k > 0f)
                {
                    Color c = t.GetPixel(x, y);
                    c = Color.Lerp(c, Color.black, 0.32f * k);
                    c.a = 1f;
                    t.SetPixel(x, y, c);
                }
            }
        }

        UnityEngine.Object.DestroyImmediate(groundTile);
        UnityEngine.Object.DestroyImmediate(fillTile);
        UnityEngine.Object.DestroyImmediate(platformTile);
        UnityEngine.Object.DestroyImmediate(enemyTex);
        UnityEngine.Object.DestroyImmediate(playerTex);
        UnityEngine.Object.DestroyImmediate(decoTex);

        return Import("level_preview" + level, t, false, 100);
    }

    // Nearest-neighbour scaled blit used by the level thumbnails.
    static void BlitScaled(Texture2D dst, Texture2D src, int ox, int oy, int scale)
    {
        int dw = src.width * scale;
        int dh = src.height * scale;

        for (int y = 0; y < dh; y++)
        {
            int sy = y / scale;
            int dy = oy + y;
            if (dy < 0 || dy >= dst.height) continue;

            for (int x = 0; x < dw; x++)
            {
                int dx = ox + x;
                if (dx < 0 || dx >= dst.width) continue;

                Color c = src.GetPixel(x / scale, sy);
                if (c.a <= 0f) continue;
                Color under = dst.GetPixel(dx, dy);
                dst.SetPixel(dx, dy, Color.Lerp(under, c, c.a));
            }
        }
    }

    // ------------------------------------------------------------------ preview

    public static void WritePreview()
    {
        const int maxWidth = 1500;
        const int pad = 8;

        List<KeyValuePair<Texture2D, float>> items = new List<KeyValuePair<Texture2D, float>>();
        List<Texture2D> decoded = new List<Texture2D>();
        foreach (KeyValuePair<string, byte[]> entry in Generated)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(entry.Value);
            decoded.Add(tex);
            float scale;
            if (entry.Key.StartsWith("ui_") || entry.Key == "vignette") scale = 2f;
            else if (tex.width >= 1024) scale = 0.7f;
            else if (tex.width >= 128) scale = 3f;
            else scale = 6f;
            scale = Mathf.Min(scale, (maxWidth - 2f * pad) / tex.width);
            items.Add(new KeyValuePair<Texture2D, float>(tex, scale));
        }

        List<Vector2Int> positions = new List<Vector2Int>();
        List<Vector2Int> sizes = new List<Vector2Int>();
        int cursorX = pad;
        int cursorY = pad;
        int rowHeight = 0;

        foreach (KeyValuePair<Texture2D, float> item in items)
        {
            int sw = Mathf.Max(1, Mathf.RoundToInt(item.Key.width * item.Value));
            int sh = Mathf.Max(1, Mathf.RoundToInt(item.Key.height * item.Value));
            if (cursorX + sw + pad > maxWidth)
            {
                cursorX = pad;
                cursorY += rowHeight + pad;
                rowHeight = 0;
            }
            positions.Add(new Vector2Int(cursorX, cursorY));
            sizes.Add(new Vector2Int(sw, sh));
            cursorX += sw + pad;
            rowHeight = Mathf.Max(rowHeight, sh);
        }

        int totalHeight = Mathf.Max(cursorY + rowHeight + pad, 64);
        Texture2D canvas = new Texture2D(maxWidth, totalHeight, TextureFormat.RGBA32, false);
        Color bg = new Color(0.16f, 0.17f, 0.2f, 1f);
        for (int y = 0; y < canvas.height; y++)
        {
            for (int x = 0; x < canvas.width; x++) canvas.SetPixel(x, y, bg);
        }

        for (int i = 0; i < items.Count; i++)
        {
            int topDownY = positions[i].y;
            int oy = canvas.height - topDownY - sizes[i].y;
            Blit(canvas, items[i].Key, positions[i].x, oy, items[i].Value);
        }

        canvas.Apply();
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/art_preview.png"));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, canvas.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(canvas);
        foreach (Texture2D tex in decoded) UnityEngine.Object.DestroyImmediate(tex);
        Debug.Log("[PixelArt] preview written to " + path);
        Generated.Clear();
    }

    static void Blit(Texture2D dst, Texture2D src, int ox, int oy, float scale)
    {
        int dw = Mathf.Max(1, Mathf.RoundToInt(src.width * scale));
        int dh = Mathf.Max(1, Mathf.RoundToInt(src.height * scale));
        for (int y = 0; y < dh; y++)
        {
            int sy = Mathf.Min(src.height - 1, Mathf.FloorToInt(y / scale));
            int dy = oy + y;
            if (dy < 0 || dy >= dst.height) continue;
            for (int x = 0; x < dw; x++)
            {
                int sx = Mathf.Min(src.width - 1, Mathf.FloorToInt(x / scale));
                int dx = ox + x;
                if (dx < 0 || dx >= dst.width) continue;
                Color c = src.GetPixel(sx, sy);
                if (c.a <= 0f) continue;
                Color under = dst.GetPixel(dx, dy);
                dst.SetPixel(dx, dy, Color.Lerp(under, c, c.a));
            }
        }
    }

    // ------------------------------------------------------------------ helpers

    static string[] Combine(string[] upper, string[] lower)
    {
        string[] rows = new string[upper.Length + lower.Length];
        Array.Copy(upper, 0, rows, 0, upper.Length);
        Array.Copy(lower, 0, rows, upper.Length, lower.Length);
        return rows;
    }

    static Sprite FromMap(string name, string[] rows, Dictionary<char, Color> palette)
    {
        int height = rows.Length;
        int width = rows[0].Length;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int row = 0; row < height; row++)
        {
            if (rows[row].Length != width)
            {
                throw new Exception("PixelArt " + name + ": row " + row + " has " + rows[row].Length + " chars, expected " + width);
            }
            for (int x = 0; x < width; x++)
            {
                char ch = rows[row][x];
                Color c = ch == '.' ? Color.clear : palette[ch];
                tex.SetPixel(x, height - 1 - row, c);
            }
        }
        tex.Apply();
        return Import(name, tex, true, Ppu);
    }

    static Texture2D FromMapRows(string[] rows, Dictionary<char, Color> palette)
    {
        int height = rows.Length;
        int width = rows[0].Length;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int row = 0; row < height; row++)
        {
            if (rows[row].Length != width)
            {
                throw new Exception("PixelArt: row " + row + " has " + rows[row].Length + " chars, expected " + width);
            }
            for (int x = 0; x < width; x++)
            {
                char ch = rows[row][x];
                tex.SetPixel(x, height - 1 - row, ch == '.' ? Color.clear : palette[ch]);
            }
        }
        tex.Apply();
        return tex;
    }

    static Sprite FromTexture(string name, Texture2D tex)
    {
        return Import(name, tex, true, Ppu);
    }

    static Sprite Import(string name, Texture2D tex, bool point, int ppu)
    {
        byte[] png = tex.EncodeToPNG();
        Generated.Add(new KeyValuePair<string, byte[]>(name, png));
        UnityEngine.Object.DestroyImmediate(tex);

        Directory.CreateDirectory(Dir);
        string path = Dir + "/" + name + ".png";
        File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        // Unity defaults to 2048; the wide background strips are 4096 so they must not be downscaled.
        importer.maxTextureSize = 8192;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Texture2D NewTex(int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        // New textures hold uninitialised pixels; clear them so untouched pixels stay transparent.
        t.SetPixels32(new Color32[w * h]);
        return t;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    static float Rand(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    static float Hash(int n, int seed)
    {
        unchecked
        {
            uint h = (uint)n * 374761393u + (uint)seed * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFFu) / 16777215f;
        }
    }

    static float Noise(float x, float period, int seed)
    {
        float p = x / period;
        int i = Mathf.FloorToInt(p);
        float f = p - i;
        float u = f * f * (3f - 2f * f);
        return Mathf.Lerp(Hash(i, seed), Hash(i + 1, seed), u);
    }

    // Sharp-crested ridge noise: 0 in valleys, 1 on peaks.
    static float Ridged(float x, float period, int seed)
    {
        float n = Noise(x, period, seed);
        float r = 1f - Mathf.Abs(2f * n - 1f);
        return r * r;
    }
}
