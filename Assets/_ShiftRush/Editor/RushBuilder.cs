using ShiftRush.Race;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftRush.EditorTools
{
    /// <summary>
    /// Builds Resources/SR_RushHUD.prefab: the bonus pop (SLIPSTREAM, CLOSE CALL, WALL SKIM with its chain count)
    /// and the draft meter, down the right of the race screen above the speedometer.
    /// </summary>
    public static partial class MenuBuilder
    {
        const string RushPath = "Assets/_ShiftRush/Resources/SR_RushHUD.prefab";

        [MenuItem("SHIFT RUSH/Build Rush Bonus HUD")]
        static void BuildRushOnly()
        {
            LoadFonts();
            MakeArt();
            MakeRushHud();
            AssetDatabase.SaveAssets();
            Debug.Log("[SHIFT//RUSH] Rush bonus HUD built: " + RushPath);
        }

        static void MakeRushHud()
        {
            var root = new GameObject("SR_RushHUD", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 340; // over the race HUD, under the SHIFT HUD's slow-motion tint
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var rush = root.AddComponent<RushBonus>();

            // The pop: right of centre, leaning like the logo. Scales from its right edge when it punches.
            var pop = new GameObject("Pop", typeof(RectTransform)).GetComponent<RectTransform>();
            pop.SetParent(root.transform, false);
            Place(pop, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-70, 60), new Vector2(560, 150));
            var popGroup = pop.gameObject.AddComponent<CanvasGroup>();
            var backing = Img(pop, "Backing", slant, new Color(Panel.r, Panel.g, Panel.b, 0.6f)); // reads over bright sky
            backing.type = Image.Type.Sliced;
            Place(backing.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-10, 4), new Vector2(40, 10));
            var bar = Img(pop, "Stripe", slant, Magenta);
            bar.type = Image.Type.Sliced; bar.pixelsPerUnitMultiplier = 4f;
            Place(bar.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-6, -8), new Vector2(70, 10));
            var head = Txt(pop, "Headline", "CLOSE CALL", fontTitle, 64, Color.white, TextAlignmentOptions.Right);
            Place(head.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, -22), new Vector2(0, 74));
            var chain = Txt(pop, "Chain", "x2", fontTitle, 44, Magenta, TextAlignmentOptions.Right);
            Place(chain.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 8), new Vector2(0, 50));
            var detail = Txt(pop, "Detail", "+6% BOOST", fontBold, 22, Steel, TextAlignmentOptions.Right);
            detail.characterSpacing = 10;
            Place(detail.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-96, 18), new Vector2(-96, 30));

            // Draft meter: a thin slanted bar that fills while you're in a rival's slipstream.
            var draft = new GameObject("Draft", typeof(RectTransform)).GetComponent<RectTransform>();
            draft.SetParent(root.transform, false);
            Place(draft, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-76, -66), new Vector2(300, 44));
            var draftGroup = draft.gameObject.AddComponent<CanvasGroup>();
            var tag = Txt(draft, "Tag", "SLIPSTREAM", fontBold, 17, Cyan, TextAlignmentOptions.Right);
            tag.characterSpacing = 12;
            Place(tag.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 22));
            var track = Img(draft, "Track", slant, new Color(1, 1, 1, 0.14f));
            track.type = Image.Type.Sliced; track.pixelsPerUnitMultiplier = 4f;
            Place(track.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(0, 12));
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(track.rectTransform, false);
            Stretch(fillArea);
            var fill = Img(fillArea, "Fill", slant, Cyan);
            fill.type = Image.Type.Sliced; fill.pixelsPerUnitMultiplier = 4f;
            Place(fill.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);

            var so = new SerializedObject(rush);
            so.FindProperty("popGroup").objectReferenceValue = popGroup;
            so.FindProperty("popHeadline").objectReferenceValue = head;
            so.FindProperty("popDetail").objectReferenceValue = detail;
            so.FindProperty("popChain").objectReferenceValue = chain;
            so.FindProperty("draftGroup").objectReferenceValue = draftGroup;
            so.FindProperty("draftFill").objectReferenceValue = fill.rectTransform;
            so.FindProperty("draftFillImage").objectReferenceValue = fill;
            so.FindProperty("accent").colorValue = Magenta;
            so.FindProperty("cyan").colorValue = Cyan;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Landing: the SHIFT diamond again. A magenta diamond closes on a white one as the ground comes up;
            // the live trick name sits above it.
            var diamondFrame = AssetDatabase.LoadAssetAtPath<Sprite>(UIArt + "T_DiamondFrame.png");
            var land = new GameObject("Landing", typeof(RectTransform)).GetComponent<RectTransform>();
            land.SetParent(root.transform, false);
            Place(land, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(520, 260));
            var landGroup = land.gameObject.AddComponent<CanvasGroup>();
            // The fill: a solid diamond inside the target that grows as the closing diamond nears and turns green
            // in the window, so there's one thing to watch.
            var diamondFill = SaveSprite("T_DiamondFill", 128, 128, (x, y, w, h) =>
                Mathf.Clamp01(54f - (Mathf.Abs(x - w * 0.5f) + Mathf.Abs(y - h * 0.5f)) + 0.5f), Vector4.zero);
            var fillD = Img(land, "Fill Diamond", diamondFill, new Color(1, 1, 1, 0f));
            Place(fillD.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104, 104));
            var target = Img(land, "Target Diamond", diamondFrame, Color.white);
            Place(target.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 120));
            var closer = Img(land, "Closing Diamond", diamondFrame, Magenta);
            Place(closer.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 120));
            var now = Txt(land, "Now", "NOW!", fontTitle, 64, new Color32(61, 255, 138, 255), TextAlignmentOptions.Center);
            now.textWrappingMode = TextWrappingModes.NoWrap;
            Place(now.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(170, 0), new Vector2(240, 80));
            var trick = Txt(land, "Trick", "BACKFLIP 360", fontTitle, 54, Color.white, TextAlignmentOptions.Center);
            Place(trick.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(0, 64));
            var key = Txt(land, "Key", "SHIFT TO LAND", fontTitle, 28, Color.white, TextAlignmentOptions.Center);
            if (dirtOutline != null) key.fontSharedMaterial = dirtOutline; // ink outline: reads over sky and pines
            key.characterSpacing = 8;
            Place(key.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(0, 30));
            // Lesson and miss tips on a strip of dark gaffer tape (sized to the words at runtime), so they read
            // over bright sky as well as dark pines.
            var coachTape = Img(land, "Coach", slant, new Color(Ink.r, Ink.g, Ink.b, 0.88f));
            coachTape.type = Image.Type.Sliced;
            Place(coachTape.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(700, 50));
            var coach = Txt(coachTape.transform, "Text", "", fontMarker != null ? fontMarker : fontBold, 26, Color.white, TextAlignmentOptions.Center);
            coach.textWrappingMode = TextWrappingModes.NoWrap;
            Stretch(coach.rectTransform);
            var stunts = root.AddComponent<ShiftRush.Stunts.StuntDirector>();
            var sso = new SerializedObject(stunts);
            sso.FindProperty("landGroup").objectReferenceValue = landGroup;
            sso.FindProperty("targetDiamond").objectReferenceValue = target;
            sso.FindProperty("closingDiamond").objectReferenceValue = closer;
            sso.FindProperty("trickText").objectReferenceValue = trick;
            sso.FindProperty("keyText").objectReferenceValue = key;
            sso.FindProperty("fillDiamond").objectReferenceValue = fillD;
            sso.FindProperty("nowText").objectReferenceValue = now;
            sso.FindProperty("coachText").objectReferenceValue = coach;
            sso.FindProperty("accent").colorValue = Magenta;
            sso.ApplyModifiedPropertiesWithoutUndo();

            foreach (var g in root.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(root, RushPath);
            Object.DestroyImmediate(root);
        }
    }
}
