using ShiftRush.AirShow;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftRush.EditorTools
{
    /// <summary>
    /// Builds Resources/BR_AirShowHUD.prefab: score top left in Air Show orange, the course strip top centre,
    /// clock and best top right, a points pop for each landing, the countdown, the "fly through the boxes" hint
    /// and the results card with a big rank letter.
    /// </summary>
    public static partial class MenuBuilder
    {
        const string AirHudPath = "Assets/_ShiftRush/Resources/BR_AirShowHUD.prefab";
        static readonly Color Amber = new Color32(255, 158, 46, 255);

        [MenuItem("BIKE RUSH/Menu/Build Air Show HUD")]
        static void BuildAirShowHudOnly()
        {
            LoadFonts();
            MakeArt();
            MakeAirShowHud(LoadSounds());
            AssetDatabase.SaveAssets();
            Debug.Log("[BIKE//RUSH] Air Show HUD built: " + AirHudPath);
        }

        static void MakeAirShowHud(Sounds sounds)
        {
            if (softDisc == null || disc == null) MakeHudArt();
            var root = new GameObject("BR_AirShowHUD", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 310; // over the race HUD's speedo, under the pause menu (400)
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var hud = root.AddComponent<AirShowHUD>();
            var src = root.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.outputAudioMixerGroup = MixerGroup("UI");
            var so = new SerializedObject(hud);

            var play = new GameObject("Play", typeof(RectTransform)).GetComponent<RectTransform>();
            play.SetParent(root.transform, false);
            Stretch(play);
            var playGroup = play.gameObject.AddComponent<CanvasGroup>();

            // ---- score, top left
            var bar = Img(play, "Accent", slant, Amber);
            bar.type = Image.Type.Sliced;
            Place(bar.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -46), new Vector2(84, 10));
            var scoreLabel = Txt(play, "Score Label", "SCORE", fontBold, 22, Steel, TextAlignmentOptions.Left);
            scoreLabel.characterSpacing = 10;
            Place(scoreLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -64), new Vector2(400, 30));
            var score = Txt(play, "Score", "0", fontTitle, 112, Amber, TextAlignmentOptions.TopLeft);
            score.textWrappingMode = TextWrappingModes.NoWrap;
            Place(score.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(56, -84), new Vector2(700, 130));
            var venue = Txt(play, "Venue", "CLOUDBREAK", fontBold, 22, Color.white, TextAlignmentOptions.Left);
            venue.characterSpacing = 10;
            Place(venue.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -214), new Vector2(700, 30));

            // ---- course strip, top centre: start flag ... finish arch, the bike sliding along
            var strip = new GameObject("Course", typeof(RectTransform)).GetComponent<RectTransform>();
            strip.SetParent(play, false);
            Place(strip, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(640, 10));
            var stripBg = Img(strip, "Bg", slant, new Color(1, 1, 1, 0.14f));
            stripBg.type = Image.Type.Sliced; Stretch(stripBg.rectTransform);
            var fill = Img(strip, "Fill", slant, Amber);
            fill.type = Image.Type.Sliced;
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var marker = Img(strip, "Bike", chevron, Color.white);
            Place(marker.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 34));
            var sLbl = Txt(strip, "Start", "DROP IN", fontBold, 16, Steel, TextAlignmentOptions.Left);
            sLbl.characterSpacing = 8;
            Place(sLbl.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, -12), new Vector2(200, 24));
            var fLbl = Txt(strip, "Finish", "FINISH", fontBold, 16, Steel, TextAlignmentOptions.Right);
            fLbl.characterSpacing = 8;
            Place(fLbl.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, -12), new Vector2(200, 24));

            // ---- clock and best, top right
            var tBar = Img(play, "Accent R", slant, Amber);
            tBar.type = Image.Type.Sliced;
            Place(tBar.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -46), new Vector2(84, 10));
            string[] names = { "TIME", "BEST SCORE" };
            var vals = new TextMeshProUGUI[2];
            for (int i = 0; i < 2; i++)
            {
                var row = new GameObject(names[i], typeof(RectTransform)).GetComponent<RectTransform>();
                row.SetParent(play, false);
                Place(row, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -70 - i * 46), new Vector2(460, 40));
                var bg = Img(row, "Bg", slant, new Color(Ink.r, Ink.g, Ink.b, 0.55f));
                bg.type = Image.Type.Sliced; Stretch(bg.rectTransform);
                var l = Txt(row, "Label", names[i], fontBold, 20, Steel, TextAlignmentOptions.Left);
                l.characterSpacing = 8;
                Place(l.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 0), new Vector2(220, 30));
                vals[i] = Txt(row, "Value", "--", fontTitle, 28, i == 1 ? Amber : Color.white, TextAlignmentOptions.Right);
                Place(vals[i].rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-26, 0), new Vector2(220, 36));
            }

            // ---- landing pop, upper centre
            var pop = new GameObject("Pop", typeof(RectTransform)).GetComponent<RectTransform>();
            pop.SetParent(play, false);
            Place(pop, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(900, 200));
            var popGroup = pop.gameObject.AddComponent<CanvasGroup>();
            var popPts = Txt(pop, "Points", "+1,600", fontTitle, 96, Amber, TextAlignmentOptions.Center);
            Place(popPts.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(900, 110));
            var popName = Txt(pop, "Name", "BACKFLIP", fontTitle, 40, Color.white, TextAlignmentOptions.Center);
            Place(popName.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -104), new Vector2(900, 50));
            var popGrade = Txt(pop, "Grade", "PERFECT", fontBold, 22, Steel, TextAlignmentOptions.Center);
            popGrade.characterSpacing = 14;
            Place(popGrade.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -156), new Vector2(900, 30));

            // ---- countdown and hint
            var count = Txt(play, "Countdown", "", fontTitle, 200, Color.white, TextAlignmentOptions.Center);
            count.textWrappingMode = TextWrappingModes.NoWrap;
            Place(count.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1400, 260));
            var hint = new GameObject("Hint", typeof(RectTransform)).GetComponent<RectTransform>();
            hint.SetParent(play, false);
            Place(hint, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 170), new Vector2(1000, 54));
            var hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            var hintBg = Img(hint, "Bg", slant, new Color(Ink.r, Ink.g, Ink.b, 0.7f));
            hintBg.type = Image.Type.Sliced; Stretch(hintBg.rectTransform);
            var hintText = Txt(hint, "Text", "", fontBold, 22, Color.white, TextAlignmentOptions.Center);
            hintText.characterSpacing = 6;
            Stretch(hintText.rectTransform);

            // ---- results
            var fin = new GameObject("Results", typeof(RectTransform)).GetComponent<RectTransform>();
            fin.SetParent(root.transform, false);
            Stretch(fin);
            var finGroup = fin.gameObject.AddComponent<CanvasGroup>();
            var finShade = Img(fin, "Shade", null, new Color(Ink.r, Ink.g, Ink.b, 0.4f));
            Stretch(finShade.rectTransform);
            var finDim = Img(fin, "Dim", fadeLeft, new Color(Ink.r, Ink.g, Ink.b, 0.95f));
            Stretch(finDim.rectTransform);
            finDim.rectTransform.anchorMax = new Vector2(0.75f, 1f);
            var finBar = Img(fin, "Accent", slant, Amber);
            finBar.type = Image.Type.Sliced;
            Place(finBar.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, 300), new Vector2(110, 12));
            var finTitle = Txt(fin, "Title", "SHOW'S OVER", fontTitle, 96, Color.white, TextAlignmentOptions.Left);
            Place(finTitle.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(116, 230), new Vector2(1100, 110));
            var sl = Txt(fin, "Score Label", "SCORE", fontBold, 22, Steel, TextAlignmentOptions.Left);
            sl.characterSpacing = 10;
            Place(sl.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 1), new Vector2(124, 172), new Vector2(400, 30));
            var finScore = Txt(fin, "Score", "0", fontTitle, 150, Amber, TextAlignmentOptions.TopLeft);
            finScore.textWrappingMode = TextWrappingModes.NoWrap;
            Place(finScore.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 1), new Vector2(116, 150), new Vector2(900, 170));
            var bl = Txt(fin, "Best Label", "BEST SCORE", fontBold, 22, Steel, TextAlignmentOptions.Left);
            bl.characterSpacing = 10;
            Place(bl.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 1), new Vector2(124, -36), new Vector2(400, 30));
            var finBest = Txt(fin, "Best", "0", fontTitle, 56, Color.white, TextAlignmentOptions.TopLeft);
            Place(finBest.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 1), new Vector2(120, -60), new Vector2(400, 70));
            var finStats = Txt(fin, "Stats", "", fontBold, 26, Color.white, TextAlignmentOptions.TopLeft);
            finStats.lineSpacing = 16;
            Place(finStats.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 1), new Vector2(520, -40), new Vector2(520, 260));

            // The rank: one huge letter, no frame, stamped in once the score has counted up. A soft flash
            // behind it pops on impact.
            var rank = new GameObject("Rank", typeof(RectTransform)).GetComponent<RectTransform>();
            rank.SetParent(fin, false);
            Place(rank, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1280, 20), new Vector2(680, 680));
            var flash = Img(rank, "Flash", softDisc, new Color(1, 1, 1, 0f));
            Place(flash.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 620));
            var rl = Txt(rank, "Label", "RANK", fontBold, 26, Steel, TextAlignmentOptions.Center);
            rl.characterSpacing = 18;
            Place(rl.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(400, 40));
            var finRank = Txt(rank, "Letter", "S", fontTitle, 660, Amber, TextAlignmentOptions.Center);
            finRank.textWrappingMode = TextWrappingModes.NoWrap;
            Place(finRank.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(680, 680));

            var finHint = Txt(fin, "Hint", "<color=#FF9E2E>ENTER</color>  RIDE AGAIN      <color=#FF9E2E>M</color>  MAIN MENU", fontBold, 22, Color.white, TextAlignmentOptions.Left);
            finHint.characterSpacing = 6;
            Place(finHint.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(124, 70), new Vector2(900, 40));

            so.FindProperty("scoreValue").objectReferenceValue = score;
            so.FindProperty("timeValue").objectReferenceValue = vals[0];
            so.FindProperty("bestValue").objectReferenceValue = vals[1];
            so.FindProperty("venueText").objectReferenceValue = venue;
            so.FindProperty("progressFill").objectReferenceValue = fill.rectTransform;
            so.FindProperty("progressBike").objectReferenceValue = marker.rectTransform;
            so.FindProperty("popGroup").objectReferenceValue = popGroup;
            so.FindProperty("popPoints").objectReferenceValue = popPts;
            so.FindProperty("popName").objectReferenceValue = popName;
            so.FindProperty("popGrade").objectReferenceValue = popGrade;
            so.FindProperty("countdownText").objectReferenceValue = count;
            so.FindProperty("hintText").objectReferenceValue = hintText;
            so.FindProperty("hintGroup").objectReferenceValue = hintGroup;
            so.FindProperty("finishGroup").objectReferenceValue = finGroup;
            so.FindProperty("playGroup").objectReferenceValue = playGroup;
            so.FindProperty("finScore").objectReferenceValue = finScore;
            so.FindProperty("finRank").objectReferenceValue = finRank;
            so.FindProperty("rankRoot").objectReferenceValue = rank;
            so.FindProperty("rankFlash").objectReferenceValue = flash;
            so.FindProperty("rankLabel").objectReferenceValue = rl;
            so.FindProperty("stamp").objectReferenceValue = sounds.confirm;
            so.FindProperty("tick").objectReferenceValue = sounds.tick;
            so.FindProperty("finBest").objectReferenceValue = finBest;
            so.FindProperty("finStats").objectReferenceValue = finStats;
            so.FindProperty("finTitle").objectReferenceValue = finTitle;
            so.FindProperty("audioSource").objectReferenceValue = src;
            so.FindProperty("beep").objectReferenceValue = sounds.select;
            so.FindProperty("go").objectReferenceValue = sounds.confirm;
            so.FindProperty("accent").colorValue = Amber;
            so.ApplyModifiedPropertiesWithoutUndo();

            foreach (var g in root.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(root, AirHudPath);
            Object.DestroyImmediate(root);
        }
    }
}
