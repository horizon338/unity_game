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

    public static Sprite[] PlayerFrames()
    {
        Dictionary<char, Color> palette = PlayerPalette();
        return new[]
        {
            FromMap("player_idle", Combine(PlayerUpper, PlayerLegsIdle), palette),
            FromMap("player_run1", Combine(PlayerUpper, PlayerLegsRunA), palette),
            FromMap("player_run2", Combine(PlayerUpper, PlayerLegsRunB), palette)
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
        int w = 8;
        int h = 256;
        Texture2D t = NewTex(w, h);

        Color horizon = Hex("#F2E3BE");
        Color low = Hex("#DCE9EA");
        Color mid = Hex("#8FBFE4");
        Color top = Hex("#2E63B2");

        for (int y = 0; y < h; y++)
        {
            float tt = y / (float)(h - 1);
            Color c;
            if (tt < 0.22f) c = Color.Lerp(horizon, low, tt / 0.22f);
            else if (tt < 0.55f) c = Color.Lerp(low, mid, (tt - 0.22f) / 0.33f);
            else c = Color.Lerp(mid, top, (tt - 0.55f) / 0.45f);
            for (int x = 0; x < w; x++) t.SetPixel(x, y, c);
        }

        return Import("sky", t, false, Ppu);
    }

    public static Sprite MountainSprite()
    {
        int w = 4096;
        int h = 128;
        Texture2D t = NewTex(w, h);

        Color haze = Hex("#AEC6D9");
        Color far = Hex("#7E9BBC");
        Color near = Hex("#5E7C9A");

        float[] ridgeFar = new float[w];
        float[] ridgeNear = new float[w];
        for (int x = 0; x < w; x++)
        {
            float nf = 0.55f * Ridged(x, 340f, 11) + 0.30f * Ridged(x, 130f, 12) + 0.15f * Ridged(x, 48f, 13);
            ridgeFar[x] = 42f + nf * 74f;
            float nn = 0.55f * Ridged(x, 280f, 21) + 0.30f * Ridged(x, 100f, 22) + 0.15f * Ridged(x, 36f, 23);
            ridgeNear[x] = 16f + nn * 62f;
        }

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Color c = Color.clear;
                if (y <= ridgeFar[x])
                {
                    c = Color.Lerp(haze, far, Mathf.Clamp01(y / Mathf.Max(1f, ridgeFar[x]) * 1.15f));
                    if (Mathf.Abs(y - ridgeFar[x]) < 1.5f) c = Color.Lerp(c, Color.white, 0.18f);
                }
                if (y <= ridgeNear[x])
                {
                    c = Color.Lerp(haze, near, Mathf.Clamp01(y / Mathf.Max(1f, ridgeNear[x]) * 1.15f));
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

        return Import("mountains", t, true, Ppu);
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
        int w = 32;
        int h = 64;
        Texture2D t = NewTex(w, h);
        for (int y = 0; y < h; y++)
        {
            float k = 1f - y / (h - 1f); // 1 at the bottom row, 0 at the top row
            float a = 0.40f * k * k;
            Color c = new Color(0.05f, 0.03f, 0.02f, a);
            for (int x = 0; x < w; x++) t.SetPixel(x, y, c);
        }
        return Import("depth_shade", t, true, Ppu);
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
