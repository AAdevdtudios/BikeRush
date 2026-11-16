using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ShiftRush.EditorTools
{
    /// <summary>
    /// The "trail zine" look: three free fonts (Black Ops One stencil, Rubik Dirt distressed, Permanent Marker
    /// scrawl; licences beside them in Art/Fonts/Rough) turned into TextMeshPro assets, and rough shapes drawn in
    /// code (torn paper, duct tape, a bolted race plate, a die-cut sticker disc, a sprayed ring, a paint blob,
    /// mud splatter, notebook lines). No paid or AI art; everything here regenerates from this file.
    /// </summary>
    public static partial class MenuBuilder
    {
        const string RoughFonts = "Assets/_ShiftRush/Art/Fonts/Rough/";
        static TMP_FontAsset fontStencil, fontDirt, fontMarker, fontNumber;
        static Material dirtOutline;
        static Sprite roughPaper, roughTape, roughPlate, roughDisc, roughRing, roughBlob, roughMud, roughLines;

        static readonly Color PaperCol = new Color32(239, 231, 213, 255);
        static readonly Color KraftCol = new Color32(201, 162, 116, 255);
        static readonly Color TapeCol = new Color32(191, 188, 179, 235);
        static readonly Color InkCol = new Color32(21, 18, 26, 255);
        static readonly Color PencilCol = new Color32(92, 84, 74, 255);
        static readonly Color MudCol = new Color32(75, 52, 36, 235);
        static readonly Color DeepMagenta = new Color32(176, 18, 127, 255);

        static void LoadRoughFonts()
        {
            fontStencil = RoughFont("BlackOpsOne-Regular");
            fontDirt = RoughFont("RubikDirt-Regular");
            fontMarker = RoughFont("PermanentMarker-Regular");
            // The stencil face gets an ink outline for the countdown, so it reads over bright sky and dark pines
            // alike. (The distressed face doesn't take an outline well: its worn holes fill in.)
            string mp = RoughFonts + "BlackOpsOne Outline.mat";
            dirtOutline = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (dirtOutline == null && fontStencil != null)
            {
                dirtOutline = new Material(fontStencil.material) { name = "BlackOpsOne Outline" };
                AssetDatabase.CreateAsset(dirtOutline, mp);
            }
            if (dirtOutline != null)
            {
                // Follow the font's live material (its atlas texture and sizes), then add the outline.
                if (fontStencil != null) { dirtOutline.shader = fontStencil.material.shader; dirtOutline.CopyPropertiesFromMaterial(fontStencil.material); }
                dirtOutline.EnableKeyword("OUTLINE_ON");
                dirtOutline.SetFloat("_OutlineWidth", 0.16f);
                dirtOutline.SetColor("_OutlineColor", InkCol);
                dirtOutline.SetFloat("_FaceDilate", 0.08f);
                EditorUtility.SetDirty(dirtOutline);
            }
        }

        /// <summary>A TMP font asset next to the TTF, made once; its atlas is filled with ASCII up front.</summary>
        static TMP_FontAsset RoughFont(string file)
        {
            string path = RoughFonts + file + " SDF.asset";
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fa != null) return fa;
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(RoughFonts + file + ".ttf");
            if (ttf == null) { Debug.LogWarning("[BIKE//RUSH] Missing font " + file); return null; }
            fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            fa.name = file + " SDF";
            const string ascii = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
            fa.TryAddCharacters(ascii);
            AssetDatabase.CreateAsset(fa, path);
            fa.atlasTextures[0].name = file + " Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = file + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        static float Edge(float t, float seed, float amp)
        {
            return amp * (0.35f + 0.65f * Mathf.PerlinNoise(t * 0.07f + seed, seed * 3.1f) + 0.35f * Mathf.PerlinNoise(t * 0.31f + seed * 2f, 1.7f));
        }

        static void MakeRoughArt()
        {
            // Torn paper: every edge ragged, sliced so the rag survives stretching.
            roughPaper = SaveSprite("T_RoughPaper", 256, 192, (x, y, w, h) =>
            {
                float d = Mathf.Min(x - Edge(y, 1f, 9f), w - x - Edge(y, 5f, 9f), y - Edge(x, 9f, 12f), h - y - Edge(x, 13f, 12f));
                return Mathf.Clamp01(d + 0.5f);
            }, new Vector4(40, 40, 40, 40));

            // Duct tape: zig-zag torn ends, faint fibres along its length.
            roughTape = SaveSprite("T_RoughTape", 160, 48, (x, y, w, h) =>
            {
                float tri = Mathf.Abs(Mathf.Repeat(y / 7f, 2f) - 1f);
                float left = 3f + 7f * tri + 2f * Mathf.PerlinNoise(y * 0.4f, 2f);
                float right = w - 3f - 7f * Mathf.Abs(Mathf.Repeat(y / 6f + 0.4f, 2f) - 1f) - 2f * Mathf.PerlinNoise(y * 0.4f, 7f);
                float d = Mathf.Min(x - left, right - x, y - 1f, h - 1f - y);
                float fibre = 0.86f + 0.14f * Mathf.PerlinNoise(x * 0.05f, y * 0.9f);
                return Mathf.Clamp01(d + 0.5f) * fibre;
            }, new Vector4(22, 0, 22, 0));

            // A race number plate: rounded, two bolt holes along the top.
            roughPlate = SaveSprite("T_RoughPlate", 256, 192, (x, y, w, h) =>
            {
                const float r = 34f;
                float cx = Mathf.Clamp(x, r, w - r), cy = Mathf.Clamp(y, r, h - r);
                float d = r - Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                float a = Mathf.Clamp01(d + 0.5f);
                float hole = Mathf.Min(Vector2.Distance(new Vector2(x, y), new Vector2(30, h - 26)), Vector2.Distance(new Vector2(x, y), new Vector2(w - 30, h - 26)));
                return a * Mathf.Clamp01(hole - 8.5f);
            }, new Vector4(56, 56, 56, 56));

            // A die-cut sticker: round, the cut a little uneven.
            roughDisc = SaveSprite("T_RoughDisc", 256, 256, (x, y, w, h) =>
            {
                float ang = Mathf.Atan2(y - h * 0.5f, x - w * 0.5f);
                float r = 122f - 2.5f * Mathf.PerlinNoise(ang * 3f + 4f, 1f);
                return Mathf.Clamp01(r - Vector2.Distance(new Vector2(x, y), new Vector2(w, h) * 0.5f) + 0.5f);
            }, Vector4.zero);

            // A sprayed ring: soft overspray at the edges, patchy where the can ran dry.
            roughRing = SaveSprite("T_RoughRing", 256, 256, (x, y, w, h) =>
            {
                float ang = Mathf.Atan2(y - h * 0.5f, x - w * 0.5f);
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(w, h) * 0.5f);
                float wob = 3f * Mathf.PerlinNoise(ang * 4f + 2f, 3f);
                float core = Mathf.Min(d - (100f + wob), (120f - wob) - d);
                float body = Mathf.Clamp01(core / 3f + 0.5f);
                float spray = Mathf.Clamp01(1f - Mathf.Abs(d - 110f) / 16f) * 0.25f;
                float dry = 0.7f + 0.3f * Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                return Mathf.Max(body, spray) * dry;
            }, Vector4.zero);

            // A splodge of paint.
            roughBlob = SaveSprite("T_RoughBlob", 128, 128, (x, y, w, h) =>
            {
                float ang = Mathf.Atan2(y - h * 0.5f, x - w * 0.5f);
                float r = 50f + 9f * Mathf.PerlinNoise(Mathf.Cos(ang) * 1.6f + 3f, Mathf.Sin(ang) * 1.6f + 3f) - 4f;
                return Mathf.Clamp01(r - Vector2.Distance(new Vector2(x, y), new Vector2(w, h) * 0.5f) + 0.5f);
            }, Vector4.zero);

            // Mud flicked up from the bottom-left corner (mirrored for the right).
            var rng = new System.Random(23);
            var blobs = new List<Vector3>();
            for (int i = 0; i < 150; i++)
            {
                float dist = Mathf.Pow((float)rng.NextDouble(), 1.6f) * 470f;
                float ang = (float)rng.NextDouble() * 80f * Mathf.Deg2Rad;
                float rad = Mathf.Max(1.5f, (1f - dist / 470f) * 22f * (0.35f + (float)rng.NextDouble()));
                blobs.Add(new Vector3(Mathf.Cos(ang) * dist, Mathf.Sin(ang) * dist * 0.75f, rad));
            }
            roughMud = SaveSprite("T_RoughMud", 512, 384, (x, y, w, h) =>
            {
                float a = 0f;
                foreach (var b in blobs)
                {
                    float dx = x - b.x, dy = y - b.y;
                    if (Mathf.Abs(dx) > b.z + 2f || Mathf.Abs(dy) > b.z + 2f) continue;
                    a = Mathf.Max(a, Mathf.Clamp01(b.z - Mathf.Sqrt(dx * dx * 0.7f + dy * dy) + 0.5f));
                }
                return a;
            }, Vector4.zero);

            // One ruled line, tiled down the notebook leaderboard.
            roughLines = SaveSprite("T_RoughLines", 16, 36, (x, y, w, h) => y < 2f ? 1f : 0f, Vector4.zero, TextureWrapMode.Repeat);
        }

        // ---- building blocks

        static UnityEngine.UI.Image Paper(Transform parent, string name, Color c, Vector2 anchor, Vector2 pos, Vector2 size, float tilt)
        {
            var img = Img(parent, name, roughPaper, c);
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.4f;
            Place(img.rectTransform, anchor, anchor, anchor, pos, size);
            img.rectTransform.localEulerAngles = new Vector3(0, 0, tilt);
            return img;
        }

        static UnityEngine.UI.Image Tape(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, float tilt, Color? tint = null)
        {
            var img = Img(parent, name, roughTape, tint ?? TapeCol);
            img.type = UnityEngine.UI.Image.Type.Sliced;
            Place(img.rectTransform, anchor, anchor, new Vector2(0.5f, 0.5f), pos, size);
            img.rectTransform.localEulerAngles = new Vector3(0, 0, tilt);
            return img;
        }

        static TextMeshProUGUI Scrawl(Transform parent, string name, string text, float size, Color c, TextAlignmentOptions align)
        {
            var t = Txt(parent, name, text, fontMarker != null ? fontMarker : fontBold, size, c, align);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }
    }
}
