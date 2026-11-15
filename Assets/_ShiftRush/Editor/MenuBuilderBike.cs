using System.Collections.Generic;
using System.IO;
using ShiftRush.Core;
using ShiftRush.Race;
using ShiftRush.UI;
using ShiftRush.Vehicles;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftRush.EditorTools
{
    /// <summary>
    /// The bike side of the menu: the three bikes (each its own race prefab with its own numbers), the trail list,
    /// the trail's hero shot and map, and the Tune screen. Called from Build Main Menu; "BIKE RUSH/Menu/Build
    /// Bikes and Catalog" redoes just the bikes and catalog.
    /// </summary>
    public static partial class MenuBuilder
    {
        const string BikeDir = "Assets/_ShiftRush/Prefabs/Bikes/";
        const string PackDir = "Assets/Simple Bicycle Physics/Prefabs/";
        const string RidgeLineScene = "Assets/_ShiftRush/Scenes/Course_RidgeLine.unity";

        class BikeSpec
        {
            public string id, name, tagline, source, prefab;
            public int top, launch, carve, air;
            public Color accent;
            public BikeTuning.Numbers n;
        }

        // The MTB is the one the course builder makes and the rivals ride; the other two are built here.
        static readonly BikeSpec[] Bikes =
        {
            new BikeSpec
            {
                id = "switchback", name = "Switchback", tagline = "Does a bit of everything. The one to learn on.",
                source = PackDir + "Mountain Bike MTBRider2.prefab", prefab = BikeDir + "BR_Bike_MTB.prefab",
                top = 6, launch = 6, carve = 6, air = 6, accent = new Color32(255, 64, 217, 255),
                n = new BikeTuning.Numbers { cruiseKmh = 58f, boostKmh = 86f, launchAccel = 8f, boostAccel = 9f, boostKick = 3f, burnPerSecond = 0.16f, carveRate = 70f, carveGrip = 120f, groundStick = 14f, takeoffPop = 0f },
            },
            new BikeSpec
            {
                id = "sparkplug", name = "Sparkplug", tagline = "Tiny, twitchy, off the line like a slingshot. Lives in the air.",
                source = PackDir + "Bmx.prefab", prefab = BikeDir + "BR_Bike_BMX.prefab",
                top = 4, launch = 9, carve = 8, air = 9, accent = new Color32(56, 225, 255, 255),
                n = new BikeTuning.Numbers { cruiseKmh = 54f, boostKmh = 82f, launchAccel = 10.5f, boostAccel = 10f, boostKick = 3.6f, burnPerSecond = 0.18f, carveRate = 82f, carveGrip = 135f, groundStick = 10f, takeoffPop = 0.8f },
            },
            new BikeSpec
            {
                id = "landslide", name = "Landslide", tagline = "Long travel, huge top end. Turns like it's thinking about it.",
                source = PackDir + "Downhill Bicycle.prefab", prefab = BikeDir + "BR_Bike_DH.prefab",
                top = 9, launch = 4, carve = 4, air = 5, accent = new Color32(255, 150, 30, 255),
                n = new BikeTuning.Numbers { cruiseKmh = 63f, boostKmh = 92f, launchAccel = 6.5f, boostAccel = 8f, boostKick = 2.6f, burnPerSecond = 0.13f, carveRate = 60f, carveGrip = 105f, groundStick = 18f, takeoffPop = 0f },
            },
        };

        [MenuItem("BIKE RUSH/Menu/Build Bikes and Catalog")]
        public static void BuildBikesAndCatalog()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var cat = MakeBikeCatalog();
            MakeCarPictures(cat);
            AssetDatabase.SaveAssets();
        }

        const string CatalogPath = "Assets/_ShiftRush/Resources/RaceCatalog.asset";

        /// <summary>
        /// Bikes and trails into the catalog (replacing the car game's list), with the trail's art. Opens the trail
        /// scene for its photo, which unloads assets, so the catalog is loaded afresh after that and returned.
        /// </summary>
        static RaceCatalog MakeBikeCatalog()
        {
            var track = new RaceCatalog.Track
            {
                id = "ridgeline", displayName = "Ridge Line", scene = "Course_RidgeLine",
                description = "A ridge trail above the pines: fast dirt, kickers, a cliff drop and boost pads that make you pick a line.",
                lengthKm = 0.9f,
                loadingTips = new[]
                {
                    "The trail steers with you. Lean into the stick to pick your line.",
                    "Boost pads sit left, middle or right. Pick your line early.",
                    "Hold Shift for boost on the ground. In the air, press it on the green.",
                    "Hold Space to crouch, let go at the lip for more air.",
                    "R puts you back on the trail.",
                },
            };
            MakeTrailArt(track);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cat = AssetDatabase.LoadAssetAtPath<RaceCatalog>(CatalogPath);
            if (cat == null) return null;
            var keep = new Dictionary<string, RaceCatalog.Car>();
            foreach (var c in cat.cars) if (c != null && !string.IsNullOrEmpty(c.id)) keep[c.id] = c;
            cat.cars.Clear();
            foreach (var b in Bikes)
            {
                var prefab = MakeBikePrefab(b);
                if (prefab == null) continue;
                keep.TryGetValue(b.id, out var old);
                cat.cars.Add(new RaceCatalog.Car
                {
                    id = b.id, displayName = b.name, tagline = b.tagline, prefab = prefab,
                    topSpeed = b.top, acceleration = b.launch, handling = b.carve, air = b.air, boost = b.air,
                    accent = b.accent, thumbnail = old != null ? old.thumbnail : null,
                    nitroExhausts = new Vector3[0],
                });
            }

            track.thumbnail = AssetDatabase.LoadAssetAtPath<Sprite>(UIArt + "T_Trail_RidgeLine.png");
            track.map = AssetDatabase.LoadAssetAtPath<Sprite>(UIArt + "T_TrailMap_RidgeLine.png");
            cat.tracks.Clear();
            cat.tracks.Add(track);
            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            PlayerPrefs.SetInt("SR_LastCar", 0);
            PlayerPrefs.SetInt("SR_LastTrack", 0);
            return cat;
        }

        /// <summary>A race bike from a pack bike: our adapter and reset on top, and this bike's own numbers.</summary>
        static GameObject MakeBikePrefab(BikeSpec b)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(b.prefab);
            GameObject prefab = existing;
            if (prefab == null)
            {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(b.source);
                if (src == null) { Debug.LogWarning("[BIKE//RUSH] Missing pack bike: " + b.source); return null; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
                if (inst.GetComponent<BikeVehicleAdapter>() == null) inst.AddComponent<BikeVehicleAdapter>();
                if (inst.GetComponent<VehicleReset>() == null) inst.AddComponent<VehicleReset>();
                prefab = PrefabUtility.SaveAsPrefabAsset(inst, b.prefab);
                Object.DestroyImmediate(inst);
            }
            var so = new SerializedObject(prefab.GetComponent<BikeVehicleAdapter>());
            so.FindProperty("cruiseKmh").floatValue = b.n.cruiseKmh;
            so.FindProperty("boostKmh").floatValue = b.n.boostKmh;
            so.FindProperty("launchAccel").floatValue = b.n.launchAccel;
            so.FindProperty("boostAccel").floatValue = b.n.boostAccel;
            so.FindProperty("boostKick").floatValue = b.n.boostKick;
            so.FindProperty("burnPerSecond").floatValue = b.n.burnPerSecond;
            so.FindProperty("carveRate").floatValue = b.n.carveRate;
            so.FindProperty("carveGrip").floatValue = b.n.carveGrip;
            so.FindProperty("groundStick").floatValue = b.n.groundStick;
            so.FindProperty("takeoffPop").floatValue = b.n.takeoffPop;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssetIfDirty(prefab);
            return prefab;
        }

        /// <summary>The trail card's hero shot (from the course itself) and its map (drawn from the race line).</summary>
        static void MakeTrailArt(RaceCatalog.Track track)
        {
            if (!File.Exists(RidgeLineScene)) return;
            var scene = EditorSceneManager.OpenScene(RidgeLineScene, OpenSceneMode.Single);
            var rl = Object.FindFirstObjectByType<RaceLine>();
            if (rl == null || rl.points == null || rl.points.Length < 2) return;

            // Hero: low over the trail just before a boost pad, looking down the line.
            float at = 230f;
            foreach (var pad in Object.FindObjectsByType<BoostPad>(FindObjectsSortMode.None)) { at = pad.distance - 16f; break; }
            Vector3 eye = rl.PointAt(at) + Vector3.up * 1.7f - rl.Right(rl.IndexAt(at)) * 1.2f;
            Vector3 look = rl.PointAt(at + 26f) + Vector3.up * 0.6f;
            var camGo = new GameObject("Hero Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 62f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 1500f;
            camGo.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
            var data = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(camGo);
            rt.Release();
            SaveSprite(tex, UIArt + "T_Trail_RidgeLine.png");

            // Map: the race line as one clean white stroke on clear, fitted into a square.
            const int size = 512, pad2 = 36;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            foreach (var p in rl.points) { min = Vector2.Min(min, new Vector2(p.x, p.z)); max = Vector2.Max(max, new Vector2(p.x, p.z)); }
            float scale = (size - pad2 * 2) / Mathf.Max(max.x - min.x, max.y - min.y);
            Vector2 offset = new Vector2(size, size) * 0.5f - (min + max) * 0.5f * scale;
            var alpha = new float[size * size];
            int n = rl.points.Length;
            for (int i = 0; i < n; i++)
            {
                Vector3 a3 = rl.points[i], b3 = rl.points[(i + 1) % n];
                Vector2 a = new Vector2(a3.x, a3.z) * scale + offset, b = new Vector2(b3.x, b3.z) * scale + offset;
                int steps = Mathf.CeilToInt(Vector2.Distance(a, b)) + 1;
                for (int s = 0; s <= steps; s++)
                {
                    Vector2 c = Vector2.Lerp(a, b, s / (float)steps);
                    for (int dy = -5; dy <= 5; dy++)
                        for (int dx = -5; dx <= 5; dx++)
                        {
                            int x = Mathf.RoundToInt(c.x) + dx, y = Mathf.RoundToInt(c.y) + dy;
                            if (x < 0 || y < 0 || x >= size || y >= size) continue;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            alpha[y * size + x] = Mathf.Max(alpha[y * size + x], Mathf.Clamp01(4.5f - d));
                        }
                }
            }
            var map = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, (byte)(alpha[i] * 255));
            map.SetPixels32(px);
            map.Apply();
            SaveSprite(map, UIArt + "T_TrailMap_RidgeLine.png");
        }

        // ------------------------------------------------------------------ tune

        static Animator BuildTune(Transform canvas, MenuRouter router, Showroom showroom)
        {
            // A copy of the panel the garage is made from, for the same show/hide animation.
            var src = canvas.Find("Main Content/Shop");
            var panel = Object.Instantiate(src.gameObject, src.parent).transform;
            panel.SetSiblingIndex(src.GetSiblingIndex() + 1);
            panel.name = "Shop Copy";
            panel = RepurposePanel(canvas, "Shop Copy", MenuRouter.Tune);
            var content = panel.Find("Content");
            Header(content, "TUNE", "STEP 2 / 3   SET UP YOUR BIKE");

            var bikeName = Txt(content, "Bike Name", "SWITCHBACK", fontTitle, 56, Color.white, TextAlignmentOptions.Left);
            Place(bikeName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(124, -262), new Vector2(700, 70));

            // Four sliders down the left: name, eleven slanted notches (stock in the middle), what each end gives.
            var list = new GameObject("Sliders", typeof(RectTransform)).GetComponent<RectTransform>();
            list.SetParent(content, false);
            Place(list, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 1), new Vector2(120, 200), new Vector2(640, 480));

            var selector = Img(list, "Selector", slant, new Color(Magenta.r, Magenta.g, Magenta.b, 0.16f));
            selector.type = Image.Type.Sliced; selector.raycastTarget = false;
            Place(selector.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(-20, -60), new Vector2(620, 100));
            var selEdge = Img(selector.transform, "Edge", slant, Magenta);
            selEdge.type = Image.Type.Sliced; selEdge.raycastTarget = false; selEdge.pixelsPerUnitMultiplier = 4f;
            Place(selEdge.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(14, -16));

            var rows = new TuningPanel.Row[BikeTuning.Count];
            for (int r = 0; r < BikeTuning.Count; r++)
            {
                var s = BikeTuning.Sliders[r];
                var row = new GameObject(s.name, typeof(RectTransform)).GetComponent<RectTransform>();
                row.SetParent(list, false);
                Place(row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(0, -60 - r * 104), new Vector2(600, 100));
                var label = Txt(row, "Label", s.name, fontTitle, 40, Color.white, TextAlignmentOptions.Left);
                Place(label.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(36, 26), new Vector2(400, 50));
                var notches = new Image[BikeTuning.Max + 1];
                for (int k = 0; k <= BikeTuning.Max; k++)
                {
                    notches[k] = Img(row, "Notch " + k, slant, new Color(1, 1, 1, 0.12f));
                    notches[k].type = Image.Type.Sliced; notches[k].pixelsPerUnitMultiplier = 4f;
                    Place(notches[k].rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(54 + k * 46, -12), new Vector2(38, 28));
                }
                var low = Txt(row, "Low", s.low, fontBold, 18, Steel, TextAlignmentOptions.Left);
                low.characterSpacing = 8;
                Place(low.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(36, -40), new Vector2(240, 26));
                var high = Txt(row, "High", s.high, fontBold, 18, Steel, TextAlignmentOptions.Right);
                high.characterSpacing = 8;
                Place(high.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(54 + BikeTuning.Max * 46 + 20, -40), new Vector2(240, 26));
                rows[r] = new TuningPanel.Row { root = row, label = label, low = low, high = high, notches = notches };
            }

            var desc = Txt(content, "Description", "", fontRegular, 26, Steel, TextAlignmentOptions.TopLeft);
            Place(desc.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(124, 290), new Vector2(780, 80));
            var readout = Txt(content, "Readout", "", fontBold, 24, Color.white, TextAlignmentOptions.Left);
            readout.characterSpacing = 6;
            Place(readout.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(124, 166), new Vector2(1100, 40));

            var confirm = SlantButton(content, "Ride On", "RIDE ON", new Vector2(1, 0), new Vector2(-120, 150), new Vector2(330, 64));
            var reset = SlantButton(content, "Stock", "STOCK", new Vector2(1, 0), new Vector2(-470, 150), new Vector2(260, 64));
            reset.GetComponent<Image>().color = new Color(1, 1, 1, 0.14f);
            reset.GetComponentInChildren<TMP_Text>().color = Color.white;

            var tp = panel.gameObject.AddComponent<TuningPanel>();
            var so = new SerializedObject(tp);
            so.FindProperty("visibleGroup").objectReferenceValue = panel.GetComponent<CanvasGroup>();
            so.FindProperty("showroom").objectReferenceValue = showroom;
            so.FindProperty("bikeName").objectReferenceValue = bikeName;
            so.FindProperty("description").objectReferenceValue = desc;
            so.FindProperty("readout").objectReferenceValue = readout;
            so.FindProperty("selector").objectReferenceValue = selector.rectTransform;
            so.FindProperty("resetButton").objectReferenceValue = reset;
            so.FindProperty("confirmButton").objectReferenceValue = confirm;
            so.FindProperty("steel").colorValue = Steel;
            var rp = so.FindProperty("rows");
            rp.arraySize = rows.Length;
            for (int r = 0; r < rows.Length; r++)
            {
                var e = rp.GetArrayElementAtIndex(r);
                e.FindPropertyRelative("root").objectReferenceValue = rows[r].root;
                e.FindPropertyRelative("label").objectReferenceValue = rows[r].label;
                e.FindPropertyRelative("low").objectReferenceValue = rows[r].low;
                e.FindPropertyRelative("high").objectReferenceValue = rows[r].high;
                var np = e.FindPropertyRelative("notches");
                np.arraySize = rows[r].notches.Length;
                for (int k = 0; k < rows[r].notches.Length; k++) np.GetArrayElementAtIndex(k).objectReferenceValue = rows[r].notches[k];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddVoidPersistentListener(tp.onConfirm, router.OpenTracks);

            CloneHotkeys(canvas.Find("Main Content/Home Panel"), content, router, "Back", router.Back);
            return panel.GetComponent<Animator>();
        }
    }
}
