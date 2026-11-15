using System.Collections.Generic;
using ShiftRush.Race;
using ShiftRush.Vehicles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShiftRush.EditorTools
{
    /// <summary>
    /// Builds the BIKE//RUSH courses. A course is a loop of control points (x, height, z) smoothed into a trail
    /// ribbon, a terrain carved around it, kickers and drops for stunts, Synty Alpine scenery, a race line with
    /// gates, and the bikes. BIKE RUSH → Courses → Build Ridge Line. Rebuilding regenerates the scene.
    /// The scene starts as a copy of BikeTest (camera, light, bike), so the bike and camera set-up carry over.
    /// </summary>
    public static class CourseBuilder
    {
        const string Root = "Assets/_ShiftRush/";
        const string ArtDir = Root + "Art/Courses/";
        const string Alpine = "Assets/Synty/PolygonNatureBiomes/PNB_Alpine_Mountain/";
        const string BikeSource = "Assets/Simple Bicycle Physics/Prefabs/Mountain Bike MTBRider2.prefab";
        public const string BikePrefab = Root + "Prefabs/Bikes/BR_Bike_MTB.prefab";

        const float Step = 2f;          // metres between centreline samples
        const float HalfWidth = 4.5f;   // half the trail: six bikes, three abreast
        const float Shoulder = 1.4f;

        // ------------------------------------------------------------------ course definitions

        public class Jump
        {
            public Vector3 near;        // a point the jump sits nearest to (x, ignored, z)
            public float along;         // metres further along the line from there
            public float lateral, width, length, height;
        }

        public class Spec
        {
            public string name, scene;
            public Vector3[] points;    // x, height, z — race direction, point 0 on the start line
            public int[] sharp;         // segments (from that point to the next) kept steep: drops
            public Jump[] jumps;
            public int laps = 2;
            public int seed = 7;
        }

        public static Spec RidgeLine()
        {
            return new Spec
            {
                name = "Ridge Line", scene = Root + "Scenes/Course_RidgeLine.unity", laps = 2, seed = 23,
                points = new[]
                {
                    new Vector3(-60, 28, -20),  // 0  start, heading north up the west ridge
                    new Vector3(-60, 30, 50),
                    new Vector3(-35, 30.5f, 120),
                    new Vector3(30, 30, 150),   // 3  north ridge, rolling east
                    new Vector3(110, 29, 155),
                    new Vector3(180, 27, 140),
                    new Vector3(230, 25.5f, 95),
                    new Vector3(240, 24.5f, 40),
                    new Vector3(232, 24, 0),    // 8  the cliff edge
                    new Vector3(229, 16, -6),   // 9  foot of the face
                    new Vector3(220, 13.5f, -30),
                    new Vector3(205, 13, -60),
                    new Vector3(165, 13, -95),  // 12 valley floor
                    new Vector3(100, 15, -110),
                    new Vector3(30, 19, -110),  // 14 climbing back west
                    new Vector3(-30, 23, -100),
                    new Vector3(-60, 25.5f, -75),
                },
                sharp = new[] { 8 },
                jumps = new[]
                {
                    new Jump { near = new Vector3(110, 0, 155), along = 0f, lateral = -2f, width = 4.2f, length = 6f, height = 1.8f },
                    new Jump { near = new Vector3(232, 0, 0), along = -4f, lateral = 0f, width = 8.6f, length = 3.6f, height = 0.7f },
                    new Jump { near = new Vector3(100, 0, -110), along = 0f, lateral = 2f, width = 4.2f, length = 6f, height = 1.8f },
                    new Jump { near = new Vector3(30, 0, -110), along = 0f, lateral = 0f, width = 5f, length = 7f, height = 2.4f },
                },
            };
        }

        [MenuItem("BIKE RUSH/Courses/Build Ridge Line")]
        public static void BuildRidgeLine() { Build(RidgeLine()); }

        // ------------------------------------------------------------------ build

        public static void Build(Spec spec)
        {
            EnsureFolder(ArtDir.TrimEnd('/'));
            EnsureFolder(Root + "Prefabs/Bikes");
            var bikePrefab = MakeBikePrefab();
            MakeRivalField(bikePrefab);

            // Start from the bike test scene: camera, light, bike already set up.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.DeleteAsset(spec.scene);
            AssetDatabase.CopyAsset(Root + "Scenes/BikeTest.unity", spec.scene);
            var scene = EditorSceneManager.OpenScene(spec.scene, OpenSceneMode.Single);
            var env = GameObject.Find("Environment");
            if (env != null) Object.DestroyImmediate(env);

            Random.InitState(spec.seed);
            var line = Centreline(spec);
            var course = new GameObject(spec.name);
            string key = spec.name.Replace(" ", "");

            var trail = TrailMesh(line, key);
            var trailGo = new GameObject("Trail", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            trailGo.transform.SetParent(course.transform, false);
            trailGo.GetComponent<MeshFilter>().sharedMesh = trail;
            trailGo.GetComponent<MeshCollider>().sharedMesh = trail;
            trailGo.GetComponent<MeshRenderer>().sharedMaterial = TrailMaterial();
            trailGo.isStatic = true;

            var terrain = MakeTerrain(line, key, course.transform);
            MakeJumps(spec, line, course.transform, key);
            Scenery(line, terrain, course.transform);
            Arrows(line, terrain, course.transform);
            Markers(line, terrain, course.transform);
            StartArch(line, course.transform);
            var raceLine = MakeRaceLine(line, spec, course.transform);

            // The player's bike on the line (RaceGrid moves it to its slot when the race starts).
            foreach (var old in Object.FindObjectsByType<BikeVehicleAdapter>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(bikePrefab);
            player.name = "Player Bike";
            player.transform.SetPositionAndRotation(line.points[line.points.Length - 4] + Vector3.up * 0.6f, Quaternion.LookRotation(raceLine.Direction(0)));
            var cam = Object.FindFirstObjectByType<SBPScripts.BicycleCamera>();
            if (cam != null) { cam.target = player.transform; cam.transform.position = player.transform.position - player.transform.forward * 4f + Vector3.up * 2.5f; }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.9f);
            RenderSettings.fogStartDistance = 120f;
            RenderSettings.fogEndDistance = 620f;
            var sun = GameObject.Find("Directional Light");
            if (sun != null) sun.transform.rotation = Quaternion.Euler(38f, -50f, 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddToBuild(spec.scene);
            Debug.Log("[BIKE//RUSH] Built " + spec.name + ": " + raceLine.length.ToString("0") + " m a lap, " + spec.jumps.Length + " jumps.");
        }

        // ------------------------------------------------------------------ centreline

        public class Line
        {
            public Vector3[] points;   // with height
            public float[] distances;
            public float length;
            public Vector3 Dir(int i)
            {
                int n = points.Length;
                return (points[(i + 1) % n] - points[(i - 1 + n) % n]).normalized;
            }
            public Vector3 FlatDir(int i) { var d = Dir(i); d.y = 0f; return d.normalized; }
            public Vector3 Right(int i) { return Vector3.Cross(Vector3.up, FlatDir(i)).normalized; }
            public int Nearest(Vector3 p)
            {
                int best = 0; float bd = float.MaxValue;
                for (int i = 0; i < points.Length; i++)
                {
                    float dx = points[i].x - p.x, dz = points[i].z - p.z, d = dx * dx + dz * dz;
                    if (d < bd) { bd = d; best = i; }
                }
                return best;
            }
        }

        // Closed Catmull-Rom through the points in x/z; height runs straight between points, then is smoothed
        // except across sharp (drop) segments. Resampled every Step metres.
        static Line Centreline(Spec spec)
        {
            var cp = spec.points;
            int n = cp.Length;
            var fine = new List<Vector3>();
            var sharpFlag = new List<bool>();
            var sharp = new HashSet<int>(spec.sharp ?? new int[0]);
            const int sub = 60;
            for (int s = 0; s < n; s++)
            {
                Vector3 p0 = cp[(s - 1 + n) % n], p1 = cp[s], p2 = cp[(s + 1) % n], p3 = cp[(s + 2) % n];
                for (int k = 0; k < sub; k++)
                {
                    float t = k / (float)sub;
                    var xz = CatmullRom(Flat(p0), Flat(p1), Flat(p2), Flat(p3), t);
                    xz.y = Mathf.Lerp(p1.y, p2.y, t);
                    fine.Add(xz);
                    sharpFlag.Add(sharp.Contains(s) || sharp.Contains((s - 1 + n) % n) && t < 0.15f || sharp.Contains((s + 1) % n) && t > 0.85f);
                }
            }
            // Resample evenly by distance along the ground.
            var pts = new List<Vector3>();
            var keep = new List<bool>();
            float acc = 0f, next = 0f;
            for (int i = 0; i < fine.Count; i++)
            {
                var a = fine[i]; var b = fine[(i + 1) % fine.Count];
                float seg = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                while (next <= acc + seg && seg > 0f)
                {
                    float t = (next - acc) / seg;
                    pts.Add(Vector3.Lerp(a, b, t));
                    keep.Add(sharpFlag[i]);
                    next += Step;
                }
                acc += seg;
            }
            // Smooth the height (about ±12 m) so the trail rolls instead of kinking, but leave drops steep.
            int m = pts.Count;
            var y = new float[m];
            for (int i = 0; i < m; i++)
            {
                if (keep[i]) { y[i] = pts[i].y; continue; }
                float sum = 0f, w = 0f;
                for (int k = -6; k <= 6; k++)
                {
                    int j = (i + k + m) % m;
                    if (keep[j]) continue;
                    float wk = 1f - Mathf.Abs(k) / 7f;
                    sum += pts[j].y * wk; w += wk;
                }
                y[i] = w > 0f ? sum / w : pts[i].y;
            }
            var line = new Line { points = new Vector3[m], distances = new float[m] };
            float dist = 0f;
            for (int i = 0; i < m; i++)
            {
                line.points[i] = new Vector3(pts[i].x, y[i], pts[i].z);
                if (i > 0) dist += Vector3.Distance(Flat(line.points[i - 1]), Flat(line.points[i]));
                line.distances[i] = dist;
            }
            line.length = dist + Vector3.Distance(Flat(line.points[m - 1]), Flat(line.points[0]));
            return line;
        }

        static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            // Centripetal, so tight spacing (the cliff) doesn't loop.
            float t0 = 0f, t1 = t0 + Mathf.Pow(Vector3.Distance(p0, p1), 0.5f), t2 = t1 + Mathf.Pow(Vector3.Distance(p1, p2), 0.5f), t3 = t2 + Mathf.Pow(Vector3.Distance(p2, p3), 0.5f);
            if (t1 - t0 < 1e-4f) t1 = t0 + 1e-4f; if (t2 - t1 < 1e-4f) t2 = t1 + 1e-4f; if (t3 - t2 < 1e-4f) t3 = t2 + 1e-4f;
            float u = Mathf.Lerp(t1, t2, t);
            Vector3 a1 = (t1 - u) / (t1 - t0) * p0 + (u - t0) / (t1 - t0) * p1;
            Vector3 a2 = (t2 - u) / (t2 - t1) * p1 + (u - t1) / (t2 - t1) * p2;
            Vector3 a3 = (t3 - u) / (t3 - t2) * p2 + (u - t2) / (t3 - t2) * p3;
            Vector3 b1 = (t2 - u) / (t2 - t0) * a1 + (u - t0) / (t2 - t0) * a2;
            Vector3 b2 = (t3 - u) / (t3 - t1) * a2 + (u - t1) / (t3 - t1) * a3;
            return (t2 - u) / (t2 - t1) * b1 + (u - t1) / (t2 - t1) * b2;
        }

        // ------------------------------------------------------------------ trail

        static Mesh TrailMesh(Line line, string key)
        {
            int n = line.points.Length;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            // Across: outer shoulder, edge, centre-left, centre-right, edge, outer shoulder.
            float[] off = { -HalfWidth - Shoulder, -HalfWidth, HalfWidth, HalfWidth + Shoulder };
            float[] drop = { -0.45f, 0f, 0f, -0.45f };
            for (int i = 0; i <= n; i++)
            {
                int k = i % n;
                var p = line.points[k]; var r = line.Right(k);
                float d = i == n ? line.length : line.distances[k];
                for (int c = 0; c < off.Length; c++)
                {
                    v.Add(p + r * off[c] + Vector3.up * (0.06f + drop[c]));
                    uv.Add(new Vector2((off[c] + HalfWidth + Shoulder) / 4f, d / 4f));
                }
            }
            int w = off.Length;
            for (int i = 0; i < n; i++)
                for (int c = 0; c < w - 1; c++)
                {
                    int a = i * w + c, b = a + 1, a2 = a + w, b2 = b + w;
                    tri.Add(a); tri.Add(a2); tri.Add(b);
                    tri.Add(b); tri.Add(a2); tri.Add(b2);
                }
            var mesh = new Mesh { name = key + "_Trail", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return SaveMesh(mesh, key + "_Trail");
        }

        static Material TrailMaterial()
        {
            string path = ArtDir + "M_Trail.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(Alpine + "Terrain/Pine_Dirt.terrainlayer");
            if (layer != null && layer.diffuseTexture != null) m.SetTexture("_BaseMap", layer.diffuseTexture);
            m.SetColor("_BaseColor", new Color(0.86f, 0.74f, 0.6f));
            m.SetFloat("_Smoothness", 0.05f);
            m.SetTextureScale("_BaseMap", new Vector2(2f, 2f)); // finer grain streaming past = more sense of speed
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ jumps

        static void MakeJumps(Spec spec, Line line, Transform parent, string key)
        {
            var group = new GameObject("Jumps").transform;
            group.SetParent(parent, false);
            var deck = SimpleMat("M_KickerDeck", new Color(0.42f, 0.29f, 0.18f), Color.black, 0.15f);
            var lip = SimpleMat("M_KickerLip", new Color(1f, 0.25f, 0.85f), new Color(1f, 0.15f, 0.8f) * 1.6f, 0.4f);
            for (int j = 0; j < spec.jumps.Length; j++)
            {
                var jp = spec.jumps[j];
                int i0 = line.Nearest(jp.near);
                float d = line.distances[i0] + jp.along - jp.length; // the lip sits at the named spot
                int i = IndexAt(line, d);
                var p = line.points[i];
                Vector3 fwd = line.Dir(i), right = line.Right(i);
                var up = Vector3.Cross(fwd, right).normalized;
                var go = new GameObject("Kicker " + (j + 1), typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                go.transform.SetParent(group, false);
                go.transform.SetPositionAndRotation(p + right * jp.lateral + up * 0.05f, Quaternion.LookRotation(fwd, up));
                var mesh = KickerMesh(jp.width, jp.length, jp.height, key + "_Kicker" + (j + 1));
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshCollider>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterials = new[] { deck, lip };
                go.isStatic = true;
            }
        }

        static int IndexAt(Line line, float d)
        {
            d = Mathf.Repeat(d, line.length);
            for (int i = line.points.Length - 1; i >= 0; i--) if (line.distances[i] <= d) return i;
            return 0;
        }

        // A curved wedge: flat run-in steepening to the lip (y = h·(s/L)^1.6), sides and a back face.
        // Submesh 0 the deck, submesh 1 a painted stripe along the lip.
        static Mesh KickerMesh(float width, float length, float height, string name)
        {
            const int seg = 12;
            var v = new List<Vector3>(); var uv = new List<Vector2>();
            var deck = new List<int>(); var stripe = new List<int>();
            float hw = width * 0.5f;
            System.Func<float, float> Y = s => height * Mathf.Pow(s / length, 1.6f);
            // Top surface
            for (int k = 0; k <= seg; k++)
            {
                float s = length * k / seg;
                v.Add(new Vector3(-hw, Y(s), s)); uv.Add(new Vector2(0, s));
                v.Add(new Vector3(hw, Y(s), s)); uv.Add(new Vector2(1, s));
            }
            for (int k = 0; k < seg; k++)
            {
                int a = k * 2, b = a + 1, c = a + 2, d = a + 3;
                var list = k >= seg - 1 ? stripe : deck;
                list.Add(a); list.Add(c); list.Add(b);
                list.Add(b); list.Add(c); list.Add(d);
            }
            // Sides
            for (int side = -1; side <= 1; side += 2)
            {
                int start = v.Count;
                for (int k = 0; k <= seg; k++)
                {
                    float s = length * k / seg;
                    v.Add(new Vector3(side * hw, Y(s), s)); uv.Add(new Vector2(s, Y(s)));
                    v.Add(new Vector3(side * hw, -0.3f, s)); uv.Add(new Vector2(s, 0));
                }
                for (int k = 0; k < seg; k++)
                {
                    int a = start + k * 2, b = a + 1, c = a + 2, d = a + 3;
                    if (side < 0) { deck.Add(a); deck.Add(b); deck.Add(c); deck.Add(c); deck.Add(b); deck.Add(d); }
                    else { deck.Add(a); deck.Add(c); deck.Add(b); deck.Add(c); deck.Add(d); deck.Add(b); }
                }
            }
            // Back face (under the lip)
            int bb = v.Count;
            v.Add(new Vector3(-hw, height, length)); v.Add(new Vector3(hw, height, length));
            v.Add(new Vector3(-hw, -0.3f, length)); v.Add(new Vector3(hw, -0.3f, length));
            uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0));
            stripe.Add(bb); stripe.Add(bb + 1); stripe.Add(bb + 2); stripe.Add(bb + 2); stripe.Add(bb + 1); stripe.Add(bb + 3);

            var mesh = new Mesh { name = name, subMeshCount = 2 };
            mesh.SetVertices(v); mesh.SetUVs(0, uv);
            mesh.SetTriangles(deck, 0); mesh.SetTriangles(stripe, 1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return SaveMesh(mesh, name);
        }

        // ------------------------------------------------------------------ terrain

        const float TerrainSize = 760f;
        const int HeightRes = 513;
        const float TerrainHeight = 140f;

        static Terrain MakeTerrain(Line line, string key, Transform parent)
        {
            // Centre the terrain on the course.
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            foreach (var p in line.points) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
            Vector3 centre = (min + max) * 0.5f;
            Vector3 origin = new Vector3(centre.x - TerrainSize * 0.5f, 0f, centre.z - TerrainSize * 0.5f);

            var data = new TerrainData { heightmapResolution = HeightRes, alphamapResolution = 256 };
            data.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
            float cell = TerrainSize / (HeightRes - 1);
            var h = new float[HeightRes, HeightRes];
            var nearDist = new float[HeightRes, HeightRes];
            float band = HalfWidth + Shoulder + 0.5f;
            float noiseX = Random.Range(0f, 1000f), noiseZ = Random.Range(0f, 1000f);
            var pts = line.points;
            for (int z = 0; z < HeightRes; z++)
                for (int x = 0; x < HeightRes; x++)
                {
                    float wx = origin.x + x * cell, wz = origin.z + z * cell;
                    // Nearest point on the trail (segments), with its height.
                    float best = float.MaxValue, roadY = 0f;
                    for (int i = 0; i < pts.Length; i += 2)
                    {
                        float dx = pts[i].x - wx, dz = pts[i].z - wz, d = dx * dx + dz * dz;
                        if (d < best) { best = d; roadY = pts[i].y; }
                    }
                    best = Mathf.Sqrt(best);
                    nearDist[z, x] = best;
                    // Rolling hills either side of the trail, rising to mountains far out.
                    float n = Mathf.PerlinNoise(noiseX + wx * 0.011f, noiseZ + wz * 0.011f);
                    float n2 = Mathf.PerlinNoise(noiseX + 300f + wx * 0.04f, noiseZ + wz * 0.04f);
                    float away = Mathf.Max(0f, best - band);
                    float natural = roadY + away * Mathf.Lerp(-0.22f, 0.55f, n) + (n2 - 0.5f) * 3f * Mathf.Clamp01(away / 12f);
                    natural += Mathf.Max(0f, best - 170f) * 0.45f;
                    float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(band, band + 16f, best));
                    float y = Mathf.Lerp(roadY - 0.35f, natural, blend);
                    h[z, x] = Mathf.Clamp01(Mathf.Max(0.5f, y) / TerrainHeight);
                }
            data.SetHeights(0, 0, h);

            // Paint: grass, dirt by the trail, rock on the steep, moss in patches.
            var layers = new List<TerrainLayer>();
            string t = Alpine + "Terrain/";
            foreach (var name in new[] { "Grass_01", "Pine_Dirt", "RiverRocks_02", "Pine_Moss_01" })
            {
                var l = AssetDatabase.LoadAssetAtPath<TerrainLayer>(t + name + ".terrainlayer");
                if (l != null) layers.Add(l);
            }
            if (layers.Count == 4)
            {
                data.terrainLayers = layers.ToArray();
                int ar = data.alphamapResolution;
                var a = new float[ar, ar, 4];
                for (int z = 0; z < ar; z++)
                    for (int x = 0; x < ar; x++)
                    {
                        float fx = x / (float)(ar - 1), fz = z / (float)(ar - 1);
                        float steep = data.GetSteepness(fx, fz);
                        float dist = nearDist[Mathf.RoundToInt(fz * (HeightRes - 1)), Mathf.RoundToInt(fx * (HeightRes - 1))];
                        float dirt = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(band, band + 4f, dist));
                        float rock = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(30f, 42f, steep)) * (1f - dirt);
                        float moss = Mathf.Clamp01((Mathf.PerlinNoise(x * 0.06f, z * 0.06f) - 0.55f) * 4f) * (1f - dirt) * (1f - rock);
                        float grass = Mathf.Max(0f, 1f - dirt - rock - moss);
                        a[z, x, 0] = grass; a[z, x, 1] = dirt; a[z, x, 2] = rock; a[z, x, 3] = moss;
                    }
                data.SetAlphamaps(0, 0, a);
            }

            string path = ArtDir + key + "_Terrain.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(data, path);
            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terrain";
            go.transform.SetParent(parent, false);
            go.transform.position = origin;
            var terrain = go.GetComponent<Terrain>();
            terrain.heightmapPixelError = 4f;
            terrain.basemapDistance = 400f;
            go.isStatic = true;
            return terrain;
        }

        // ------------------------------------------------------------------ scenery

        static void Scenery(Line line, Terrain terrain, Transform parent)
        {
            var root = new GameObject("Scenery").transform;
            root.SetParent(parent, false);
            string p = Alpine + "Prefabs/";
            var pines = Load(p, "SM_Env_Pine_0", 1, 5);
            var rocks = Load(p, "SM_Env_Rock_0", 1, 9);
            var bushes = new List<GameObject>();
            foreach (var b in new[] { "SM_Env_Bush_01", "SM_Env_Bush_02", "SM_Env_Bush_Flower_01" })
            {
                var g = AssetDatabase.LoadAssetAtPath<GameObject>(p + b + ".prefab");
                if (g != null) bushes.Add(g);
            }
            var cliffs = Load(p, "SM_Env_Rock_Cliff_0", 1, 7);
            var mountains = Load(p, "SM_Env_MountainRange_0", 1, 4);

            Scatter(root, "Pines", pines, line, terrain, 900, 9f, 190f, 0.9f, 1.6f, 32f);
            Scatter(root, "Rocks", rocks, line, terrain, 160, 8f, 120f, 0.6f, 1.6f, 60f);
            Scatter(root, "Bushes", bushes, line, terrain, 260, 7.5f, 40f, 0.8f, 1.4f, 35f);

            // Rock walls either side of the cliff face, so the drop reads as a cliff.
            if (cliffs.Count > 0)
            {
                var g = new GameObject("Cliff").transform; g.SetParent(root, false);
                int edge = line.Nearest(new Vector3(232, 0, 0));
                for (int s = -1; s <= 1; s += 2)
                    for (int k = 0; k < 3; k++)
                    {
                        int i = (edge + 2 + k * 3) % line.points.Length;
                        var pos = line.points[i] + line.Right(i) * s * (HalfWidth + 5f + k * 1.5f);
                        pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y - 2f;
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(cliffs[(k + (s > 0 ? 3 : 0)) % cliffs.Count], g);
                        go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-line.Right(i) * s) * Quaternion.Euler(0, Random.Range(-25f, 25f), 0));
                        go.transform.localScale = Vector3.one * Random.Range(1.2f, 1.7f);
                        PushClear(go, line, s);
                        FixBrokenMaterials(go);
                    }
            }
            // A ring of mountains on the horizon.
            if (mountains.Count > 0)
            {
                var g = new GameObject("Mountains").transform; g.SetParent(root, false);
                var c = terrain.transform.position + new Vector3(TerrainSize * 0.5f, 0, TerrainSize * 0.5f);
                for (int i = 0; i < 10; i++)
                {
                    float a = i / 10f * Mathf.PI * 2f;
                    var pos = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 520f;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(mountains[i % mountains.Count], g);
                    go.transform.SetPositionAndRotation(pos + Vector3.down * 10f, Quaternion.LookRotation(c - pos));
                    go.transform.localScale = Vector3.one * Random.Range(2.2f, 3.2f);
                }
            }
        }

        /// <summary>
        /// Some Synty cliffs use a glacier material on the refractive shader graph, which doesn't import here
        /// (renders pink). Swap any material with a broken shader for the pack's plain Alpine rock.
        /// </summary>
        public static void FixBrokenMaterials(GameObject go)
        {
            var rock = AssetDatabase.LoadAssetAtPath<Material>(Alpine + "Materials/PolygonNatureBiomes_Alpine_Mat_01.mat");
            if (rock == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials; bool changed = false;
                for (int k = 0; k < mats.Length; k++)
                    if (mats[k] == null || mats[k].shader == null || !mats[k].shader.isSupported || mats[k].shader.name == "Hidden/InternalErrorShader") { mats[k] = rock; changed = true; }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// <summary>Slide a big prop outwards until no part of it is within the riding width (+ a margin).</summary>
        public static void PushClear(GameObject go, Line line, float side, float clearance = HalfWidth + Shoulder + 1.5f)
        {
            for (int pass = 0; pass < 12; pass++)
            {
                var rends = go.GetComponentsInChildren<Renderer>();
                if (rends.Length == 0) return;
                var b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds);
                int i = line.Nearest(b.center);
                // The inner-most corner of the bounds, sideways from the line.
                float inner = float.MaxValue;
                for (int c = 0; c < 4; c++)
                {
                    var corner = new Vector3((c & 1) == 0 ? b.min.x : b.max.x, b.center.y, (c & 2) == 0 ? b.min.z : b.max.z);
                    inner = Mathf.Min(inner, Vector3.Dot(corner - line.points[i], line.Right(i)) * side);
                }
                if (inner >= clearance) return;
                go.transform.position += line.Right(i) * side * (clearance - inner + 0.2f);
            }
        }

        static List<GameObject> Load(string dir, string prefix, int from, int to)
        {
            var list = new List<GameObject>();
            for (int i = from; i <= to; i++)
            {
                var g = AssetDatabase.LoadAssetAtPath<GameObject>(dir + prefix + i + ".prefab");
                if (g != null) list.Add(g);
            }
            return list;
        }

        static void Scatter(Transform root, string name, List<GameObject> prefabs, Line line, Terrain terrain, int count,
            float minDist, float maxDist, float minScale, float maxScale, float maxSlope)
        {
            if (prefabs.Count == 0) return;
            var g = new GameObject(name).transform; g.SetParent(root, false);
            var data = terrain.terrainData;
            var origin = terrain.transform.position;
            int placed = 0;
            for (int tries = 0; placed < count && tries < count * 8; tries++)
            {
                int i = Random.Range(0, line.points.Length);
                float side = Random.value < 0.5f ? -1f : 1f;
                float off = Mathf.Lerp(minDist, maxDist, Mathf.Pow(Random.value, 1.6f)) + HalfWidth;
                var pos = line.points[i] + line.Right(i) * side * off + line.FlatDir(i) * Random.Range(-Step, Step);
                // Clear of every part of the trail, not just the point it was thrown from.
                int near = line.Nearest(pos);
                if (Vector3.Distance(Flat(pos), Flat(line.points[near])) < HalfWidth + minDist * 0.8f) continue;
                float fx = (pos.x - origin.x) / data.size.x, fz = (pos.z - origin.z) / data.size.z;
                if (fx < 0.02f || fx > 0.98f || fz < 0.02f || fz > 0.98f) continue;
                if (data.GetSteepness(fx, fz) > maxSlope) continue;
                pos.y = terrain.SampleHeight(pos) + origin.y - 0.1f;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[Random.Range(0, prefabs.Count)], g);
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
                go.transform.localScale = Vector3.one * Random.Range(minScale, maxScale);
                go.isStatic = true;
                placed++;
            }
        }

        // ------------------------------------------------------------------ start and race line

        static void StartArch(Line line, Transform parent)
        {
            var g = new GameObject("Start Arch").transform; g.SetParent(parent, false);
            var p = line.points[0]; var r = line.Right(0); var f = line.FlatDir(0);
            var post = SimpleMat("M_ArchPost", new Color(0.12f, 0.1f, 0.16f), Color.black, 0.3f);
            var glow = SimpleMat("M_ArchGlow", new Color(1f, 0.25f, 0.85f), new Color(1f, 0.15f, 0.8f) * 2.4f, 0.5f);
            var white = SimpleMat("M_StartLine", new Color(0.95f, 0.95f, 0.92f), Color.black, 0.1f);
            float span = HalfWidth + Shoulder + 0.8f;
            for (int s = -1; s <= 1; s += 2)
            {
                Cube(g, "Post", p + r * s * span + Vector3.up * 3f, new Vector3(0.5f, 6f, 0.5f), f, post);
                Cube(g, "Post Glow", p + r * s * (span - 0.26f) + Vector3.up * 3f, new Vector3(0.06f, 5.6f, 0.3f), f, glow);
            }
            Cube(g, "Banner", p + Vector3.up * 6.2f, new Vector3(span * 2f + 0.5f, 0.9f, 0.4f), f, post);
            Cube(g, "Banner Glow", p + Vector3.up * 6.2f - f * 0.21f, new Vector3(span * 2f, 0.18f, 0.02f), f, glow);
            var lineStripe = Cube(g, "Start Line", p + Vector3.up * 0.08f, new Vector3(HalfWidth * 2f, 0.02f, 0.6f), f, white);
            Object.DestroyImmediate(lineStripe.GetComponent<BoxCollider>());
        }

        static GameObject Cube(Transform parent, string name, Vector3 pos, Vector3 size, Vector3 fwd, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd, Vector3.up));
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            go.isStatic = true;
            return go;
        }

        static RaceLine MakeRaceLine(Line line, Spec spec, Transform parent)
        {
            var go = new GameObject("Race Line");
            go.transform.SetParent(parent, false);
            var rl = go.AddComponent<RaceLine>();
            rl.laps = spec.laps;
            rl.points = (Vector3[])line.points.Clone();
            rl.distances = (float[])line.distances.Clone();
            rl.length = line.length;
            rl.halfWidth = HalfWidth + Shoulder;
            const int gates = 8;
            rl.gates = new RaceLine.Gate[gates];
            for (int g = 0; g < gates; g++)
            {
                int i = IndexAt(line, line.length * g / gates);
                rl.gates[g] = new RaceLine.Gate { position = line.points[i], forward = line.FlatDir(i), halfWidth = HalfWidth + Shoulder + 4f };
            }
            return rl;
        }

        // ------------------------------------------------------------------ bikes

        /// <summary>The race bike: the pack's MTB with our adapter and reset. Player and rivals both use it.</summary>
        static GameObject MakeBikePrefab()
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(BikeSource);
            if (src == null) { Debug.LogError("[BIKE//RUSH] Simple Bicycle Physics isn't imported: " + BikeSource); return null; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
            if (inst.GetComponent<BikeVehicleAdapter>() == null) inst.AddComponent<BikeVehicleAdapter>();
            if (inst.GetComponent<VehicleReset>() == null) inst.AddComponent<VehicleReset>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, BikePrefab);
            Object.DestroyImmediate(inst);
            return prefab;
        }

        static readonly string[] RivalNames = { "KESTREL", "VANTA", "MOSSBACK", "RIPCORD", "HALCYON" };
        static readonly Color[] RivalColors =
        {
            new Color(1f, 0.55f, 0.1f), new Color(0.2f, 0.85f, 1f), new Color(0.45f, 0.9f, 0.3f),
            new Color(1f, 0.85f, 0.15f), new Color(0.7f, 0.45f, 1f),
        };

        static void MakeRivalField(GameObject bike)
        {
            const string path = Root + "Resources/SR_RivalField.asset";
            var f = AssetDatabase.LoadAssetAtPath<RivalField>(path);
            if (f == null) { f = ScriptableObject.CreateInstance<RivalField>(); AssetDatabase.CreateAsset(f, path); }
            f.carPrefab = bike;
            // The frame's paint ("Blue", the pack's livery texture) and the rider's jersey.
            f.paintToReplace = FindMaterial(bike, "Blue");
            f.paint2ToReplace = FindMaterial(bike, "MTBRider2Jersey");
            f.rivals.Clear();
            for (int i = 0; i < RivalNames.Length; i++)
            {
                // Frame: solid team colour, glossy paint. Jersey: the pack's print, tinted to the team.
                var frame = Livery(f.paintToReplace, "M_Frame_" + RivalNames[i], RivalColors[i], true);
                var jersey = Livery(f.paint2ToReplace, "M_Jersey_" + RivalNames[i], Color.Lerp(Color.white, RivalColors[i], 0.75f), false);
                f.rivals.Add(new RivalField.Rival
                {
                    name = RivalNames[i], livery = frame, livery2 = jersey, color = RivalColors[i],
                    // Cruise = 78% of this: about 30-36 km/h against the player's 32, so boost decides it.
                    grip = 1.1f + i * 0.05f, topSpeedKmh = 38f + i * 2f, shortcutChance = 0.5f,
                });
            }
            f.firstRowBack = 5f; f.rowSpacing = 5f; f.laneSpacing = 2.2f;
            f.catchUp = 0.06f;
            f.rampNitro = new Vector2(0.12f, 0.22f); // × seconds in the air
            f.nitroSpeedGap = 1.5f;
            f.airForBoost = 0.8f;
            EditorUtility.SetDirty(f);
            AssetDatabase.SaveAssets();
        }

        static Material FindMaterial(GameObject root, string name)
        {
            if (root == null) return null;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials) if (m != null && m.name == name) return m;
            return null;
        }

        static Material Livery(Material from, string name, Color color, bool solid)
        {
            if (from == null) return null;
            string path = ArtDir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(from); AssetDatabase.CreateAsset(m, path); }
            else m.CopyPropertiesFromMaterial(from);
            if (solid) { m.SetTexture("_BaseMap", null); m.SetFloat("_Smoothness", 0.6f); m.SetFloat("_Metallic", 0.2f); }
            m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ arrows

        [MenuItem("BIKE RUSH/Courses/Add Trackside to Open Course")]
        public static void AddArrowsToOpenCourse()
        {
            var rl = Object.FindFirstObjectByType<RaceLine>();
            var terrain = Object.FindFirstObjectByType<Terrain>();
            if (rl == null || terrain == null) { Debug.LogWarning("[BIKE//RUSH] Open a course scene first."); return; }
            var line = new Line { points = rl.points, distances = rl.distances, length = rl.length };
            Arrows(line, terrain, rl.transform.parent);
            Markers(line, terrain, rl.transform.parent);
            TrailMaterial();
            EditorSceneManager.MarkSceneDirty(rl.gameObject.scene);
            EditorSceneManager.SaveScene(rl.gameObject.scene);
        }

        /// <summary>
        /// Glowing chevron boards on the outside of every real bend: one early warning, then a run of them round
        /// to the apex, pointing the way. Kept clear of jumps and the start arch.
        /// </summary>
        const float BendDeg = 14f; // heading change over 24 m that counts as a bend worth an arrow
        static void Arrows(Line line, Terrain terrain, Transform parent)
        {
            var old = parent.Find("Arrows");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Arrows").transform; root.SetParent(parent, false);

            var avoid = new List<Vector3> { line.points[0] };
            foreach (var t in parent.GetComponentsInChildren<Transform>())
                if (t.name.StartsWith("Kicker")) avoid.Add(t.position);

            var right = ChevronMaterial(true); var left = ChevronMaterial(false);
            var post = SimpleMat("M_ArchPost", new Color(0.12f, 0.1f, 0.16f), Color.black, 0.3f);

            int n = line.points.Length; const int span = 6; // ±12 m
            var turn = new float[n];
            for (int i = 0; i < n; i++)
                turn[i] = Vector3.SignedAngle(line.FlatDir((i - span + n) % n), line.FlatDir((i + span) % n), Vector3.up);

            var placed = new List<Vector3>();
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                // A bend starts where the turn first gets sharp; follow it to its apex.
                int prev = (i - 1 + n) % n;
                if (Mathf.Abs(turn[i]) < BendDeg || (Mathf.Abs(turn[prev]) >= BendDeg && Mathf.Sign(turn[prev]) == Mathf.Sign(turn[i]))) continue;
                float sign = Mathf.Sign(turn[i]);
                int apex = i, k = i;
                while (Mathf.Abs(turn[k % n]) >= BendDeg && Mathf.Sign(turn[k % n]) == sign && k < i + n) { if (Mathf.Abs(turn[k % n]) > Mathf.Abs(turn[apex % n])) apex = k; k++; }
                if (k - i < 3) continue;
                var spots = new List<int> { i - 10, i - 2 };
                for (int s = i + 4; s <= apex + 2; s += 5) spots.Add(s);
                foreach (int s0 in spots)
                {
                    int s = ((s0 % n) + n) % n;
                    Vector3 c = line.points[s], f = line.FlatDir(s);
                    Vector3 outside = -line.Right(s) * sign; // right-hand bend: boards on the left
                    Vector3 pos = c + outside * (HalfWidth + Shoulder + 1.4f);
                    bool clear = true;
                    foreach (var a in avoid) if (Flat(a - pos).sqrMagnitude < 18f * 18f) clear = false;
                    foreach (var q in placed) if (Flat(q - pos).sqrMagnitude < 7f * 7f) clear = false;
                    if (!clear) continue;
                    placed.Add(pos);
                    pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
                    // Face the riders coming in, turned a little towards the inside of the bend.
                    Vector3 face = Quaternion.AngleAxis(18f * sign, Vector3.up) * f;
                    Board(root, "Arrow " + (++count), pos, face, sign > 0f ? right : left, post);
                }
            }
            Debug.Log("[BIKE//RUSH] " + count + " arrow boards.");
        }

        /// <summary>
        /// Short marker posts with glowing caps both sides of the trail every 10 m: close objects flicking past
        /// are what sells speed. Caps alternate magenta and white. No colliders.
        /// </summary>
        static void Markers(Line line, Terrain terrain, Transform parent)
        {
            var old = parent.Find("Markers");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Markers").transform; root.SetParent(parent, false);
            var post = SimpleMat("M_MarkerPost", new Color(0.1f, 0.09f, 0.13f), Color.black, 0.2f);
            var hot = SimpleMat("M_MarkerHot", new Color(1f, 0.25f, 0.85f), new Color(1f, 0.15f, 0.8f) * 3f, 0.5f);
            var cool = SimpleMat("M_MarkerCool", new Color(0.95f, 0.95f, 1f), new Color(0.9f, 0.95f, 1f) * 1.6f, 0.5f);
            foreach (var m in new[] { post, hot, cool }) { m.enableInstancing = true; EditorUtility.SetDirty(m); }

            var avoid = new List<Vector3> { line.points[0] };
            foreach (var t in parent.GetComponentsInChildren<Transform>())
                if (t.name.StartsWith("Kicker")) avoid.Add(t.position);
            var arrows = parent.Find("Arrows");
            var boards = new List<Vector3>();
            if (arrows != null) foreach (Transform a in arrows) boards.Add(a.position);

            int count = 0, step = 5; // samples are 2 m apart: a post every 10 m
            for (int i = 0; i < line.points.Length; i += step)
            {
                Vector3 c = line.points[i], f = line.FlatDir(i), r = line.Right(i);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 pos = c + r * side * (HalfWidth + Shoulder + 0.7f);
                    bool clear = true;
                    foreach (var a in avoid) if (Flat(a - pos).sqrMagnitude < 12f * 12f) clear = false;
                    foreach (var b in boards) if (Flat(b - pos).sqrMagnitude < 3.5f * 3.5f) clear = false;
                    if (!clear) continue;
                    pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
                    var g = new GameObject("Marker").transform; g.SetParent(root, false); g.position = pos;
                    var a1 = Cube(g, "Post", pos + Vector3.up * 0.5f, new Vector3(0.12f, 1f, 0.12f), f, post);
                    var a2 = Cube(g, "Cap", pos + Vector3.up * 1.08f, new Vector3(0.14f, 0.16f, 0.14f), f, (i / step) % 2 == 0 ? hot : cool);
                    Object.DestroyImmediate(a1.GetComponent<Collider>()); Object.DestroyImmediate(a2.GetComponent<Collider>());
                    count++;
                }
            }
            Debug.Log("[BIKE//RUSH] " + count + " marker posts.");
        }

        static void Board(Transform parent, string name, Vector3 foot, Vector3 face, Material chevrons, Material post)
        {
            var g = new GameObject(name).transform; g.SetParent(parent, false);
            g.SetPositionAndRotation(foot, Quaternion.LookRotation(face, Vector3.up));
            var parts = new List<GameObject>
            {
                Cube(g, "Post", foot + Vector3.up * 1.1f, new Vector3(0.18f, 2.2f, 0.18f), face, post),
                Cube(g, "Back", foot + Vector3.up * 2.1f + face * 0.06f, new Vector3(2.5f, 1.25f, 0.1f), face, post),
            };
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Chevrons"; quad.transform.SetParent(g, false);
            quad.transform.SetPositionAndRotation(foot + Vector3.up * 2.1f - face * 0.005f, Quaternion.LookRotation(face, Vector3.up));
            quad.transform.localScale = new Vector3(2.36f, 1.12f, 1f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = chevrons;
            quad.isStatic = true;
            parts.Add(quad);
            // Scenery, not obstacles: nothing here to crash into.
            foreach (var p in parts) { var col = p.GetComponent<Collider>(); if (col != null) Object.DestroyImmediate(col); }
        }

        static Material ChevronMaterial(bool pointRight)
        {
            string texPath = ArtDir + (pointRight ? "T_ChevronRight.png" : "T_ChevronLeft.png");
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
            {
                const int w = 256, h = 128;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var bg = new Color(0.07f, 0.05f, 0.1f); var hot = new Color(1f, 0.22f, 0.82f);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int xx = pointRight ? x : w - 1 - x;
                        float yc = Mathf.Abs(y - h * 0.5f + 0.5f);
                        bool on = y < 7 || y >= h - 7; // top and bottom rails
                        for (int c = 0; c < 3 && !on; c++)
                        {
                            float v = (xx - 34 - c * 78) + yc * 0.95f;
                            if (yc < 50f && v >= 0f && v < 28f) on = true;
                        }
                        tex.SetPixel(x, y, on ? hot : bg);
                    }
                tex.Apply();
                System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
                var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
                imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = true; imp.SaveAndReimport();
            }
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            var m = SimpleMat(pointRight ? "M_ChevronRight" : "M_ChevronLeft", Color.white, Color.white * 1.8f, 0.4f);
            m.SetTexture("_BaseMap", t);
            m.SetTexture("_EmissionMap", t);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ helpers

        static Material SimpleMat(string name, Color color, Color emission, float smooth)
        {
            string path = ArtDir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smooth);
            if (emission.maxColorComponent > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
            EditorUtility.SetDirty(m);
            return m;
        }

        static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = ArtDir + name + ".asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        static void AddToBuild(string scene)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in list) if (s.path == scene) return;
            list.Add(new EditorBuildSettingsScene(scene, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
