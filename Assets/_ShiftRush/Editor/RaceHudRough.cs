using ShiftRush.Race;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftRush.EditorTools
{
    /// <summary>
    /// The race HUD in the "trail zine" look (Resources/SR_RaceHUD): position on a bolted race plate, the
    /// leaderboard on torn notebook paper, the lap on a taped kraft tag, the minimap sketched on a torn square,
    /// a sticker speedometer with a sprayed rev ring, boost as a strip of tape, mud flicked up at the corners.
    /// Rough frames, clean numbers: speed, times and gaps stay in Roboto Condensed. Drives the same
    /// <see cref="RaceHUD"/> component as before, so no game code changes.
    /// </summary>
    public static partial class MenuBuilder
    {
        static void MakeRaceHudRough(Sounds sounds)
        {
            MakeHudArt();
            if (fontNumber == null) LoadFonts();
            MakeRoughArt();
            var root = new GameObject("SR_RaceHUD", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var hud = root.AddComponent<RaceHUD>();
            var src = root.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.outputAudioMixerGroup = MixerGroup("UI");
            var so = new SerializedObject(hud);

            // Mud at the bottom corners, under everything.
            var mudL = Img(root.transform, "Mud L", roughMud, MudCol);
            Place(mudL.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(-30, -40), new Vector2(520, 390));
            var mudR = Img(root.transform, "Mud R", roughMud, MudCol);
            Place(mudR.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(30, -40), new Vector2(520, 390));
            mudR.rectTransform.localScale = new Vector3(-1, 1, 1);
            mudR.rectTransform.pivot = new Vector2(0, 0);

            var race = new GameObject("Race", typeof(RectTransform)).GetComponent<RectTransform>();
            race.SetParent(root.transform, false);
            Stretch(race);
            var raceGroup = race.gameObject.AddComponent<CanvasGroup>();
            raceGroup.blocksRaycasts = false;

            // ---- race plate, top left
            var plate = Img(race, "Position", roughPlate, PaperCol);
            plate.type = Image.Type.Sliced;
            Place(plate.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -48), new Vector2(250, 180));
            plate.rectTransform.localEulerAngles = new Vector3(0, 0, -3f);
            Tape(plate.transform, "Tape", new Vector2(0, 1), new Vector2(70, 2), new Vector2(170, 40), -5f);
            var posLabel = Scrawl(plate.transform, "Label", "POSITION", 24, InkCol, TextAlignmentOptions.Center);
            Place(posLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(70, 3), new Vector2(170, 40));
            posLabel.rectTransform.localEulerAngles = new Vector3(0, 0, -5f);
            var posValue = Txt(plate.transform, "Value", "1/6", fontStencil != null ? fontStencil : fontTitle, 120, InkCol, TextAlignmentOptions.Center);
            posValue.textWrappingMode = TextWrappingModes.NoWrap;
            Place(posValue.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -14), new Vector2(240, 140));

            // ---- notebook leaderboard under it
            var board = Paper(race, "Leaderboard", PaperCol, new Vector2(0, 1), new Vector2(44, -252), new Vector2(300, 6 * 36 + 34), 1.4f);
            board.rectTransform.pivot = new Vector2(0, 1);
            Place(board.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -252), new Vector2(300, 6 * 36 + 34));
            board.rectTransform.localEulerAngles = new Vector3(0, 0, 1.4f);
            var lines = Img(board.transform, "Lines", roughLines, new Color(0.27f, 0.43f, 0.7f, 0.28f));
            lines.type = Image.Type.Tiled;
            Stretch(lines.rectTransform);
            lines.rectTransform.offsetMin = new Vector2(10, 12); lines.rectTransform.offsetMax = new Vector2(-10, -14);
            Tape(board.transform, "Tape", new Vector2(0.5f, 1), new Vector2(0, 2), new Vector2(110, 32), -6f);
            var rowsProp = so.FindProperty("rows");
            rowsProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                var row = new GameObject("Row " + (i + 1), typeof(RectTransform)).GetComponent<RectTransform>();
                row.SetParent(board.transform, false);
                Place(row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, -18 - i * 36f), new Vector2(0, 34));
                var hl = Img(row, "You", roughTape, new Color(Magenta.r, Magenta.g, Magenta.b, 0.7f));
                hl.type = Image.Type.Sliced; Stretch(hl.rectTransform);
                hl.rectTransform.offsetMin = new Vector2(8, 1); hl.rectTransform.offsetMax = new Vector2(-8, -1);
                var rank = Txt(row, "Rank", "1", fontStencil != null ? fontStencil : fontTitle, 24, PencilCol, TextAlignmentOptions.Center);
                Place(rank.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(34, 0));
                var name = Scrawl(row, "Name", "YOU", 24, InkCol, TextAlignmentOptions.Left);
                Place(name.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(56, 1), new Vector2(-150, 0));
                var gap = Txt(row, "Gap", "LEADER", fontBold, 21, PencilCol, TextAlignmentOptions.Right);
                gap.fontStyle = FontStyles.Normal;
                Place(gap.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(110, 0));
                var el = rowsProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("root").objectReferenceValue = row.gameObject;
                el.FindPropertyRelative("highlight").objectReferenceValue = hl;
                el.FindPropertyRelative("rank").objectReferenceValue = rank;
                el.FindPropertyRelative("name").objectReferenceValue = name;
                el.FindPropertyRelative("gap").objectReferenceValue = gap;
            }

            // ---- kraft lap tag, top right
            var tag = Paper(race, "Lap", KraftCol, new Vector2(1, 1), Vector2.zero, new Vector2(330, 250), 2f);
            tag.rectTransform.pivot = new Vector2(1, 1);
            tag.rectTransform.anchoredPosition = new Vector2(-46, -40);
            Tape(tag.transform, "Tape", new Vector2(0.5f, 1), new Vector2(-20, 4), new Vector2(130, 36), 4f);
            var lapLabel = Scrawl(tag.transform, "Label", "LAP", 40, InkCol, TextAlignmentOptions.Left);
            Place(lapLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -34), new Vector2(110, 60));
            var lapValue = Txt(tag.transform, "Value", "1/2", fontDirt != null ? fontDirt : fontTitle, 84, InkCol, TextAlignmentOptions.Left);
            lapValue.textWrappingMode = TextWrappingModes.NoWrap;
            Place(lapValue.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -18), new Vector2(200, 96));
            TMP_Text[] timeVals = new TMP_Text[3];
            string[] timeNames = { "TIME", "LAP", "BEST" };
            for (int i = 0; i < 3; i++)
            {
                var l = Txt(tag.transform, timeNames[i] + " Label", timeNames[i], fontBold, 20, new Color32(94, 74, 52, 255), TextAlignmentOptions.Left);
                l.characterSpacing = 12;
                Place(l.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -122 - i * 38), new Vector2(110, 34));
                var v = Txt(tag.transform, timeNames[i], "--:--.---", fontNumber, 32, i == 2 ? DeepMagenta : InkCol, TextAlignmentOptions.Right);
                v.fontStyle = FontStyles.Normal;
                Place(v.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -118 - i * 38), new Vector2(200, 38));
                timeVals[i] = v;
            }

            // ---- trail sketch, bottom left
            // Transparent: just the trail, drawn like chalk over the world, fading out at the edge.
            var mapPaper = new GameObject("Minimap", typeof(RectTransform)).GetComponent<RectTransform>();
            mapPaper.SetParent(race, false);
            Place(mapPaper, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(44, 44), new Vector2(300, 300));
            var mapRoot = new GameObject("Map", typeof(RectTransform)).GetComponent<RectTransform>();
            mapRoot.SetParent(mapPaper, false);
            Place(mapRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(270, 270));
            var mapMask = mapRoot.gameObject.AddComponent<AMS.UI.SoftMask.UISoftMask>();
            mapMask.mask = softDisc;
            mapMask.fallOff = 1f;
            var map = Img(mapRoot, "Track", null, new Color(PaperCol.r, PaperCol.g, PaperCol.b, 0.9f));
            Place(map.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 240));
            map.raycastTarget = false;
            var dot = Img(mapRoot, "You", mapArrow, Magenta);
            Place(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(32, 32));

            // ---- free roam tag
            var free = Paper(root.transform, "Free Roam", KraftCol, new Vector2(0, 1), Vector2.zero, new Vector2(360, 120), -2f);
            free.rectTransform.pivot = new Vector2(0, 1);
            free.rectTransform.anchoredPosition = new Vector2(50, -46);
            Tape(free.transform, "Tape", new Vector2(0.5f, 1), new Vector2(0, 2), new Vector2(120, 32), -4f);
            var fv = Txt(free.transform, "Value", "FREE ROAM", fontDirt != null ? fontDirt : fontTitle, 52, InkCol, TextAlignmentOptions.Center);
            Stretch(fv.rectTransform);

            // ---- countdown: sprayed ring, outlined dirt numbers; messages slap on as stickers
            var ring = Img(root.transform, "Countdown Ring", roughRing, Color.white);
            ring.type = Image.Type.Filled; ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top; ring.fillClockwise = false;
            Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(330, 330));
            var count = Txt(root.transform, "Countdown", "3", fontStencil != null ? fontStencil : fontTitle, 200, Color.white, TextAlignmentOptions.Center);
            if (dirtOutline != null) count.fontSharedMaterial = dirtOutline;
            count.textWrappingMode = TextWrappingModes.NoWrap;
            Place(count.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(700, 280));

            var msg = new GameObject("Message", typeof(RectTransform)).GetComponent<RectTransform>();
            msg.SetParent(root.transform, false);
            Place(msg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(720, 100));
            msg.localEulerAngles = new Vector3(0, 0, -3f);
            var msgGroup = msg.gameObject.AddComponent<CanvasGroup>();
            msgGroup.blocksRaycasts = false;
            var msgEdge = Img(msg, "Die Cut", roughPaper, Color.white);
            msgEdge.type = Image.Type.Sliced; Stretch(msgEdge.rectTransform);
            msgEdge.rectTransform.offsetMin = new Vector2(-12, -12); msgEdge.rectTransform.offsetMax = new Vector2(12, 12);
            var msgBg = Img(msg, "Sticker", roughPaper, Magenta);
            msgBg.type = Image.Type.Sliced; Stretch(msgBg.rectTransform);
            var msgText = Txt(msg, "Text", "FINAL LAP", fontDirt != null ? fontDirt : fontTitle, 54, Color.white, TextAlignmentOptions.Center);
            msgText.textWrappingMode = TextWrappingModes.NoWrap;
            Stretch(msgText.rectTransform);

            // ---- sticker speedometer, bottom right
            var dash = new GameObject("Dash", typeof(RectTransform)).GetComponent<RectTransform>();
            dash.SetParent(root.transform, false);
            Stretch(dash);
            var dashGroup = dash.gameObject.AddComponent<CanvasGroup>();
            dashGroup.blocksRaycasts = false;
            var gauge = new GameObject("Speedometer", typeof(RectTransform)).GetComponent<RectTransform>();
            gauge.SetParent(dash, false);
            Place(gauge, new Vector2(1, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-230, 230), new Vector2(320, 320));
            gauge.localEulerAngles = new Vector3(0, 0, 6f);
            var cut = Img(gauge, "Die Cut", roughDisc, Color.white);
            Stretch(cut.rectTransform);
            var face = Img(gauge, "Face", roughDisc, PaperCol);
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(12, 12); face.rectTransform.offsetMax = new Vector2(-12, -12);
            var track = Img(gauge, "Arc", roughRing, new Color(InkCol.r, InkCol.g, InkCol.b, 0.14f));
            track.type = Image.Type.Filled; track.fillMethod = Image.FillMethod.Radial360;
            track.fillOrigin = (int)Image.Origin360.Bottom; track.fillClockwise = true; track.fillAmount = 0.75f;
            Stretch(track.rectTransform); track.rectTransform.localEulerAngles = new Vector3(0, 0, -45f);
            track.rectTransform.offsetMin = new Vector2(10, 10); track.rectTransform.offsetMax = new Vector2(-10, -10);
            var rpm = Img(gauge, "RPM", roughRing, Magenta);
            rpm.type = Image.Type.Filled; rpm.fillMethod = Image.FillMethod.Radial360;
            rpm.fillOrigin = (int)Image.Origin360.Bottom; rpm.fillClockwise = true; rpm.fillAmount = 0.4f;
            Stretch(rpm.rectTransform); rpm.rectTransform.localEulerAngles = new Vector3(0, 0, -45f);
            rpm.rectTransform.offsetMin = new Vector2(10, 10); rpm.rectTransform.offsetMax = new Vector2(-10, -10);
            var speed = Txt(gauge, "Speed", "000", fontNumber, 112, InkCol, TextAlignmentOptions.Center);
            speed.textWrappingMode = TextWrappingModes.NoWrap;
            Place(speed.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 16), new Vector2(260, 116));
            var unit = Txt(gauge, "Unit", "KM/H", fontBold, 20, PencilCol, TextAlignmentOptions.Center);
            unit.characterSpacing = 14;
            Place(unit.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -44), new Vector2(200, 30));
            var gearBlob = Img(gauge, "Gear", roughBlob, Magenta);
            Place(gearBlob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(118, -112), new Vector2(96, 96));
            gearBlob.rectTransform.localEulerAngles = new Vector3(0, 0, -14f);
            var gear = Txt(gearBlob.rectTransform, "Value", "1", fontStencil != null ? fontStencil : fontTitle, 46, Color.white, TextAlignmentOptions.Center);
            Stretch(gear.rectTransform);

            // ---- boost: a strip of tape that fills
            var nos = new GameObject("Nitro", typeof(RectTransform)).GetComponent<RectTransform>();
            nos.SetParent(dash, false);
            Place(nos, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 28), new Vector2(360, 34));
            nos.localEulerAngles = new Vector3(0, 0, -1.5f);
            var nosLabel = Scrawl(nos, "Label", "boost", 30, Amber, TextAlignmentOptions.Right);
            Place(nosLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 4), new Vector2(110, 40));
            var nosBg = Img(nos, "Bg", roughTape, TapeCol); // plain grey tape: the empty part has to read over dark trail
            nosBg.type = Image.Type.Sliced; Stretch(nosBg.rectTransform);
            var nosFill = Img(nos, "Fill", roughTape, Magenta);
            nosFill.type = Image.Type.Sliced;
            nosFill.rectTransform.anchorMin = Vector2.zero; nosFill.rectTransform.anchorMax = Vector2.one;
            nosFill.rectTransform.offsetMin = new Vector2(0, 3); nosFill.rectTransform.offsetMax = new Vector2(0, -3);

            // ---- results: a kraft card taped over a darkened screen
            var fin = new GameObject("Results", typeof(RectTransform)).GetComponent<RectTransform>();
            fin.SetParent(root.transform, false);
            Stretch(fin);
            var finGroup = fin.gameObject.AddComponent<CanvasGroup>();
            var finShade = Img(fin, "Shade", null, new Color(InkCol.r, InkCol.g, InkCol.b, 0.55f));
            Stretch(finShade.rectTransform);
            var card = Paper(fin, "Card", KraftCol, new Vector2(0, 0.5f), Vector2.zero, new Vector2(920, 760), -1.5f);
            card.rectTransform.pivot = new Vector2(0, 0.5f);
            card.rectTransform.anchoredPosition = new Vector2(90, 10);
            Tape(card.transform, "Tape L", new Vector2(0, 1), new Vector2(90, 0), new Vector2(170, 44), -12f);
            Tape(card.transform, "Tape R", new Vector2(1, 1), new Vector2(-90, 0), new Vector2(170, 44), 10f);
            var c = card.transform;
            var finTitle = Txt(c, "Title", "RACE COMPLETE", fontDirt != null ? fontDirt : fontTitle, 92, InkCol, TextAlignmentOptions.Left);
            finTitle.textWrappingMode = TextWrappingModes.NoWrap;
            Place(finTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(54, -60), new Vector2(840, 110));
            var posL = Scrawl(c, "Position Label", "FINISHED", 30, PencilCol, TextAlignmentOptions.Left);
            Place(posL.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -178), new Vector2(400, 44));
            var finPos = Txt(c, "Position", "1ST", fontStencil != null ? fontStencil : fontTitle, 150, DeepMagenta, TextAlignmentOptions.TopLeft);
            finPos.textWrappingMode = TextWrappingModes.NoWrap;
            Place(finPos.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(54, -218), new Vector2(800, 170));
            var totL = Scrawl(c, "Total Label", "TOTAL TIME", 26, PencilCol, TextAlignmentOptions.Left);
            Place(totL.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -404), new Vector2(380, 40));
            var finTotal = Txt(c, "Total", "00:00.000", fontNumber, 58, InkCol, TextAlignmentOptions.TopLeft);
            Place(finTotal.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(56, -440), new Vector2(380, 70));
            var bestL = Scrawl(c, "Best Label", "BEST LAP EVER", 26, PencilCol, TextAlignmentOptions.Left);
            Place(bestL.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(460, -404), new Vector2(400, 40));
            var finBest = Txt(c, "Best", "00:00.000", fontNumber, 58, DeepMagenta, TextAlignmentOptions.TopLeft);
            Place(finBest.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(456, -440), new Vector2(400, 70));
            var finLaps = Txt(c, "Laps", "", fontBold, 28, InkCol, TextAlignmentOptions.TopLeft);
            finLaps.lineSpacing = 18;
            Place(finLaps.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -530), new Vector2(780, 200));
            var hintTape = Tape(fin, "Hint Tape", new Vector2(0, 0), new Vector2(420, 70), new Vector2(640, 52), 1f, new Color(InkCol.r, InkCol.g, InkCol.b, 0.85f));
            var finHint = Scrawl(hintTape.transform, "Hint", "<color=#FF40D9>ENTER</color>  race again      <color=#FF40D9>M</color>  main menu", 28, Color.white, TextAlignmentOptions.Center);
            Stretch(finHint.rectTransform);

            // ---- wire up (same fields as the clean HUD)
            so.FindProperty("raceGroup").objectReferenceValue = raceGroup;
            so.FindProperty("freeRoamTag").objectReferenceValue = free.gameObject;
            so.FindProperty("positionValue").objectReferenceValue = posValue;
            so.FindProperty("lapValue").objectReferenceValue = lapValue;
            so.FindProperty("timeValue").objectReferenceValue = timeVals[0];
            so.FindProperty("lapTimeValue").objectReferenceValue = timeVals[1];
            so.FindProperty("bestValue").objectReferenceValue = timeVals[2];
            so.FindProperty("countdownText").objectReferenceValue = count;
            so.FindProperty("countdownRing").objectReferenceValue = ring;
            so.FindProperty("messageGroup").objectReferenceValue = msgGroup;
            so.FindProperty("messageText").objectReferenceValue = msgText;
            so.FindProperty("speedValue").objectReferenceValue = speed;
            so.FindProperty("unitText").objectReferenceValue = unit;
            so.FindProperty("gearText").objectReferenceValue = gear;
            so.FindProperty("rpmFill").objectReferenceValue = rpm;
            so.FindProperty("nitroFill").objectReferenceValue = nosFill.rectTransform;
            so.FindProperty("nitroFillImage").objectReferenceValue = nosFill;
            so.FindProperty("nitroGroup").objectReferenceValue = nos.gameObject;
            so.FindProperty("mapImage").objectReferenceValue = map;
            so.FindProperty("mapView").objectReferenceValue = mapRoot;
            so.FindProperty("mapFade").objectReferenceValue = mapMask;
            so.FindProperty("mapDot").objectReferenceValue = dot.rectTransform;
            so.FindProperty("rivalDot").objectReferenceValue = roughBlob;
            so.FindProperty("rivalDotSize").floatValue = 17f;
            so.FindProperty("finishGroup").objectReferenceValue = finGroup;
            so.FindProperty("dashGroup").objectReferenceValue = dashGroup;
            so.FindProperty("finishPosition").objectReferenceValue = finPos;
            so.FindProperty("finishTotal").objectReferenceValue = finTotal;
            so.FindProperty("finishBest").objectReferenceValue = finBest;
            so.FindProperty("finishLaps").objectReferenceValue = finLaps;
            so.FindProperty("audioSource").objectReferenceValue = src;
            so.FindProperty("beep").objectReferenceValue = sounds.select;
            so.FindProperty("go").objectReferenceValue = sounds.confirm;
            so.FindProperty("lap").objectReferenceValue = sounds.shift;
            so.FindProperty("accent").colorValue = Magenta;
            so.FindProperty("steel").colorValue = PencilCol; // non-player names, now on paper
            so.ApplyModifiedPropertiesWithoutUndo();

            foreach (var g in root.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(root, HudPath);
            Object.DestroyImmediate(root);
        }
    }
}
