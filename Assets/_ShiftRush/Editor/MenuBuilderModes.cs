using ShiftRush.Core;
using ShiftRush.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftRush.EditorTools
{
    /// <summary>
    /// Start Gate: the ride-mode screen between Tune and Trails. Three tall slanted cards (Air Show, Ghost Run,
    /// Pack Race), each with its own colour and a little icon drawn in code.
    /// </summary>
    public static partial class MenuBuilder
    {
        static Animator BuildModes(Transform canvas, MenuRouter router, Showroom showroom)
        {
            // Another copy of the garage's source panel, for the same show/hide animation (built before the garage
            // repurposes it).
            var src = canvas.Find("Main Content/Shop");
            var panel = Object.Instantiate(src.gameObject, src.parent).transform;
            panel.SetSiblingIndex(src.GetSiblingIndex() + 2);
            panel.name = "Shop Gate";
            panel = RepurposePanel(canvas, "Shop Gate", MenuRouter.Modes);
            var content = panel.Find("Content");
            Header(content, "START GATE", "STEP 3 / 4   HOW DO YOU WANT TO RIDE");

            var icons = new[]
            {
                SaveSprite("T_Mode_AirShow", 128, 128, AirShowIcon, Vector4.zero),
                SaveSprite("T_Mode_GhostRun", 128, 128, GhostRunIcon, Vector4.zero),
                SaveSprite("T_Mode_PackRace", 128, 128, PackRaceIcon, Vector4.zero),
            };

            var cards = new ModeSelectPanel.Card[RideModes.All.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                var m = RideModes.All[i];
                var fill = Img(content, "Card " + m.name, slant, new Color(0.04f, 0.04f, 0.07f, 0.8f));
                fill.type = Image.Type.Sliced;
                var root = fill.rectTransform;
                Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-480 + i * 480, -70), new Vector2(450, 610));

                var edge = Img(root, "Edge", slantFrame, new Color(1, 1, 1, 0.16f));
                edge.type = Image.Type.Sliced; edge.raycastTarget = false; edge.pixelsPerUnitMultiplier = 2f;
                Stretch(edge.rectTransform);

                var index = Txt(root, "Index", "0" + (i + 1), fontTitle, 190, new Color(1, 1, 1, 0.08f), TextAlignmentOptions.TopRight);
                index.raycastTarget = false;
                Place(index.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, 10), new Vector2(260, 220));

                var icon = Img(root, "Icon", icons[i], Color.white);
                icon.raycastTarget = false;
                Place(icon.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(54, -52), new Vector2(128, 128));

                var stripe = Img(root, "Stripe", slant, m.accent);
                stripe.type = Image.Type.Sliced; stripe.raycastTarget = false;
                Place(stripe.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -222), new Vector2(90, 10));

                var title = Txt(root, "Title", m.name, fontTitle, 64, Color.white, TextAlignmentOptions.Left);
                title.raycastTarget = false;
                Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -238), new Vector2(380, 80));

                var tag = Txt(root, "Tag", m.tag, fontBold, 17, m.accent, TextAlignmentOptions.Left);
                tag.characterSpacing = 6; tag.raycastTarget = false;
                Place(tag.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(46, -318), new Vector2(380, 30));

                var desc = Txt(root, "Description", m.description, fontRegular, 23, Steel, TextAlignmentOptions.TopLeft);
                desc.raycastTarget = false;
                Place(desc.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -366), new Vector2(350, 160));

                var badge = Txt(root, "Badge", m.ready ? "" : m.badge, fontBold, 18, m.accent, TextAlignmentOptions.Left);
                badge.characterSpacing = 12; badge.raycastTarget = false;
                Place(badge.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 34), new Vector2(380, 30));

                cards[i] = new ModeSelectPanel.Card { root = root, fill = fill, edge = edge, icon = icon, stripe = stripe, index = index, title = title, tag = tag, description = desc, badge = badge };
            }

            var mp = panel.gameObject.AddComponent<ModeSelectPanel>();
            var so = new SerializedObject(mp);
            so.FindProperty("visibleGroup").objectReferenceValue = panel.GetComponent<CanvasGroup>();
            so.FindProperty("showroom").objectReferenceValue = showroom;
            so.FindProperty("steel").colorValue = Steel;
            var cp = so.FindProperty("cards");
            cp.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                var e = cp.GetArrayElementAtIndex(i);
                var c = cards[i];
                e.FindPropertyRelative("root").objectReferenceValue = c.root;
                e.FindPropertyRelative("fill").objectReferenceValue = c.fill;
                e.FindPropertyRelative("edge").objectReferenceValue = c.edge;
                e.FindPropertyRelative("icon").objectReferenceValue = c.icon;
                e.FindPropertyRelative("stripe").objectReferenceValue = c.stripe;
                e.FindPropertyRelative("index").objectReferenceValue = c.index;
                e.FindPropertyRelative("title").objectReferenceValue = c.title;
                e.FindPropertyRelative("tag").objectReferenceValue = c.tag;
                e.FindPropertyRelative("description").objectReferenceValue = c.description;
                e.FindPropertyRelative("badge").objectReferenceValue = c.badge;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddVoidPersistentListener(mp.onConfirm, router.OpenTracks);

            CloneHotkeys(canvas.Find("Main Content/Home Panel"), content, router, "Back", router.Back);
            return panel.GetComponent<Animator>();
        }

        // ---- icons, white on clear (tinted per card). y runs up.

        static float Line(float px, float py, float ax, float ay, float bx, float by, float thick)
        {
            float dx = bx - ax, dy = by - ay;
            float t = Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy));
            float d = Mathf.Sqrt((px - ax - t * dx) * (px - ax - t * dx) + (py - ay - t * dy) * (py - ay - t * dy));
            return Mathf.Clamp01(thick * 0.5f - d + 0.5f);
        }

        /// <summary>A kicker ramp and the dashed arc of a jump off it, landing far right.</summary>
        static float AirShowIcon(float x, float y, float w, float h)
        {
            float a = 0f;
            // Ramp: a filled wedge bottom-left.
            if (x > 8 && x < 46 && y > 14 && y < 14 + (x - 8) * 0.75f) a = 1f;
            a = Mathf.Max(a, Line(x, y, 6, 14, 122, 14, 5));
            // Dashed arc from the ramp lip to the landing.
            float bestD = 99f, bestT = 0f;
            for (int i = 0; i <= 60; i++)
            {
                float t = i / 60f;
                float ax = 44 + t * 78f, ay = 40 + 46f * 4f * t * (1 - t) - t * 18f;
                float d = Mathf.Sqrt((x - ax) * (x - ax) + (y - ay) * (y - ay));
                if (d < bestD) { bestD = d; bestT = t; }
            }
            if (Mathf.Repeat(bestT * 9f, 1f) < 0.6f) a = Mathf.Max(a, Mathf.Clamp01(3f - bestD + 0.5f));
            // The rider at the top: a small diamond.
            float rx = Mathf.Abs(x - 79), ry = Mathf.Abs(y - 79);
            a = Mathf.Max(a, Mathf.Clamp01(9f - (rx + ry) + 0.5f));
            return a;
        }

        /// <summary>A stopwatch: ring, crown button, one hand.</summary>
        static float GhostRunIcon(float x, float y, float w, float h)
        {
            float cx = 64, cy = 58, r = 42;
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            float a = Mathf.Clamp01(4f - Mathf.Abs(d - r) + 0.5f);
            a = Mathf.Max(a, Line(x, y, 64, 104, 64, 114, 8));
            a = Mathf.Max(a, Line(x, y, 52, 118, 76, 118, 7));
            a = Mathf.Max(a, Line(x, y, 96, 92, 102, 98, 6));
            a = Mathf.Max(a, Line(x, y, cx, cy, cx + 20, cy + 22, 6));
            a = Mathf.Max(a, Mathf.Clamp01(6f - d + 0.5f));
            // Ticks at 12, 3, 6, 9.
            a = Mathf.Max(a, Line(x, y, cx, cy + 30, cx, cy + 36, 4));
            a = Mathf.Max(a, Line(x, y, cx + 30, cy, cx + 36, cy, 4));
            a = Mathf.Max(a, Line(x, y, cx, cy - 30, cx, cy - 36, 4));
            a = Mathf.Max(a, Line(x, y, cx - 30, cy, cx - 36, cy, 4));
            return a;
        }

        /// <summary>Three chevrons in a row, fading back: the pack chasing the leader.</summary>
        static float PackRaceIcon(float x, float y, float w, float h)
        {
            float a = 0f;
            float[] fade = { 0.35f, 0.65f, 1f };
            for (int i = 0; i < 3; i++)
            {
                float ox = 14 + i * 34;
                float c = Mathf.Max(Line(x, y, ox, 102, ox + 30, 64, 10), Line(x, y, ox + 30, 64, ox, 26, 10));
                a = Mathf.Max(a, c * fade[i]);
            }
            return a;
        }
    }
}
