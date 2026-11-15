using System.Collections.Generic;
using System.IO;
using ShiftRush.AirShow;
using ShiftRush.Vehicles;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShiftRush.EditorTools
{
    /// <summary>
    /// Builds Cloudbreak, the Air Show venue: a side-on (2.5D) summit run at sunset. The course runs along +X as a
    /// solid slab of earth cut open towards the camera (strata, a grass lip), kickers with glowing trick boxes
    /// hanging over each gap, a cliff drop and a finale. Behind it the valley falls away under a sea of cloud to
    /// a mountain range, with balloons, wind turbines, a cable car, birds and flags keeping it all moving.
    /// BIKE RUSH → Courses → Build Cloudbreak. Rebuilding regenerates the scene.
    /// </summary>
    public static class AirShowBuilder
    {
        const string Root = "Assets/_ShiftRush/";
        const string ArtDir = Root + "Art/Courses/";
        const string ScenePath = Root + "Scenes/" + ShiftRush.Core.RideModes.AirShowScene + ".unity";
        const string Alpine = "Assets/Synty/PolygonNatureBiomes/PNB_Alpine_Mountain/";
        const string Generic = "Assets/Synty/PolygonGeneric/Prefabs/";
        const string Font = "Assets/Heat - Complete Modern UI/Fonts/RobotoCondensed-BoldItalic SDF.asset";
        const float HalfDepth = 3.5f;   // the slab runs z = -3.5..3.5; the bike rides z = 0
        const float Step = 0.5f;
        const float Bottom = -70f;

        static readonly Color Amber = new Color32(255, 158, 46, 255);
        static readonly Color Magenta = new Color32(255, 64, 217, 255);
        static readonly Color Teal = new Color32(64, 224, 255, 255);

        struct Knot { public float x, y; public Knot(float x, float y) { this.x = x; this.y = y; } }

        class Box { public Vector3 centre, size; public string label; }

        static readonly List<Knot> knots = new List<Knot>();
        static readonly List<Box> boxes = new List<Box>();
        static readonly List<Vector3> checkpoints = new List<Vector3>();
        static readonly List<(float x, float y, float length, float height)> kickers = new List<(float, float, float, float)>();
        static TMP_FontAsset font;

        [MenuItem("BIKE RUSH/Courses/Build Cloudbreak (Air Show)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CourseBuilder.EnsureFolder(ArtDir.TrimEnd('/'));
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Font);

            AssetDatabase.DeleteAsset(ScenePath);
            AssetDatabase.CopyAsset(Root + "Scenes/BikeTest.unity", ScenePath);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var env = GameObject.Find("Environment");
            if (env != null) Object.DestroyImmediate(env);
            foreach (var old in Object.FindObjectsByType<BikeVehicleAdapter>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);

            Random.InitState(41);
            float finishX = Layout();
            var venue = new GameObject("Cloudbreak").transform;

            Slab(venue);
            Kickers(venue);
            TrickBoxes(venue);
            Backdrop(venue, finishX);
            Life(venue, finishX);
            Flags(venue, finishX);
            StartAndFinish(venue, finishX);

            // The run, its checkpoints and the bike.
            var runGo = new GameObject("Air Show");
            var run = runGo.AddComponent<AirShowRun>();
            var cps = new List<Transform>();
            for (int i = 0; i < checkpoints.Count; i++)
            {
                var c = new GameObject("Checkpoint " + i).transform;
                c.SetParent(runGo.transform, false);
                c.position = checkpoints[i];
                cps.Add(c);
            }
            var so = new SerializedObject(run);
            so.FindProperty("finishX").floatValue = finishX;
            so.FindProperty("killY").floatValue = -50f;
            var cp = so.FindProperty("checkpoints");
            cp.arraySize = cps.Count;
            for (int i = 0; i < cps.Count; i++) cp.GetArrayElementAtIndex(i).objectReferenceValue = cps[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            var bikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CourseBuilder.BikePrefab);
            if (bikePrefab != null)
            {
                var bike = (GameObject)PrefabUtility.InstantiatePrefab(bikePrefab);
                bike.name = "Player Bike";
                bike.transform.SetPositionAndRotation(new Vector3(-20f, H(-20f) + 0.7f, 0f), Quaternion.LookRotation(Vector3.right, Vector3.up));
            }

            Camera();
            Sky();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            CourseBuilder.AddToBuild(ScenePath);
            Debug.Log("[BIKE//RUSH] Built Cloudbreak: " + finishX.ToString("0") + " m, " + boxes.Count + " trick boxes.");
        }

        // ------------------------------------------------------------------ the line of the course

        /// <summary>Lays out the ground profile, kickers, boxes and checkpoints. Returns the finish X.</summary>
        static float Layout()
        {
            knots.Clear(); boxes.Clear(); checkpoints.Clear(); kickers.Clear();
            // Start deck on the summit, then the drop-in.
            knots.Add(new Knot(-60f, 16f));
            knots.Add(new Knot(-6f, 16f));
            knots.Add(new Knot(42f, 0f));
            checkpoints.Add(new Vector3(-20f, 16.8f, 0f));
            float x = 64f, y = 0f;
            x = Jump(x, y, 5f, 1.6f, "WARM-UP", 18f);
            x = Jump(x, y, 6f, 2.2f, "STEP IT UP", 18f);
            x = Jump(x, y, 5f, 1.5f, "DOUBLE", 6f);
            x = Jump(x, y, 5f, 1.5f, "DOUBLE", 18f);
            x = Jump(x, y, 6.5f, 3.0f, "BIG BOX", 22f);
            x = Drop(x, ref y);
            x = Jump(x, y, 7f, 3.6f, "FINALE", 30f);
            float finishX = x;
            knots.Add(new Knot(finishX + 160f, y));
            return finishX;
        }

        static float Jump(float x, float y0, float length, float height, string label, float runOut)
        {
            checkpoints.Add(new Vector3(x - 18f, y0 + 1.2f, 0f));
            knots.Add(new Knot(x - 1f, y0));
            kickers.Add((x, y0, length, height));
            float lip = x + length;
            float gap = 4f + 2f * height, table = y0 + 0.85f * height, land = 12f + 8f * height;
            // No pit: the gap stays at ground level and the landing has a rideable face, so coming up short
            // rolls up onto the table instead of ending in a hole.
            knots.Add(new Knot(lip + gap * 0.35f, y0));
            knots.Add(new Knot(lip + gap, table));
            knots.Add(new Knot(lip + gap + 3f, table));
            float end = lip + gap + 3f + land;
            knots.Add(new Knot(end, y0));
            knots.Add(new Knot(end + runOut, y0));
            // The box hangs over the gap, from just past the lip to over the landing.
            float span = gap + 3f + land * 0.55f;
            boxes.Add(new Box
            {
                centre = new Vector3(lip + span * 0.5f + 1f, y0 + 0.9f + (height + 7f) * 0.5f, 0f),
                size = new Vector3(span, height + 7f, 7f),
                label = label,
            });
            return end + runOut;
        }

        /// <summary>A small launch, off a cliff, onto a long falling landing 20 m below.</summary>
        static float Drop(float x, ref float y)
        {
            float y0 = y;
            checkpoints.Add(new Vector3(x - 18f, y0 + 1.2f, 0f));
            knots.Add(new Knot(x - 1f, y0));
            kickers.Add((x, y0, 4f, 1.2f));
            float lip = x + 4f;
            knots.Add(new Knot(lip + 1.5f, y0));
            knots.Add(new Knot(lip + 2.2f, y0 - 6f));   // the cliff face
            float landEnd = lip + 66f;
            knots.Add(new Knot(landEnd, y0 - 20f));
            knots.Add(new Knot(landEnd + 26f, y0 - 20f));
            boxes.Add(new Box
            {
                centre = new Vector3(lip + 20f, y0 + 1.5f, 0f),
                size = new Vector3(34f, 13f, 7f),
                label = "SUMMIT DROP",
            });
            y = y0 - 20f;
            return landEnd + 26f;
        }

        /// <summary>Ground height at x: eased between knots, so every slope starts and ends flat.</summary>
        static float H(float x)
        {
            if (x <= knots[0].x) return knots[0].y;
            for (int i = 0; i < knots.Count - 1; i++)
            {
                var a = knots[i]; var b = knots[i + 1];
                if (x > b.x) continue;
                float t = b.x > a.x ? (x - a.x) / (b.x - a.x) : 1f;
                return Mathf.Lerp(a.y, b.y, t * t * (3f - 2f * t));
            }
            return knots[knots.Count - 1].y;
        }

        /// <summary>The ground a long way behind the course: pits and kickers smoothed out.</summary>
        static float HSmooth(float x)
        {
            float s = 0f; int n = 0;
            for (float d = -24f; d <= 24f; d += 3f) { s += H(x + d); n++; }
            return s / n;
        }

        // ------------------------------------------------------------------ the slab

        static void Slab(Transform parent)
        {
            float x0 = knots[0].x, x1 = knots[knots.Count - 1].x;
            var v = new List<Vector3>(); var uv = new List<Vector2>();
            var top = new List<int>(); var face = new List<int>(); var lip = new List<int>();
            int n = Mathf.CeilToInt((x1 - x0) / Step) + 1;

            int topStart = v.Count;
            for (int i = 0; i < n; i++)
            {
                float x = x0 + i * Step, y = H(x);
                v.Add(new Vector3(x, y, -HalfDepth)); uv.Add(new Vector2(x / 4f, 0f));
                v.Add(new Vector3(x, y, HalfDepth)); uv.Add(new Vector2(x / 4f, 2f * HalfDepth / 4f));
            }
            for (int i = 0; i < n - 1; i++)
            {
                int a = topStart + i * 2, b = a + 1, c = a + 2, d = a + 3;
                top.Add(a); top.Add(b); top.Add(c); top.Add(b); top.Add(d); top.Add(c);
            }

            int faceStart = v.Count;
            for (int i = 0; i < n; i++)
            {
                float x = x0 + i * Step, y = H(x);
                v.Add(new Vector3(x, y - 0.55f, -HalfDepth)); uv.Add(new Vector2(x / 9f, (y - 0.55f) / 9f));
                v.Add(new Vector3(x, Bottom, -HalfDepth)); uv.Add(new Vector2(x / 9f, Bottom / 9f));
            }
            for (int i = 0; i < n - 1; i++)
            {
                int t0 = faceStart + i * 2, b0 = t0 + 1, t1 = t0 + 2, b1 = t0 + 3;
                face.Add(t0); face.Add(t1); face.Add(b0); face.Add(t1); face.Add(b1); face.Add(b0);
            }

            int lipStart = v.Count;
            for (int i = 0; i < n; i++)
            {
                float x = x0 + i * Step, y = H(x);
                v.Add(new Vector3(x, y, -HalfDepth)); uv.Add(new Vector2(x / 3f, 1f));
                v.Add(new Vector3(x, y - 0.55f, -HalfDepth)); uv.Add(new Vector2(x / 3f, 0f));
            }
            for (int i = 0; i < n - 1; i++)
            {
                int t0 = lipStart + i * 2, b0 = t0 + 1, t1 = t0 + 2, b1 = t0 + 3;
                lip.Add(t0); lip.Add(t1); lip.Add(b0); lip.Add(t1); lip.Add(b1); lip.Add(b0);
            }

            var mesh = new Mesh { name = "Cloudbreak_Slab", indexFormat = IndexFormat.UInt32, subMeshCount = 3 };
            mesh.SetVertices(v); mesh.SetUVs(0, uv);
            mesh.SetTriangles(top, 0); mesh.SetTriangles(face, 1); mesh.SetTriangles(lip, 2);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            CourseBuilder.SaveMesh(mesh, "Cloudbreak_Slab");

            var go = new GameObject("Course Slab", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterials = new[] { CourseBuilder.TrailMaterial(), StrataMaterial(), GrassLip() };
            go.isStatic = true;

            // Invisible, frictionless side walls: nothing rides off the front or back of the course.
            var slick = new PhysicsMaterial("Cloudbreak_Wall") { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0f };
            for (int side = -1; side <= 1; side += 2)
            {
                var wall = new GameObject(side < 0 ? "Wall Front" : "Wall Back");
                wall.transform.SetParent(parent, false);
                wall.transform.position = new Vector3((x0 + x1) * 0.5f, 0f, side * (HalfDepth + 0.15f));
                var bc = wall.AddComponent<BoxCollider>();
                bc.size = new Vector3(x1 - x0, 200f, 0.3f);
                bc.sharedMaterial = slick;
                wall.layer = 2; // ground checks look through it
                wall.isStatic = true;
            }
        }

        static Material StrataMaterial()
        {
            string texPath = ArtDir + "T_Strata.png";
            const int w = 256, h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var bands = new[]
            {
                new Color(0.42f, 0.27f, 0.2f), new Color(0.55f, 0.35f, 0.24f), new Color(0.36f, 0.22f, 0.18f),
                new Color(0.62f, 0.42f, 0.28f), new Color(0.47f, 0.3f, 0.23f), new Color(0.3f, 0.19f, 0.17f),
            };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float wob = Mathf.Sin(x / (float)w * Mathf.PI * 4f) * 5f + Mathf.Sin(x / (float)w * Mathf.PI * 10f + 1.3f) * 2f;
                    int band = Mathf.FloorToInt((y + wob + 256f) / 22f) % bands.Length;
                    var c = bands[band];
                    float grain = Mathf.PerlinNoise(x * 0.09f, y * 0.35f) * 0.16f - 0.08f;
                    float crack = Mathf.Abs(Mathf.PerlinNoise(x * 0.05f + 7f, y * 0.05f) - 0.5f) < 0.012f ? -0.12f : 0f;
                    tex.SetPixel(x, y, new Color(c.r + grain + crack, c.g + grain + crack, c.b + grain * 0.8f + crack, 1f));
                }
            File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
            imp.wrapMode = TextureWrapMode.Repeat; imp.filterMode = FilterMode.Bilinear; imp.SaveAndReimport();
            var m = CourseBuilder.SimpleMat("M_Strata", Color.white, Color.black, 0.05f);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material GrassLip() { return CourseBuilder.SimpleMat("M_GrassLip", new Color(0.36f, 0.55f, 0.22f), Color.black, 0.05f); }

        static Material Grass()
        {
            var m = CourseBuilder.SimpleMat("M_ValleyGrass", new Color(0.78f, 0.86f, 0.62f), Color.black, 0.02f);
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(Alpine + "Terrain/Pine_Grass.terrainlayer");
            if (layer != null && layer.diffuseTexture != null) m.SetTexture("_BaseMap", layer.diffuseTexture);
            m.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ kickers and boxes

        static void Kickers(Transform parent)
        {
            var group = new GameObject("Kickers").transform;
            group.SetParent(parent, false);
            var deck = CourseBuilder.SimpleMat("M_AirKickerDeck", new Color(0.5f, 0.34f, 0.2f), Color.black, 0.15f);
            var lipMat = CourseBuilder.SimpleMat("M_AirKickerLip", Amber, Amber * 1.8f, 0.4f);
            var crates = new List<GameObject>();
            for (int i = 1; i <= 3; i++)
            {
                var c = AssetDatabase.LoadAssetAtPath<GameObject>(Generic + "Props/SM_Gen_Prop_Crate_0" + i + ".prefab");
                if (c != null) crates.Add(c);
            }
            for (int k = 0; k < kickers.Count; k++)
            {
                var kk = kickers[k];
                var go = new GameObject("Kicker " + (k + 1), typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                go.transform.SetParent(group, false);
                go.transform.SetPositionAndRotation(new Vector3(kk.x, kk.y + 0.02f, 0f), Quaternion.LookRotation(Vector3.right, Vector3.up));
                var mesh = CourseBuilder.KickerMesh(5.4f, kk.length, kk.height, "Cloudbreak_Kicker" + (k + 1));
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshCollider>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterials = new[] { deck, lipMat };
                go.isStatic = true;
                // A stack of crates behind each kicker: the show's "boxes" on the ground too.
                for (int c = 0; c < 3 && crates.Count > 0; c++)
                {
                    var crate = (GameObject)PrefabUtility.InstantiatePrefab(crates[(k + c) % crates.Count], go.transform.parent);
                    crate.transform.position = new Vector3(kk.x + kk.length - 1.2f - c * 1.3f, kk.y + (c == 2 ? 1.1f : 0f), HalfDepth + 1.2f + (c == 2 ? -0.2f : c * 0.3f));
                    crate.transform.rotation = Quaternion.Euler(0f, Random.Range(-15f, 15f), 0f);
                    if (c == 2) crate.transform.position = new Vector3(kk.x + kk.length - 1.8f, kk.y + 1.05f, HalfDepth + 1.3f);
                }
            }
        }

        static void TrickBoxes(Transform parent)
        {
            var group = new GameObject("Trick Boxes").transform;
            group.SetParent(parent, false);
            var edgeMat = CourseBuilder.SimpleMat("M_TrickBoxEdge", Amber, Amber * 2f, 0.5f);
            var fillMat = Transparent("M_TrickBoxFill", new Color(Amber.r, Amber.g, Amber.b, 0.06f));
            for (int b = 0; b < boxes.Count; b++)
            {
                var box = boxes[b];
                var go = new GameObject("Trick Box " + (b + 1) + " - " + box.label);
                go.transform.SetParent(group, false);
                go.transform.position = box.centre;
                go.layer = 2; // Ignore Raycast: the bike's ground checks see through it
                var col = go.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = box.size;

                var edges = new List<Renderer>();
                Vector3 s = box.size * 0.5f;
                const float t = 0.14f;
                for (int i = -1; i <= 1; i += 2)
                    for (int j = -1; j <= 1; j += 2)
                    {
                        edges.Add(Bar(go.transform, new Vector3(0, i * s.y, j * s.z), new Vector3(box.size.x, t, t), edgeMat));
                        edges.Add(Bar(go.transform, new Vector3(i * s.x, 0, j * s.z), new Vector3(t, box.size.y, t), edgeMat));
                        edges.Add(Bar(go.transform, new Vector3(i * s.x, j * s.y, 0), new Vector3(t, t, box.size.z), edgeMat));
                    }
                var fill = Bar(go.transform, Vector3.zero, box.size, fillMat);
                fill.shadowCastingMode = ShadowCastingMode.Off;

                Text(go.transform, "Label", box.label, new Vector3(-s.x + 0.4f, s.y + 0.85f, -s.z), 6f, Amber, TextAlignmentOptions.Left, new Vector2(30f, 2f));
                Text(go.transform, "Key", "SHIFT IN THE BOX", new Vector3(-s.x + 0.45f, s.y + 0.3f, -s.z), 3f, Color.white, TextAlignmentOptions.Left, new Vector2(30f, 1f));

                var tb = go.AddComponent<TrickBox>();
                var so = new SerializedObject(tb);
                var ep = so.FindProperty("edges");
                ep.arraySize = edges.Count;
                for (int i = 0; i < edges.Count; i++) ep.GetArrayElementAtIndex(i).objectReferenceValue = edges[i];
                so.FindProperty("fill").objectReferenceValue = fill;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static Renderer Bar(Transform parent, Vector3 local, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = size;
            go.layer = 2;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = m;
            return r;
        }

        static Material Transparent(string name, Color c)
        {
            var m = CourseBuilder.SimpleMat(name, c, new Color(c.r, c.g, c.b) * 0.05f, 0.2f);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(m);
            return m;
        }

        static TextMeshPro Text(Transform parent, string name, string text, Vector3 local, float size, Color color, TextAlignmentOptions align, Vector2 rect)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshPro>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.characterSpacing = 4f;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = rect;
            rt.pivot = new Vector2(align == TextAlignmentOptions.Left ? 0f : 0.5f, 0f);
            rt.localPosition = local;
            return t;
        }

        // ------------------------------------------------------------------ the world behind

        static void Backdrop(Transform parent, float finishX)
        {
            var group = new GameObject("Backdrop").transform;
            group.SetParent(parent, false);

            // The valley falling away behind the course.
            float[] rows = { HalfDepth, 5f, 8f, 13f, 21f, 34f, 52f, 78f, 115f, 165f, 230f };
            float x0 = knots[0].x - 260f, x1 = knots[knots.Count - 1].x + 320f;
            int cols = Mathf.CeilToInt((x1 - x0) / 3f) + 1;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < cols; c++)
                {
                    float x = x0 + c * 3f, z = rows[r];
                    v.Add(new Vector3(x, ValleyY(x, z), z));
                    uv.Add(new Vector2(x / 12f, z / 12f));
                }
            for (int r = 0; r < rows.Length - 1; r++)
                for (int c = 0; c < cols - 1; c++)
                {
                    int a = r * cols + c, b = a + 1, d = a + cols, e = d + 1;
                    tris.Add(a); tris.Add(d); tris.Add(b); tris.Add(b); tris.Add(d); tris.Add(e);
                }
            var mesh = new Mesh { name = "Cloudbreak_Valley", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            CourseBuilder.SaveMesh(mesh, "Cloudbreak_Valley");
            var valley = new GameObject("Valley", typeof(MeshFilter), typeof(MeshRenderer));
            valley.transform.SetParent(group, false);
            valley.GetComponent<MeshFilter>().sharedMesh = mesh;
            valley.GetComponent<MeshRenderer>().sharedMaterial = Grass();
            valley.isStatic = true;

            // Pines on the slope behind, thicker further back.
            var pines = Prefabs(Alpine + "Prefabs/SM_Env_Pine_0", 1, 4);
            var mounds = Prefabs(Alpine + "Prefabs/SM_Env_Snow_Mound_0", 1, 3);
            var cliffs = Prefabs(Alpine + "Prefabs/SM_Env_Rock_Cliff_0", 1, 7);
            var trees = new GameObject("Pines").transform; trees.SetParent(group, false);
            for (float x = x0 + 80f; x < x1 - 120f; x += Random.Range(2.5f, 7f))
            {
                float z = Mathf.Lerp(7.5f, 120f, Mathf.Pow(Random.value, 1.6f));
                if (pines.Count == 0) break;
                var p = Place(pines[Random.Range(0, pines.Count)], trees, new Vector3(x, ValleyY(x, z) - 0.3f, z), Random.Range(0.8f, 1.5f) * (1f + z / 140f));
                p.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            var rocks = new GameObject("Rocks and Snow").transform; rocks.SetParent(group, false);
            for (int i = 0; i < 40 && mounds.Count > 0; i++)
            {
                float x = Random.Range(x0 + 100f, x1 - 100f), z = Random.Range(60f, 220f);
                Place(mounds[i % mounds.Count], rocks, new Vector3(x, ValleyY(x, z) - 1f, z), Random.Range(2f, 5f));
            }
            for (int i = 0; i < 14 && cliffs.Count > 0; i++)
            {
                float x = Random.Range(x0 + 120f, x1 - 120f), z = Random.Range(30f, 140f);
                var c = Place(cliffs[i % cliffs.Count], rocks, new Vector3(x, ValleyY(x, z) - 2f, z), Random.Range(1.2f, 2.4f));
                c.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                CourseBuilder.FixBrokenMaterials(c);
            }

            // The range on the horizon.
            var ranges = Prefabs(Alpine + "Prefabs/SM_Env_MountainRange_0", 1, 3);
            var mountains = new GameObject("Mountain Range").transform; mountains.SetParent(group, false);
            for (int i = 0; ranges.Count > 0 && i < 9; i++)
            {
                float x = x0 + 60f + i * (x1 - x0) / 8.5f;
                var m = Place(ranges[i % ranges.Count], mountains, new Vector3(x + Random.Range(-40f, 40f), -95f, Random.Range(520f, 700f)), Random.Range(4f, 6.5f));
                m.transform.rotation = Quaternion.Euler(0f, Random.Range(-25f, 25f) + (i % 2 == 0 ? 0f : 180f), 0f);
                CourseBuilder.FixBrokenMaterials(m);
            }
        }

        /// <summary>Falls away from the course's back edge into the valley, rolling into hills further out.</summary>
        static float ValleyY(float x, float z)
        {
            if (z <= HalfDepth + 0.01f) return H(Mathf.Clamp(x, knots[0].x, knots[knots.Count - 1].x)) - 0.05f;
            float k = Mathf.InverseLerp(HalfDepth, 14f, z);
            float near = Mathf.Lerp(H(Mathf.Clamp(x, knots[0].x, knots[knots.Count - 1].x)), HSmooth(x), k) - 0.4f * k;
            float fall = Mathf.Min(z - HalfDepth, 120f) * 0.42f + Mathf.Max(0f, z - 120f) * 0.1f;
            float hills = (Mathf.PerlinNoise(x * 0.006f + 3f, z * 0.01f) - 0.4f) * Mathf.Clamp01((z - 25f) / 80f) * 50f;
            return near - fall + hills;
        }

        static GameObject Place(GameObject prefab, Transform parent, Vector3 pos, float scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            go.isStatic = true;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            return go;
        }

        static List<GameObject> Prefabs(string prefix, int from, int to)
        {
            var list = new List<GameObject>();
            for (int i = from; i <= to; i++)
            {
                var g = AssetDatabase.LoadAssetAtPath<GameObject>(prefix + i + ".prefab");
                if (g != null) list.Add(g);
            }
            return list;
        }

        // ------------------------------------------------------------------ things that move

        static void Life(Transform parent, float finishX)
        {
            var group = new GameObject("Life").transform;
            group.SetParent(parent, false);
            float x0 = knots[0].x - 300f, x1 = knots[knots.Count - 1].x + 360f;

            // Clouds: a sea of them down in the valley (the summit breaks through), more high up.
            var cloudPrefabs = Prefabs(Generic + "Environment/SM_Gen_Env_Cloud_0", 1, 3);
            var clouds = new GameObject("Clouds").transform; clouds.SetParent(group, false);
            for (int i = 0; i < 60 && cloudPrefabs.Count > 0; i++)
            {
                bool low = i < 34;
                float x = Random.Range(x0, x1);
                float z = low ? Random.Range(40f, 230f) : Random.Range(260f, 480f);
                float y = low ? Random.Range(-46f, -30f) - z * 0.05f : Random.Range(45f, 130f);
                var c = Place(cloudPrefabs[i % cloudPrefabs.Count], clouds, new Vector3(x, y, z), low ? Random.Range(3f, 6f) : Random.Range(5f, 10f));
                c.isStatic = false;
                c.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                var m = c.AddComponent<AmbientMotion>();
                m.kind = AmbientMotion.Kind.Drift; m.speed = low ? Random.Range(0.8f, 2f) : Random.Range(1.5f, 4f);
                m.a = new Vector3(x0, 0, 0); m.b = new Vector3(x1, 0, 0);
            }

            // Hot-air balloons drifting over the valley.
            var cols = new[] { (Amber, Magenta), (Teal, Color.white), (Magenta, Amber), (Color.white, Teal), (Amber, Teal), (Magenta, Color.white) };
            var balloons = new GameObject("Balloons").transform; balloons.SetParent(group, false);
            for (int i = 0; i < 6; i++)
            {
                float x = Mathf.Lerp(x0 + 250f, x1 - 250f, (i + Random.value * 0.6f) / 6f);
                float z = Random.Range(70f, 260f);
                var b = Balloon(balloons, new Vector3(x, Random.Range(18f, 55f), z), cols[i].Item1, cols[i].Item2, i);
                var m = b.AddComponent<AmbientMotion>();
                m.kind = AmbientMotion.Kind.Bob; m.speed = Random.Range(0.25f, 0.45f); m.amount = Random.Range(1.2f, 2.5f);
            }

            // Wind turbines on a far ridge.
            var turbines = new GameObject("Wind Turbines").transform; turbines.SetParent(group, false);
            var white = CourseBuilder.SimpleMat("M_Turbine", new Color(0.93f, 0.93f, 0.95f), Color.black, 0.4f);
            for (int i = 0; i < 7; i++)
            {
                float x = Mathf.Lerp(x0 + 300f, x1 - 200f, i / 6f) + Random.Range(-30f, 30f);
                float z = Random.Range(300f, 380f);
                Turbine(turbines, new Vector3(x, ValleyY(x, Mathf.Min(z, 230f)) - 6f + Random.Range(-4f, 8f), z), white, Random.Range(1.6f, 2.4f));
            }

            // A cable car across the valley.
            CableCar(group, new Vector3(120f, ValleyY(120f, 60f) + 1f, 60f), new Vector3(560f, ValleyY(560f, 160f) + 18f, 160f));

            // Birds.
            var birdMat = CourseBuilder.SimpleMat("M_Bird", new Color(0.12f, 0.1f, 0.14f), Color.black, 0.1f);
            var birds = new GameObject("Birds").transform; birds.SetParent(group, false);
            for (int f = 0; f < 4; f++)
            {
                var flock = new GameObject("Flock " + (f + 1)).transform;
                flock.SetParent(birds, false);
                flock.position = new Vector3(Random.Range(x0, x1), Random.Range(22f, 48f), Random.Range(25f, 110f));
                var wings = new List<Transform>();
                int count = Random.Range(4, 8);
                for (int k = 0; k < count; k++)
                {
                    var bird = new GameObject("Bird", typeof(Transform)).transform;
                    bird.SetParent(flock, false);
                    bird.localPosition = new Vector3(-k * 1.6f - Random.value, (k % 2) * 0.8f + Random.value * 0.4f, (k % 2 == 0 ? 1 : -1) * k * 0.9f);
                    Bar(bird, Vector3.zero, new Vector3(0.7f, 0.14f, 0.16f), birdMat).gameObject.layer = 0;
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var pivot = new GameObject(s < 0 ? "Wing R" : "Wing L").transform;
                        pivot.SetParent(bird, false);
                        var w = Bar(pivot, new Vector3(0f, 0f, s * 0.55f), new Vector3(0.35f, 0.04f, 1.1f), birdMat);
                        w.gameObject.layer = 0;
                        wings.Add(pivot);
                    }
                }
                var m = flock.gameObject.AddComponent<AmbientMotion>();
                m.kind = AmbientMotion.Kind.Fly; m.speed = Random.Range(4f, 8f); m.amount = Random.Range(0.8f, 2f);
                m.a = new Vector3(x0, 0, 0); m.b = new Vector3(x1, 0, 0);
                m.wings = wings.ToArray();
            }
        }

        static GameObject Balloon(Transform parent, Vector3 pos, Color main, Color band, int seed)
        {
            var root = new GameObject("Balloon " + (seed + 1));
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var mMain = CourseBuilder.SimpleMat("M_Balloon_" + ColorUtility.ToHtmlStringRGB(main), main, main * 0.08f, 0.35f);
            var mBand = CourseBuilder.SimpleMat("M_Balloon_" + ColorUtility.ToHtmlStringRGB(band), band, band * 0.08f, 0.35f);
            var basket = CourseBuilder.SimpleMat("M_BalloonBasket", new Color(0.45f, 0.3f, 0.18f), Color.black, 0.1f);
            Shape(root.transform, PrimitiveType.Sphere, new Vector3(0, 0, 0), new Vector3(6f, 7f, 6f), mMain);
            Shape(root.transform, PrimitiveType.Cylinder, new Vector3(0, 0.6f, 0), new Vector3(6.15f, 0.9f, 6.15f), mBand);
            Shape(root.transform, PrimitiveType.Cylinder, new Vector3(0, -2.6f, 0), new Vector3(4.6f, 0.5f, 4.6f), mBand);
            Shape(root.transform, PrimitiveType.Sphere, new Vector3(0, -3.3f, 0), new Vector3(3f, 1.6f, 3f), mMain);
            Shape(root.transform, PrimitiveType.Cube, new Vector3(0, -6.2f, 0), new Vector3(1.4f, 1.1f, 1.4f), basket);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                var rope = Shape(root.transform, PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 0.9f, -4.9f, Mathf.Sin(a) * 0.9f), new Vector3(0.05f, 2.4f, 0.05f), basket);
                rope.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 10f, 0f, -Mathf.Cos(a) * 10f);
            }
            return root;
        }

        static GameObject Shape(Transform parent, PrimitiveType type, Vector3 local, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        static void Turbine(Transform parent, Vector3 foot, Material white, float scale)
        {
            var root = new GameObject("Turbine").transform;
            root.SetParent(parent, false);
            root.position = foot;
            root.localScale = Vector3.one * scale;
            Shape(root, PrimitiveType.Cylinder, new Vector3(0, 9f, 0), new Vector3(0.7f, 9f, 0.7f), white);
            Shape(root, PrimitiveType.Cube, new Vector3(0, 18.2f, 0.6f), new Vector3(1.1f, 1.1f, 2.6f), white);
            var hub = new GameObject("Rotor").transform;
            hub.SetParent(root, false);
            hub.localPosition = new Vector3(0, 18.2f, -0.9f);
            hub.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 120f));
            Shape(hub, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.9f, white);
            for (int i = 0; i < 3; i++)
            {
                var arm = new GameObject("Blade").transform;
                arm.SetParent(hub, false);
                arm.localRotation = Quaternion.Euler(0f, 0f, i * 120f);
                Shape(arm, PrimitiveType.Cube, new Vector3(0f, 4.4f, 0f), new Vector3(0.55f, 8.4f, 0.12f), white);
            }
            var spin = hub.gameObject.AddComponent<AmbientMotion>();
            spin.kind = AmbientMotion.Kind.Spin; spin.axis = Vector3.forward; spin.speed = Random.Range(35f, 70f);
        }

        static void CableCar(Transform parent, Vector3 a, Vector3 b)
        {
            var root = new GameObject("Cable Car").transform;
            root.SetParent(parent, false);
            var steel = CourseBuilder.SimpleMat("M_CableSteel", new Color(0.2f, 0.2f, 0.24f), Color.black, 0.5f);
            var red = CourseBuilder.SimpleMat("M_Gondola", new Color(0.86f, 0.2f, 0.2f), Color.black, 0.5f);
            var window = CourseBuilder.SimpleMat("M_GondolaWindow", new Color(1f, 0.85f, 0.55f), new Color(1f, 0.7f, 0.35f) * 1.6f, 0.8f);
            const float towerH = 16f, sag = 5f;
            Vector3 ta = a + Vector3.up * towerH, tb = b + Vector3.up * towerH;
            foreach (var foot in new[] { a, b })
            {
                Shape(root, PrimitiveType.Cube, foot + Vector3.up * towerH * 0.5f, new Vector3(0.8f, towerH, 0.8f), steel);
                Shape(root, PrimitiveType.Cube, foot + Vector3.up * towerH, new Vector3(0.5f, 0.5f, 4f), steel);
            }
            var line = root.gameObject.AddComponent<LineRenderer>();
            line.sharedMaterial = steel;
            line.widthMultiplier = 0.12f;
            line.positionCount = 32;
            for (int i = 0; i < 32; i++)
            {
                float k = i / 31f;
                line.SetPosition(i, Vector3.Lerp(ta, tb, k) + Vector3.down * Mathf.Sin(k * Mathf.PI) * sag);
            }
            for (int g = 0; g < 2; g++)
            {
                var car = new GameObject("Gondola " + (g + 1)).transform;
                car.SetParent(root, false);
                Shape(car, PrimitiveType.Cube, new Vector3(0, -1.2f, 0), new Vector3(0.12f, 2.4f, 0.12f), steel);
                Shape(car, PrimitiveType.Cube, new Vector3(0, -3.4f, 0), new Vector3(2.6f, 2.2f, 2f), red);
                Shape(car, PrimitiveType.Cube, new Vector3(0, -3.1f, -1.01f), new Vector3(2.2f, 0.8f, 0.02f), window);
                var m = car.gameObject.AddComponent<AmbientMotion>();
                m.kind = AmbientMotion.Kind.Shuttle; m.a = ta; m.b = tb; m.amount = sag; m.speed = 6f;
                car.position = g == 0 ? ta : tb;
            }
            // Second gondola starts at the other end: AmbientMotion's phase comes from the start position.
        }

        static void Flags(Transform parent, float finishX)
        {
            var group = new GameObject("Flags").transform;
            group.SetParent(parent, false);
            var pole = CourseBuilder.SimpleMat("M_FlagPole", new Color(0.85f, 0.85f, 0.88f), Color.black, 0.4f);
            var colours = new[] { Amber, Magenta, Color.white, Teal };
            int n = 0;
            var spots = new List<float> { -40f, -28f, -16f, -4f };
            foreach (var k in kickers) { spots.Add(k.x - 6f); spots.Add(k.x + k.length + 2f); }
            for (float x = finishX - 20f; x <= finishX + 20f; x += 8f) spots.Add(x);
            foreach (float x in spots)
            {
                var root = new GameObject("Flag").transform;
                root.SetParent(group, false);
                root.position = new Vector3(x, H(x), HalfDepth + 0.6f);
                Shape(root, PrimitiveType.Cylinder, new Vector3(0, 2.5f, 0), new Vector3(0.08f, 2.5f, 0.08f), pole);
                var pivot = new GameObject("Cloth").transform;
                pivot.SetParent(root, false);
                pivot.localPosition = new Vector3(0, 4.5f, 0);
                var c = colours[n++ % colours.Length];
                var cloth = Shape(pivot, PrimitiveType.Cube, new Vector3(0.75f, 0f, 0f), new Vector3(1.5f, 0.9f, 0.03f),
                    CourseBuilder.SimpleMat("M_Flag_" + ColorUtility.ToHtmlStringRGB(c), c, c * 0.3f, 0.2f));
                var m = pivot.gameObject.AddComponent<AmbientMotion>();
                m.kind = AmbientMotion.Kind.Flutter; m.axis = Vector3.up; m.speed = Random.Range(1.2f, 1.9f); m.amount = Random.Range(14f, 24f);
            }
        }

        static void StartAndFinish(Transform parent, float finishX)
        {
            var group = new GameObject("Start and Finish").transform;
            group.SetParent(parent, false);
            var post = CourseBuilder.SimpleMat("M_ArchPost", new Color(0.12f, 0.1f, 0.16f), Color.black, 0.3f);
            var glow = CourseBuilder.SimpleMat("M_AirArchGlow", Amber, Amber * 2.4f, 0.5f);

            Arch(group, -9f, "DROP IN", post, glow, 6.5f);
            Arch(group, finishX, "FINISH", post, glow, 7.5f);

            // The show's sign on the summit, behind the start deck.
            float sx = -22f, sy = H(sx) - 9f;
            var sign = new GameObject("Show Sign").transform;
            sign.SetParent(group, false);
            sign.position = new Vector3(sx, sy, HalfDepth + 34f);
            CourseBuilder.Cube(sign, "Leg", sign.position + new Vector3(-7f, 3.5f, 0.3f), new Vector3(0.4f, 7f, 0.4f), Vector3.forward, post);
            CourseBuilder.Cube(sign, "Leg", sign.position + new Vector3(7f, 3.5f, 0.3f), new Vector3(0.4f, 7f, 0.4f), Vector3.forward, post);
            CourseBuilder.Cube(sign, "Board", sign.position + new Vector3(0f, 8.6f, 0.3f), new Vector3(17f, 4.6f, 0.4f), Vector3.forward, post);
            CourseBuilder.Cube(sign, "Glow", sign.position + new Vector3(0f, 6.45f, 0.05f), new Vector3(16.4f, 0.16f, 0.05f), Vector3.forward, glow);
            Text(sign, "Title", "CLOUDBREAK", new Vector3(0f, 8.75f, 0.05f), 30f, Color.white, TextAlignmentOptions.Center, new Vector2(17f, 3.2f));
            Text(sign, "Sub", "AIR SHOW  //  SUMMIT SESSIONS", new Vector3(0f, 7.25f, 0.05f), 9f, Amber, TextAlignmentOptions.Center, new Vector2(17f, 1.4f));
        }

        static void Arch(Transform parent, float x, string label, Material post, Material glow, float height)
        {
            float y = H(x);
            var g = new GameObject(label + " Arch").transform;
            g.SetParent(parent, false);
            g.position = new Vector3(x, y, 0f);
            for (int s = -1; s <= 1; s += 2)
            {
                var p = CourseBuilder.Cube(g, "Post", new Vector3(x, y + height * 0.5f, s * (HalfDepth + 0.9f)), new Vector3(0.5f, height, 0.5f), Vector3.right, post);
                Object.DestroyImmediate(p.GetComponent<BoxCollider>());
                var gl = CourseBuilder.Cube(g, "Post Glow", new Vector3(x - 0.27f, y + height * 0.5f, s * (HalfDepth + 0.9f)), new Vector3(0.05f, height - 0.4f, 0.3f), Vector3.right, glow);
                Object.DestroyImmediate(gl.GetComponent<BoxCollider>());
            }
            var banner = CourseBuilder.Cube(g, "Banner", new Vector3(x, y + height + 0.3f, 0f), new Vector3(2f * HalfDepth + 2.4f, 1.4f, 0.5f), Vector3.right, post); // local x is world z here
            Object.DestroyImmediate(banner.GetComponent<BoxCollider>());
            var stripe = CourseBuilder.Cube(g, "Banner Glow", new Vector3(x, y + height - 0.32f, -HalfDepth - 1.05f), new Vector3(2.2f, 0.12f, 0.05f), Vector3.right, glow);
            Object.DestroyImmediate(stripe.GetComponent<BoxCollider>());
            // Readable from the side-on camera: the text sits on the near post, facing it.
            Text(g, "Label", label, new Vector3(0f, height + 1.3f, -HalfDepth - 1.3f), 7f, Color.white, TextAlignmentOptions.Center, new Vector2(10f, 1.6f));
        }

        // ------------------------------------------------------------------ camera, light, sky

        static void Camera()
        {
            var cam = Object.FindFirstObjectByType<UnityEngine.Camera>();
            if (cam == null) return;
            foreach (var c in cam.GetComponents<MonoBehaviour>())
                if (c is SBPScripts.BicycleCamera || c.GetType().Name == "BikeSpeedFeel") Object.DestroyImmediate(c);
            if (cam.GetComponent<AirShowCamera>() == null) cam.gameObject.AddComponent<AirShowCamera>();
            cam.farClipPlane = 2200f;
            cam.nearClipPlane = 0.3f;
            cam.fieldOfView = 38f;
            cam.transform.position = new Vector3(-16f, 20f, -16f);
            cam.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }

        static void Sky()
        {
            string path = ArtDir + "M_SunsetSky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null) { sky = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(sky, path); }
            sky.SetFloat("_SunDisk", 2f);
            sky.SetFloat("_SunSize", 0.06f);
            sky.SetFloat("_SunSizeConvergence", 4f);
            sky.SetFloat("_AtmosphereThickness", 2.1f);
            sky.SetColor("_SkyTint", new Color(0.5f, 0.5f, 0.5f));
            sky.SetColor("_GroundColor", new Color(0.42f, 0.34f, 0.4f));
            sky.SetFloat("_Exposure", 1.1f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Skybox;

            var sun = GameObject.Find("Directional Light");
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(11f, -28f, 0f); // low, behind-left: long shadows, warm rim
                var l = sun.GetComponent<Light>();
                if (l != null) { l.color = new Color(1f, 0.72f, 0.5f); l.intensity = 1.5f; }
                RenderSettings.sun = l;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.93f, 0.7f, 0.6f);
            RenderSettings.fogStartDistance = 160f;
            RenderSettings.fogEndDistance = 1700f;
        }
    }
}
