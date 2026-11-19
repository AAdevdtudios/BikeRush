using System.Collections.Generic;
using ShiftRush.VFX;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShiftRush.EditorTools
{
    /// <summary>
    /// What makes one course look and feel unlike another: its ground, its trail, what grows beside it, its sun,
    /// sky, haze and colour grade, and the air. Plus <see cref="Dress"/>, the toolkit the courses use to place
    /// their set pieces by distance along the lap (buildings facing the trail, landmarks you ride under).
    /// </summary>
    public static partial class CourseBuilder
    {
        /// <summary>A terrain layer from a texture (or an existing layer), tinted and tiled for this course.</summary>
        public class LayerDef
        {
            public string source;   // a .terrainlayer or a texture path
            public Color tint = Color.white;
            public float tile = 3f;
            public LayerDef(string source, Color tint, float tile) { this.source = source; this.tint = tint; this.tile = tile; }
        }

        public class ScatterDef
        {
            public string name;
            public string[] prefabs;
            public int count;
            public float minDist, maxDist, minScale = 0.9f, maxScale = 1.3f, maxSlope = 35f;
            public float minY = float.NegativeInfinity, maxY = float.PositiveInfinity;
            /// <summary>Only along these stretches of the lap (metres), or everywhere when null.</summary>
            public System.Func<float, bool> where;
            public bool noShadows;
        }

        public class Theme
        {
            public LayerDef ground, trail, steep, patch;
            /// <summary>Paint the patch layer below this height (sand by the sea) instead of in noisy blobs.</summary>
            public float patchBelow = float.NaN;
            public Color trailTint = Color.white;
            public float trailTile = 2f;
            public string brokenFallback;          // material for prefabs whose shader didn't import
            /// <summary>Swap materials by name prefix (the pack's grey rock for red rock, say).</summary>
            public List<KeyValuePair<string, string>> recolour = new List<KeyValuePair<string, string>>();
            public float ambient = 1f;
            public List<ScatterDef> scatter = new List<ScatterDef>();

            // Light and sky
            public Vector3 sunEuler = new Vector3(40f, -50f, 0f);
            public Color sunColor = Color.white;
            public float sunIntensity = 1.2f;
            public Color skyTint = new Color(0.5f, 0.5f, 0.5f), skyGround = new Color(0.37f, 0.35f, 0.34f);
            public float atmosphere = 1f, exposure = 1.25f, sunSize = 0.04f;
            public Color fogColor = new Color(0.72f, 0.82f, 0.9f);
            public float fogStart = 120f, fogEnd = 620f;

            // Grade
            public float temperature, tintShift, saturation, contrast, postExposure, vignette = 0.2f;
            public Color filter = Color.white;

            // Air
            public TrackAmbience.Air air = TrackAmbience.Air.Motes;
            public Color dust = new Color(0.86f, 0.8f, 0.7f, 0.7f);
            public float airDensity = 1f;

            internal Material TrailMaterial(string key)
            {
                string path = ArtDir + "M_Trail_" + key + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
                var tex = LayerTexture(trail);
                if (tex != null) m.SetTexture("_BaseMap", tex);
                m.SetColor("_BaseColor", trailTint);
                m.SetFloat("_Smoothness", 0.05f);
                m.SetTextureScale("_BaseMap", new Vector2(trailTile, trailTile));
                EditorUtility.SetDirty(m);
                return m;
            }

            internal TerrainLayer[] TerrainLayers(string key)
            {
                var defs = new[] { ground, trail, steep, patch };
                string[] names = { "Ground", "Trail", "Steep", "Patch" };
                var list = new List<TerrainLayer>();
                for (int i = 0; i < 4; i++)
                {
                    var tex = LayerTexture(defs[i]);
                    if (tex == null) { Debug.LogWarning("[BIKE//RUSH] Missing terrain texture " + defs[i].source); return new TerrainLayer[0]; }
                    string path = ArtDir + key + "_" + names[i] + ".terrainlayer";
                    var l = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                    if (l == null) { l = new TerrainLayer(); AssetDatabase.CreateAsset(l, path); }
                    l.diffuseTexture = tex;
                    var src = AssetDatabase.LoadAssetAtPath<TerrainLayer>(defs[i].source);
                    l.normalMapTexture = src != null ? src.normalMapTexture : null;
                    l.tileSize = new Vector2(defs[i].tile, defs[i].tile);
                    l.diffuseRemapMin = Vector4.zero;
                    l.diffuseRemapMax = new Vector4(defs[i].tint.r, defs[i].tint.g, defs[i].tint.b, 1f);
                    l.smoothness = 0f; l.metallic = 0f;
                    EditorUtility.SetDirty(l);
                    list.Add(l);
                }
                return list.ToArray();
            }

            static Texture2D LayerTexture(LayerDef d)
            {
                if (d == null) return null;
                var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(d.source);
                if (layer != null) return layer.diffuseTexture;
                return AssetDatabase.LoadAssetAtPath<Texture2D>(d.source);
            }

            /// <summary>Sky, sun, haze, colour grade and the drifting air, all saved with the scene.</summary>
            internal void Atmosphere(string key, Transform parent)
            {
                string skyPath = ArtDir + "M_Sky_" + key + ".mat";
                var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
                if (sky == null) { sky = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(sky, skyPath); }
                sky.SetFloat("_SunDisk", 2f);
                sky.SetFloat("_SunSize", sunSize);
                sky.SetFloat("_SunSizeConvergence", 5f);
                sky.SetFloat("_AtmosphereThickness", atmosphere);
                sky.SetColor("_SkyTint", skyTint);
                sky.SetColor("_GroundColor", skyGround);
                sky.SetFloat("_Exposure", exposure);
                EditorUtility.SetDirty(sky);
                RenderSettings.skybox = sky;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientIntensity = ambient;

                var sunGo = GameObject.Find("Directional Light");
                if (sunGo != null)
                {
                    sunGo.transform.rotation = Quaternion.Euler(sunEuler);
                    var l = sunGo.GetComponent<Light>();
                    if (l != null) { l.color = sunColor; l.intensity = sunIntensity; l.shadows = LightShadows.Soft; RenderSettings.sun = l; }
                }
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogStartDistance = fogStart;
                RenderSettings.fogEndDistance = fogEnd;

                // The grade: a saved profile so the scene keeps it.
                string gradePath = ArtDir + key + "_Grade.asset";
                AssetDatabase.DeleteAsset(gradePath);
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, gradePath);
                var ca = profile.Add<ColorAdjustments>(true);
                ca.postExposure.Override(postExposure);
                ca.contrast.Override(contrast);
                ca.saturation.Override(saturation);
                ca.colorFilter.Override(filter);
                var wb = profile.Add<WhiteBalance>(true);
                wb.temperature.Override(temperature);
                wb.tint.Override(tintShift);
                var vg = profile.Add<Vignette>(true);
                vg.intensity.Override(vignette);
                vg.smoothness.Override(0.45f);
                foreach (var c in profile.components) { c.name = c.GetType().Name; AssetDatabase.AddObjectToAsset(c, profile); }
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();

                var volGo = new GameObject("Grade");
                volGo.transform.SetParent(parent, false);
                var vol = volGo.AddComponent<Volume>();
                vol.isGlobal = true;
                vol.priority = 1f;
                vol.sharedProfile = profile;

                var cam = Object.FindFirstObjectByType<Camera>();
                if (cam != null)
                {
                    var data = cam.GetComponent<UniversalAdditionalCameraData>();
                    if (data != null) data.renderPostProcessing = true;
                    cam.farClipPlane = Mathf.Max(cam.farClipPlane, fogEnd + 200f);
                }

                var airGo = new GameObject("Ambience");
                airGo.transform.SetParent(parent, false);
                var amb = airGo.AddComponent<TrackAmbience>();
                amb.air = air; amb.dust = dust; amb.density = airDensity;
            }
        }

        // ------------------------------------------------------------------ themed scatter

        static void ThemeScenery(Theme theme, Dress dress)
        {
            var root = new GameObject("Scenery").transform;
            root.SetParent(dress.root, false);
            foreach (var def in theme.scatter)
            {
                var prefabs = new List<GameObject>();
                foreach (var p in def.prefabs) { var g = Dress.Load(p); if (g != null) prefabs.Add(g); }
                if (prefabs.Count == 0) continue;
                var g2 = new GameObject(def.name).transform; g2.SetParent(root, false);
                var line = dress.line; var terrain = dress.terrain;
                var data = terrain.terrainData; var origin = terrain.transform.position;
                int placed = 0;
                for (int tries = 0; placed < def.count && tries < def.count * 12; tries++)
                {
                    int i = Random.Range(0, line.points.Length);
                    if (def.where != null && !def.where(line.distances[i])) continue;
                    float side = Random.value < 0.5f ? -1f : 1f;
                    float off = Mathf.Lerp(def.minDist, def.maxDist, Mathf.Pow(Random.value, 1.6f)) + HalfWidth;
                    var pos = line.points[i] + line.Right(i) * side * off + line.FlatDir(i) * Random.Range(-Step, Step);
                    int near = line.Nearest(pos);
                    if (Vector3.Distance(Flat(pos), Flat(line.points[near])) < HalfWidth + def.minDist * 0.8f) continue;
                    if (dress.Kept(pos)) continue;
                    float fx = (pos.x - origin.x) / data.size.x, fz = (pos.z - origin.z) / data.size.z;
                    if (fx < 0.02f || fx > 0.98f || fz < 0.02f || fz > 0.98f) continue;
                    if (data.GetSteepness(fx, fz) > def.maxSlope) continue;
                    pos.y = terrain.SampleHeight(pos) + origin.y - 0.1f;
                    if (pos.y < def.minY || pos.y > def.maxY) continue;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[Random.Range(0, prefabs.Count)], g2);
                    go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
                    go.transform.localScale = Vector3.one * Random.Range(def.minScale, def.maxScale);
                    go.isStatic = true;
                    if (def.noShadows) foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
                    FixBrokenMaterials(go, dress.fallback);
                    Recolour(go, theme);
                    placed++;
                }
            }
        }

        // ------------------------------------------------------------------ set-piece toolkit

        public class Dress
        {
            public readonly Spec spec;
            public readonly Line line;
            public readonly Terrain terrain;
            public readonly Transform root;
            public Material fallback;
            readonly List<Vector3> keep = new List<Vector3>(); // (x, radius, z)
            readonly Transform pieces;
            static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

            public Dress(Spec spec, Line line, Terrain terrain, Transform root)
            {
                this.spec = spec; this.line = line; this.terrain = terrain; this.root = root;
                pieces = new GameObject("Set Pieces").transform;
                pieces.SetParent(root, false);
                if (spec.theme != null && !string.IsNullOrEmpty(spec.theme.brokenFallback))
                    fallback = AssetDatabase.LoadAssetAtPath<Material>(spec.theme.brokenFallback);
            }

            public static GameObject Load(string path)
            {
                if (!path.EndsWith(".prefab")) path += ".prefab";
                if (cache.TryGetValue(path, out var g) && g != null) return g;
                g = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (g == null) Debug.LogWarning("[BIKE//RUSH] Missing prefab " + path);
                cache[path] = g;
                return g;
            }

            public Transform Group(string name)
            {
                var t = pieces.Find(name);
                if (t != null) return t;
                t = new GameObject(name).transform; t.SetParent(pieces, false);
                return t;
            }

            /// <summary>The trail sample nearest a control point of the spec.</summary>
            public int Cp(int controlPoint) { return line.Nearest(spec.points[controlPoint]); }
            public int Along(int i, float metres) { return IndexAt(line, line.distances[i] + metres); }
            public int AtDistance(float d) { return IndexAt(line, d); }
            public float Dist(int i) { return line.distances[i]; }
            public Vector3 Point(int i) { return line.points[i]; }
            public Vector3 Right(int i) { return line.Right(i); }
            public Vector3 Fwd(int i) { return line.FlatDir(i); }
            public float Ground(Vector3 p) { return terrain.SampleHeight(p) + terrain.transform.position.y; }

            public void Keep(Vector3 pos, float radius) { keep.Add(new Vector3(pos.x, radius, pos.z)); }
            public bool Kept(Vector3 pos)
            {
                foreach (var k in keep) { float dx = k.x - pos.x, dz = k.z - pos.z; if (dx * dx + dz * dz < k.y * k.y) return true; }
                return false;
            }

            /// <summary>Lowest ground under a footprint, so a building never floats on a slope.</summary>
            public float GroundUnder(Vector3 p, float radius)
            {
                float y = Ground(p);
                for (int k = 0; k < 6; k++)
                {
                    float a = k / 6f * Mathf.PI * 2f;
                    y = Mathf.Min(y, Ground(p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius));
                }
                return y;
            }

            /// <summary>
            /// A prefab beside the trail at sample i: side -1 left / +1 right, offset metres from the centre, turned
            /// to face the trail (plus yaw), pushed out if any part of it would stick into the riding width.
            /// </summary>
            public GameObject Side(string prefab, Transform parent, int i, float side, float offset, float yaw = 0f, float scale = 1f, bool push = true, float sink = 0.15f)
            {
                var src = Load(prefab);
                if (src == null) return null;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
                var r = line.Right(i);
                var pos = line.points[i] + r * side * offset;
                go.transform.localScale = Vector3.one * scale;
                go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-r * side, Vector3.up) * Quaternion.Euler(0f, yaw, 0f));
                if (push) PushClear(go, line, side);
                Settle(go, sink);
                Finish(go);
                return go;
            }

            /// <summary>A prefab at a world spot (y taken from the ground unless keepY).</summary>
            public GameObject At(string prefab, Transform parent, Vector3 pos, float yaw, float scale = 1f, bool keepY = false, float sink = 0.15f)
            {
                var src = Load(prefab);
                if (src == null) return null;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
                go.transform.localScale = Vector3.one * scale;
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
                if (!keepY) Settle(go, sink);
                Finish(go);
                return go;
            }

            /// <summary>A big piece at a world spot, slid away from whichever bit of trail is nearest if it intrudes.</summary>
            public GameObject AtClear(string prefab, Transform parent, Vector3 pos, float yaw, float scale = 1f, float sink = 0.15f)
            {
                var go = At(prefab, parent, pos, yaw, scale, false, sink);
                if (go == null) return null;
                var b = Bounds(go);
                int i = line.Nearest(b.center);
                float side = Mathf.Sign(Vector3.Dot(b.center - line.points[i], line.Right(i)));
                if (side == 0f) side = 1f;
                PushClear(go, line, side);
                Settle(go, sink);
                return go;
            }

            void Settle(GameObject go, float sink)
            {
                var b = Bounds(go);
                float radius = Mathf.Min(Mathf.Max(b.extents.x, b.extents.z), 12f);
                var p = go.transform.position;
                p.y = GroundUnder(new Vector3(b.center.x, 0f, b.center.z), radius * 0.8f) - sink;
                go.transform.position = p;
            }

            void Finish(GameObject go)
            {
                go.isStatic = true;
                foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
                FixBrokenMaterials(go, fallback);
                Recolour(go, spec.theme);
                var b = Bounds(go);
                Keep(b.center, Mathf.Max(b.extents.x, b.extents.z) + 1.5f);
            }

            public static Bounds Bounds(GameObject go)
            {
                var rends = go.GetComponentsInChildren<Renderer>();
                if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.one);
                var b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds);
                return b;
            }

            /// <summary>
            /// Props every few metres down one side of a stretch (sample a to sample b), each facing the trail.
            /// </summary>
            public void Row(string[] prefabs, Transform parent, int a, int b, float spacing, float side, float offset, float yawJitter = 10f, float minScale = 0.95f, float maxScale = 1.1f, bool push = true, float lateralJitter = 0.6f)
            {
                float d0 = line.distances[a], d1 = line.distances[b];
                if (d1 < d0) d1 += line.length;
                for (float d = d0; d <= d1; d += spacing)
                {
                    int i = IndexAt(line, d);
                    Side(prefabs[Random.Range(0, prefabs.Length)], parent, i, side, offset + Random.Range(-lateralJitter, lateralJitter), Random.Range(-yawJitter, yawJitter), Random.Range(minScale, maxScale), push);
                }
            }

            /// <summary>
            /// Something to ride under (a rock arch, a gate): tries each scale, turn and sideways shift until the
            /// riding corridor is clear of it and it really closes over the trail, then keeps the first that fits.
            /// </summary>
            public GameObject Gateway(string prefab, Transform parent, int i, float[] scales, float baseYaw = 0f, float sink = 1.2f, float clearHeight = 6.5f, float clearHalfWidth = HalfWidth + Shoulder + 0.6f, float maxShift = 32f)
            {
                var src = Load(prefab);
                if (src == null) return null;
                var p = line.points[i]; var f = line.Dir(i); var flatF = line.FlatDir(i);
                var corridorRot = Quaternion.LookRotation(f, Vector3.up);
                var half = new Vector3(clearHalfWidth, clearHeight * 0.5f, 4f);
                var centre = p + Vector3.up * (0.4f + half.y);
                foreach (float s in scales)
                    foreach (float yaw in new[] { 0f, 180f })
                        for (float lat = 0f; lat <= maxShift; lat += 1f)
                            foreach (float sgn in lat == 0f ? new[] { 1f } : new[] { -1f, 1f })
                            {
                                var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
                                go.transform.localScale = Vector3.one * s;
                                go.transform.SetPositionAndRotation(p + line.Right(i) * lat * sgn + Vector3.down * sink, Quaternion.LookRotation(flatF) * Quaternion.Euler(0f, baseYaw + yaw, 0f));
                                Physics.SyncTransforms();
                                var mine = new HashSet<Collider>(go.GetComponentsInChildren<Collider>());
                                bool blocked = false;
                                foreach (var c in Physics.OverlapBox(centre, half, corridorRot, ~0, QueryTriggerInteraction.Ignore))
                                    if (mine.Contains(c)) { blocked = true; break; }
                                bool over = false;
                                if (!blocked)
                                    foreach (var h in Physics.RaycastAll(p + Vector3.up * 1f, Vector3.up, 60f, ~0, QueryTriggerInteraction.Ignore))
                                        if (mine.Contains(h.collider)) { over = true; break; }
                                if (!blocked && over)
                                {
                                    Finish(go);
                                    Debug.Log("[BIKE//RUSH] " + src.name + " fits over the trail at scale " + s + ", shift " + lat * sgn + " m.");
                                    return go;
                                }
                                Object.DestroyImmediate(go);
                            }
                Debug.LogWarning("[BIKE//RUSH] Couldn't fit " + src.name + " over the trail.");
                return null;
            }

            /// <summary>A plain tinted material saved with the course art.</summary>
            public Material Mat(string name, Color c, float smooth = 0.1f, Color? emission = null)
            {
                return SimpleMat(name, c, emission ?? Color.black, smooth);
            }
        }

        static void Recolour(GameObject go, Theme theme)
        {
            if (theme == null || theme.recolour.Count == 0) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials; bool changed = false;
                for (int k = 0; k < mats.Length; k++)
                {
                    if (mats[k] == null) continue;
                    foreach (var kv in theme.recolour)
                        if (mats[k].name.StartsWith(kv.Key))
                        {
                            var m = AssetDatabase.LoadAssetAtPath<Material>(kv.Value);
                            if (m != null && m != mats[k]) { mats[k] = m; changed = true; }
                            break;
                        }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// <summary>True when something solid that isn't the ground or the trail is already at this spot.</summary>
        static bool Blocked(Vector3 foot, float radius)
        {
            Physics.SyncTransforms();
            foreach (var c in Physics.OverlapSphere(foot + Vector3.up * 1.6f, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c is TerrainCollider) continue;
                if (c.gameObject.name == "Trail" || c.GetComponentInParent<Race.BoostPad>() != null) continue;
                return true;
            }
            return false;
        }
    }
}
