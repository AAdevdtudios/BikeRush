using System.Collections.Generic;
using ShiftRush.AirShow;
using ShiftRush.VFX;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShiftRush.EditorTools
{
    /// <summary>
    /// More Air Show venues, each its own place and its own run:
    ///   RED ROCK RODEO   a mesa-top run above a red canyon at high noon: frontier fort on the canyon floor, a mine
    ///                    cart rattling across a trestle, vultures circling. Wagon Gap, TNT Alley, Cactus Hop, the
    ///                    Canyon Drop, Devil's Leap.
    ///   CANNONBALL BAY   a clifftop run above a turquoise bay at golden hour: pirate ships sailing past, Skull Rock,
    ///                    gulls, palms down to the beach. Plank Walk, Powder Kegs, Crow's Nest, the Cliff Dive,
    ///                    Broadside.
    /// BIKE RUSH → Courses → Build Red Rock Rodeo / Build Cannonball Bay. Synty packs (paid, git-ignored).
    /// </summary>
    public static partial class AirShowBuilder
    {
        const string DesertP = "Assets/Synty/PolygonNatureBiomes/PNB_Arid_Desert/Prefabs/";
        const string WestP = "Assets/Synty/PolygonWesternFrontier/Prefabs/";
        const string PirP = "Assets/Synty/PolygonPirates/Prefabs/";

        // ------------------------------------------------------------------ RED ROCK RODEO

        public static Venue RedRockRodeo()
        {
            var theme = new CourseBuilder.Theme
            {
                trail = new CourseBuilder.LayerDef("Assets/Synty/PolygonNatureBiomes/PNB_Arid_Desert/Terrain/Sand_01.terrainlayer", Color.white, 3f),
                trailTint = new Color(1f, 0.78f, 0.58f),
                // High noon: hard white light, short shadows, heat haze on the far side of the canyon.
                sunEuler = new Vector3(58f, -24f, 0f), sunColor = new Color(1f, 0.95f, 0.86f), sunIntensity = 1.55f,
                skyTint = new Color(0.45f, 0.55f, 0.7f), skyGround = new Color(0.78f, 0.6f, 0.45f),
                atmosphere = 1.1f, exposure = 1.3f, sunSize = 0.04f,
                fogColor = new Color(0.92f, 0.8f, 0.66f), fogStart = 140f, fogEnd = 1400f,
                temperature = 12f, saturation = 10f, contrast = 12f, postExposure = 0.08f, vignette = 0.2f,
                air = TrackAmbience.Air.Motes, dust = new Color(0.86f, 0.62f, 0.42f, 0.75f), airDensity = 1.3f, ambient = 1.25f,
            };
            return new Venue
            {
                key = "RedRock", title = "RED ROCK RODEO", sub = "AIR SHOW  //  HIGH NOON ON THE MESA", scene = "Show_RedRockRodeo", seed = 73,
                layout = LayoutRedRock,
                backdrop = (root, finishX) => BackdropRedRock(root, finishX),
                life = LifeRedRock,
                sky = () => theme.Atmosphere("RedRock", GameObject.Find("RedRock").transform),
                strata = new[]
                {
                    new Color(0.62f, 0.27f, 0.16f), new Color(0.78f, 0.42f, 0.24f), new Color(0.52f, 0.22f, 0.15f),
                    new Color(0.88f, 0.62f, 0.42f), new Color(0.7f, 0.33f, 0.2f), new Color(0.45f, 0.19f, 0.14f),
                },
                top = theme.TrailMaterial("RedRock"),
                lip = CourseBuilder.SimpleMat("M_RedRockLip", new Color(0.55f, 0.3f, 0.18f), Color.black, 0.05f),
                crates = new[] { WestP + "Props/SM_Prop_Tnt_Box", WestP + "Props/SM_Prop_Crate_01", WestP + "Props/SM_Prop_Barrel_Dirt_01" },
            };
        }

        [MenuItem("BIKE RUSH/Courses/Build Red Rock Rodeo (Air Show)")]
        public static void BuildRedRock() { Build(RedRockRodeo()); }

        static float LayoutRedRock()
        {
            knots.Clear(); boxes.Clear(); checkpoints.Clear(); kickers.Clear();
            // Start on the mesa top, a steep drop-in onto the shelf.
            knots.Add(new Knot(-60f, 16f));
            knots.Add(new Knot(-6f, 16f));
            knots.Add(new Knot(36f, 2f));
            checkpoints.Add(new Vector3(-20f, 16.8f, 0f));
            float x = 58f, y = 2f;
            x = Jump(x, y, 5.5f, 2f, "WAGON GAP", 16f);
            x = Jump(x, y, 5f, 1.5f, "TNT ALLEY", 5f);
            x = Jump(x, y, 5f, 1.5f, "TNT ALLEY", 16f);
            x = Jump(x, y, 6f, 2.6f, "CACTUS HOP", 20f);
            x = Drop(x, ref y, "CANYON DROP");
            x = Jump(x, y, 6.5f, 3f, "RODEO RAMP", 22f);
            x = Jump(x, y, 7f, 3.8f, "DEVIL'S LEAP", 30f);
            knots.Add(new Knot(x + 160f, y));
            return x;
        }

        static void BackdropRedRock(Transform parent, float finishX)
        {
            var group = new GameObject("Backdrop").transform; group.SetParent(parent, false);
            float x0 = knots[0].x - 260f, x1 = knots[knots.Count - 1].x + 320f;
            var ground = CourseBuilder.SimpleMat("M_RedRockValley", new Color(1.05f, 0.7f, 0.5f), Color.black, 0.02f);
            var sand = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Synty/PolygonNatureBiomes/PNB_Arid_Desert/Terrain/RedSand.terrainlayer");
            if (sand != null && sand.diffuseTexture != null) { ground.SetTexture("_BaseMap", sand.diffuseTexture); ground.SetTextureScale("_BaseMap", new Vector2(0.5f, 0.5f)); }
            // The canyon: falls away from the course in red ledges.
            ValleyMesh(group, x0, x1, (x, z) =>
            {
                float y = ValleyY(x, z);
                float near = H(Mathf.Clamp(x, knots[0].x, knots[knots.Count - 1].x));
                float below = near - y;
                if (below > 2f) { float t = below / 8f, f = Mathf.Floor(t); y = near - (f + Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0.45f, t - f))) * 8f; }
                return y;
            }, ground);

            var rock = RedRockMaterial();
            // Spires and buttes standing up out of the canyon.
            var spires = Prefabs(DesertP + "SM_Env_Rocks_Spikes_Large_0", 1, 3);
            var cliffs = Prefabs(DesertP + "SM_Env_Rock_Cliff_0", 1, 9);
            var rocks = new GameObject("Spires").transform; rocks.SetParent(group, false);
            for (int i = 0; i < 16 && spires.Count > 0; i++)
            {
                float x = Random.Range(x0 + 80f, x1 - 80f), z = Random.Range(60f, 300f);
                var s = Place(spires[i % spires.Count], rocks, new Vector3(x, ValleyY(x, Mathf.Min(z, 230f)) - 4f, z), Random.Range(2.5f, 5f));
                s.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                Paint(s, rock);
            }
            for (int i = 0; i < 18 && cliffs.Count > 0; i++)
            {
                float x = Random.Range(x0 + 100f, x1 - 100f), z = Random.Range(25f, 160f);
                var c = Place(cliffs[i % cliffs.Count], rocks, new Vector3(x, ValleyY(x, z) - 3f, z), Random.Range(1.4f, 2.6f));
                c.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                Paint(c, rock);
            }
            // Cactus and scrub on the near slope.
            var scrub = new List<GameObject>();
            scrub.AddRange(Prefabs(WestP + "Environments/SM_Env_Cactus_Large_0", 1, 8));
            scrub.AddRange(Prefabs(DesertP + "SM_Env_Bush_Bramble_0", 1, 2));
            scrub.AddRange(Prefabs(DesertP + "SM_Env_Tree_Dead_0", 1, 2));
            var plants = new GameObject("Cactus").transform; plants.SetParent(group, false);
            for (float x = x0 + 80f; x < x1 - 100f && scrub.Count > 0; x += Random.Range(4f, 11f))
            {
                float z = Mathf.Lerp(6f, 70f, Mathf.Pow(Random.value, 1.8f));
                var p = Place(scrub[Random.Range(0, scrub.Count)], plants, new Vector3(x, ValleyY(x, z) - 0.2f, z), Random.Range(1f, 1.8f));
                p.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            // The frontier town down on the canyon floor: fort walls, adobe houses, a mission.
            var town = new GameObject("Frontier Town").transform; town.SetParent(group, false);
            float tx = (knots[0].x + finishX) * 0.5f;
            string[] houses = { "SM_Bld_Mexican_01", "SM_Bld_Mexican_02", "SM_Bld_Mexican_04", "SM_Bld_Barn_01", "SM_Bld_Cabin_01" };
            for (int i = 0; i < 14; i++)
            {
                float x = tx - 120f + i * 18f + Random.Range(-4f, 4f), z = 150f + Random.Range(-14f, 14f);
                var h = Dress(WestP + "Buildings/" + houses[i % houses.Length], town, new Vector3(x, ValleyY(x, z) - 0.3f, z), Random.Range(-20f, 20f) + 180f, 1.6f);
            }
            for (int i = 0; i < 12; i++)
            {
                float x = tx - 140f + i * 7.3f * 1.6f, z = 128f;
                Dress(WestP + "Buildings/SM_Bld_Fort_Wall_0" + (i % 2 + 1), town, new Vector3(x, ValleyY(x, z) - 0.3f, z), 0f, 1.6f);
            }
            Dress(WestP + "Buildings/SM_Bld_Fort_Tower_01", town, new Vector3(tx - 150f, ValleyY(tx - 150f, 128f) - 0.3f, 128f), 0f, 1.8f);
            Dress(WestP + "Buildings/SM_Bld_Fort_Tower_01", town, new Vector3(tx + 50f, ValleyY(tx + 50f, 128f) - 0.3f, 128f), 0f, 1.8f);

            // Mesas on the horizon.
            var cards = new GameObject("Mesas").transform; cards.SetParent(group, false);
            for (int i = 0; i < 9; i++)
            {
                float x = x0 + 60f + i * (x1 - x0) / 8.5f;
                Dress(WestP + "Environments/SM_Env_BackgroundCard_0" + (i % 3 + 1), cards, new Vector3(x + Random.Range(-40f, 40f), -60f, Random.Range(520f, 680f)), 180f + Random.Range(-10f, 10f), Random.Range(9f, 13f));
            }
        }

        static void LifeRedRock(Transform parent, float finishX)
        {
            var group = new GameObject("Life").transform; group.SetParent(parent, false);
            float x0 = knots[0].x - 300f, x1 = knots[knots.Count - 1].x + 360f;

            // A few high clouds.
            var clouds = new GameObject("Clouds").transform; clouds.SetParent(group, false);
            var cloudPrefabs = Prefabs(WestP + "Generic/SM_Generic_Cloud_0", 1, 3);
            for (int i = 0; i < 18 && cloudPrefabs.Count > 0; i++)
            {
                float x = Random.Range(x0, x1);
                var c = Place(cloudPrefabs[i % cloudPrefabs.Count], clouds, new Vector3(x, Random.Range(70f, 150f), Random.Range(260f, 520f)), Random.Range(5f, 9f));
                c.isStatic = false;
                var m = c.AddComponent<AmbientMotion>();
                m.kind = AmbientMotion.Kind.Drift; m.speed = Random.Range(1f, 3f); m.a = new Vector3(x0, 0, 0); m.b = new Vector3(x1, 0, 0);
            }

            // A mine cart rattling to and fro across a trestle over the canyon.
            float cx = (knots[0].x + finishX) * 0.5f + 40f;
            Trestle(group, new Vector3(cx - 150f, 0f, 80f), new Vector3(cx + 150f, 0f, 80f));

            // Vultures circling, and a couple of balloons out over the far side.
            Birds(group, x0, x1, new Color(0.12f, 0.09f, 0.08f), 5, new Vector2(30f, 70f), new Vector2(30f, 160f), 1.6f);
            var balloons = new GameObject("Balloons").transform; balloons.SetParent(group, false);
            var cols = new[] { (Amber, Magenta), (Teal, Color.white), (Magenta, Amber) };
            for (int i = 0; i < 3; i++)
            {
                float x = Mathf.Lerp(x0 + 300f, x1 - 300f, (i + 0.5f) / 3f);
                var b = Balloon(balloons, new Vector3(x, Random.Range(30f, 60f), Random.Range(180f, 320f)), cols[i].Item1, cols[i].Item2, i + 10);
                var m = b.AddComponent<AmbientMotion>();
                m.kind = AmbientMotion.Kind.Bob; m.speed = Random.Range(0.25f, 0.45f); m.amount = Random.Range(1.2f, 2.5f);
            }
        }

        /// <summary>A trestle of mine-track frames across the canyon at a fixed height, a cart running along it.</summary>
        static void Trestle(Transform parent, Vector3 a, Vector3 b)
        {
            var root = new GameObject("Mine Trestle").transform; root.SetParent(parent, false);
            float railY = Mathf.Max(ValleyY(a.x, a.z), ValleyY(b.x, b.z)) + 6f;
            var frame = AssetDatabase.LoadAssetAtPath<GameObject>(WestP + "Environments/SM_Env_Mine_Track_Frame_01.prefab");
            var track = AssetDatabase.LoadAssetAtPath<GameObject>(WestP + "Environments/SM_Env_Mine_Track_Straight_01.prefab");
            float len = Vector3.Distance(a, b);
            for (float d = 0f; d <= len && track != null; d += 3f)
            {
                var p = Vector3.Lerp(a, b, d / len); p.y = railY + 0.9f;
                var t = Place(track, root, p, 1f); t.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                if (frame != null && Mathf.RoundToInt(d / 3f) % 3 == 0)
                {
                    float ground = ValleyY(p.x, p.z);
                    for (float top = railY; top > ground - 9f; top -= 9.6f)
                    {
                        var f = Place(frame, root, new Vector3(p.x, top, p.z), 1f);
                        f.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    }
                }
            }
            var cartSrc = AssetDatabase.LoadAssetAtPath<GameObject>(WestP + "Props/SM_Prop_Cart_02.prefab");
            if (cartSrc != null)
            {
                var cart = Place(cartSrc, root, new Vector3(a.x, railY + 1.1f, a.z), 1.2f);
                cart.isStatic = false;
                cart.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                var m = cart.AddComponent<AmbientMotion>();
                m.kind = AmbientMotion.Kind.Shuttle; m.a = new Vector3(a.x, railY + 1.1f, a.z); m.b = new Vector3(b.x, railY + 1.1f, b.z); m.amount = 0f; m.speed = 9f;
            }
        }

        static Material RedRockMaterial()
        {
            string path = ArtDir + "M_RedRockShow.mat";
            var src = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonNatureBiomes/PNB_Arid_Desert/Materials/Rock_Triplanar_Red_01.mat");
            if (src == null) return null;
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, path); } else m.CopyPropertiesFromMaterial(src);
            if (m.HasProperty("_Color_Tint")) m.SetColor("_Color_Tint", new Color(1f, 0.6f, 0.44f));
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ CANNONBALL BAY

        const float BaySea = -27f;

        public static Venue CannonballBay()
        {
            var theme = new CourseBuilder.Theme
            {
                trail = new CourseBuilder.LayerDef("Assets/Synty/PolygonNatureBiomes/PNB_Arid_Desert/Terrain/Sand_02.terrainlayer", Color.white, 3f),
                trailTint = new Color(1f, 0.92f, 0.78f),
                // Golden hour behind the camera: the cliff face and the sails glow, the bay goes deep blue.
                sunEuler = new Vector3(16f, 28f, 0f), sunColor = new Color(1f, 0.8f, 0.58f), sunIntensity = 1.45f,
                skyTint = new Color(0.32f, 0.5f, 0.82f), skyGround = new Color(0.55f, 0.72f, 0.82f),
                atmosphere = 0.85f, exposure = 1.15f, sunSize = 0.05f,
                fogColor = new Color(0.86f, 0.8f, 0.72f), fogStart = 180f, fogEnd = 1500f,
                temperature = 8f, saturation = 16f, contrast = 10f, postExposure = 0.05f, vignette = 0.2f,
                air = TrackAmbience.Air.Motes, dust = new Color(0.94f, 0.88f, 0.74f, 0.6f), airDensity = 0.7f, ambient = 1.15f,
            };
            return new Venue
            {
                key = "Cannonball", title = "CANNONBALL BAY", sub = "AIR SHOW  //  GOLDEN HOUR ON THE CLIFFS", scene = "Show_CannonballBay", seed = 91,
                layout = LayoutCannonball,
                backdrop = (root, finishX) => BackdropCannonball(root, finishX),
                life = LifeCannonball,
                sky = () => theme.Atmosphere("Cannonball", GameObject.Find("Cannonball").transform),
                strata = new[]
                {
                    new Color(0.82f, 0.76f, 0.62f), new Color(0.7f, 0.64f, 0.52f), new Color(0.9f, 0.85f, 0.72f),
                    new Color(0.6f, 0.55f, 0.46f), new Color(0.76f, 0.7f, 0.58f), new Color(0.52f, 0.48f, 0.42f),
                },
                top = theme.TrailMaterial("Cannonball"),
                lip = CourseBuilder.SimpleMat("M_CannonballLip", new Color(0.3f, 0.55f, 0.2f), Color.black, 0.05f),
                crates = new[] { PirP + "Props/SM_Prop_Barrel_01", PirP + "Props/SM_Prop_Crate_03", PirP + "Props/SM_Prop_Crate_Cannonballs_01" },
            };
        }

        [MenuItem("BIKE RUSH/Courses/Build Cannonball Bay (Air Show)")]
        public static void BuildCannonball() { Build(CannonballBay()); }

        static float LayoutCannonball()
        {
            knots.Clear(); boxes.Clear(); checkpoints.Clear(); kickers.Clear();
            // Start on the fort's battlement, roll down onto the clifftop.
            knots.Add(new Knot(-60f, 16f));
            knots.Add(new Knot(-6f, 16f));
            knots.Add(new Knot(48f, 0f));
            checkpoints.Add(new Vector3(-20f, 16.8f, 0f));
            float x = 70f, y = 0f;
            x = Jump(x, y, 5f, 1.8f, "PLANK WALK", 18f);
            x = Jump(x, y, 5f, 1.5f, "POWDER KEGS", 6f);
            x = Jump(x, y, 5f, 1.5f, "POWDER KEGS", 18f);
            x = Jump(x, y, 6.5f, 3.2f, "CROW'S NEST", 24f);
            x = Drop(x, ref y, "CLIFF DIVE");
            x = Jump(x, y, 7f, 3.6f, "BROADSIDE", 30f);
            knots.Add(new Knot(x + 160f, y));
            return x;
        }

        static void BackdropCannonball(Transform parent, float finishX)
        {
            var group = new GameObject("Backdrop").transform; group.SetParent(parent, false);
            float x0 = knots[0].x - 260f, x1 = knots[knots.Count - 1].x + 320f;
            var grass = CourseBuilder.SimpleMat("M_CannonballSlope", new Color(0.9f, 1.15f, 0.6f), Color.black, 0.02f);
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Synty/PolygonNatureBiomes/PNB_Alpine_Mountain/Terrain/Grass_01.terrainlayer");
            if (layer != null && layer.diffuseTexture != null) { grass.SetTexture("_BaseMap", layer.diffuseTexture); grass.SetTextureScale("_BaseMap", new Vector2(0.5f, 0.5f)); }
            // The headland falls steeply to a beach and on under the bay.
            ValleyMesh(group, x0, x1, BayY, grass);

            var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sea.name = "Bay";
            sea.transform.SetParent(group, false);
            sea.transform.position = new Vector3((x0 + x1) * 0.5f, BaySea, 600f);
            sea.transform.localScale = new Vector3(400f, 1f, 200f);
            Object.DestroyImmediate(sea.GetComponent<Collider>());
            sea.GetComponent<MeshRenderer>().sharedMaterial = CourseBuilder.SeaMaterial();
            sea.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            // Palms and jungle on the slope, rocks and wreckage on the beach.
            var palms = Prefabs(PirP + "Environments/SM_Env_PalmTree_0", 1, 4);
            palms.AddRange(Prefabs(PirP + "Environments/SM_Env_PalmTree_Tall_0", 1, 2));
            var bushes = Prefabs(PirP + "Environments/SM_Env_Bush_0", 1, 2);
            bushes.AddRange(Prefabs(PirP + "Environments/SM_Env_Plants_0", 1, 3));
            var trees = new GameObject("Palms").transform; trees.SetParent(group, false);
            for (float x = x0 + 70f; x < x1 - 100f && palms.Count > 0; x += Random.Range(3f, 8f))
            {
                float z = Mathf.Lerp(6f, 55f, Mathf.Pow(Random.value, 1.5f));
                float y = BayY(x, z);
                if (y < BaySea + 0.8f) continue;
                var p = Place(palms[Random.Range(0, palms.Count)], trees, new Vector3(x, y - 0.2f, z), Random.Range(0.9f, 1.5f));
                p.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                if (bushes.Count > 0 && Random.value < 0.6f)
                    Place(bushes[Random.Range(0, bushes.Count)], trees, new Vector3(x + Random.Range(-2f, 2f), y - 0.1f, z + Random.Range(-2f, 2f)), Random.Range(1.2f, 2f));
            }
            var rockList = Prefabs(PirP + "Environments/SM_Env_Rock_Large_0", 1, 9);
            var rocks = new GameObject("Sea Rocks").transform; rocks.SetParent(group, false);
            for (int i = 0; i < 26 && rockList.Count > 0; i++)
            {
                float x = Random.Range(x0 + 60f, x1 - 60f), z = Random.Range(40f, 120f);
                var r = Place(rockList[i % rockList.Count], rocks, new Vector3(x, Mathf.Max(BayY(x, z), BaySea - 1.5f) - 0.5f, z), Random.Range(1.5f, 3.5f));
                r.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            // Skull Rock out in the bay, islands on the horizon, the fort on the far headland.
            float mid = (knots[0].x + finishX) * 0.5f;
            Dress(PirP + "Environments/SM_Env_Rock_Skull_01", group, new Vector3(mid + 60f, BaySea - 5f, 230f), 180f, 3.2f);
            for (int i = 0; i < 8; i++)
            {
                float x = x0 + 80f + i * (x1 - x0) / 7.5f;
                Dress(PirP + "Environments/SM_Env_Background_Island_0" + (i % 6 + 1), group, new Vector3(x + Random.Range(-40f, 40f), BaySea - 1.5f, Random.Range(520f, 720f)), Random.Range(0f, 360f), Random.Range(5f, 8f));
            }
            Dress(PirP + "Buildings/SM_Bld_Fort_Tower_01", group, new Vector3(x1 - 120f, BayY(x1 - 120f, 60f), 60f), 200f, 1.4f);
            Dress(PirP + "Buildings/SM_Bld_Fort_Tower_01", group, new Vector3(knots[0].x - 30f, 2f, 28f), 160f, 1.2f);
        }

        /// <summary>The headland: falls away from the course to the beach, then under the bay.</summary>
        static float BayY(float x, float z)
        {
            float y = ValleyY(x, Mathf.Min(z, 30f));
            if (z > 30f) y -= (z - 30f) * 0.9f;
            float beach = BaySea + 1.2f;
            if (y < beach + 6f && y > beach - 2f) y = Mathf.Lerp(y, beach, 0.6f); // a shelf of sand at the waterline
            return Mathf.Max(y, BaySea - 14f);
        }

        static void LifeCannonball(Transform parent, float finishX)
        {
            var group = new GameObject("Life").transform; group.SetParent(parent, false);
            float x0 = knots[0].x - 300f, x1 = knots[knots.Count - 1].x + 360f;

            var clouds = new GameObject("Clouds").transform; clouds.SetParent(group, false);
            var cloudPrefabs = Prefabs(PirP + "Environments/SM_Env_Cloud_0", 1, 3);
            for (int i = 0; i < 22 && cloudPrefabs.Count > 0; i++)
            {
                float x = Random.Range(x0, x1);
                var c = Place(cloudPrefabs[i % cloudPrefabs.Count], clouds, new Vector3(x, Random.Range(60f, 140f), Random.Range(240f, 520f)), Random.Range(1.5f, 2.6f));
                c.isStatic = false;
                var m = c.AddComponent<AmbientMotion>();
                m.kind = AmbientMotion.Kind.Drift; m.speed = Random.Range(1.5f, 3.5f); m.a = new Vector3(x0, 0, 0); m.b = new Vector3(x1, 0, 0);
            }

            // Ships sailing across the bay, bobbing on the swell.
            var ships = new GameObject("Ships").transform; ships.SetParent(group, false);
            string[] hulls = { "SM_Veh_Boat_Warship_01_Hull_Pirate_Attachments", "SM_Veh_Boat_Medium_01_Hull_Pirate_Attachments", "SM_Veh_Boat_Large_01_Hull_Pirate_Attachments", "SM_Veh_Boat_Medium_01_Hull_Attachments", "SM_Veh_Boat_Small_01_Hull_Pirate_Attachments" };
            for (int i = 0; i < 6; i++)
            {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(PirP + "Vehicles/" + hulls[i % hulls.Length] + ".prefab");
                if (src == null) src = AssetDatabase.LoadAssetAtPath<GameObject>(PirP + "Vehicles/SM_Veh_Boat_Medium_01_Hull_Pirate_Attachments.prefab");
                if (src == null) continue;
                float x = Mathf.Lerp(x0, x1, (i + Random.value * 0.5f) / 6f), z = Random.Range(110f, 380f);
                var holder = new GameObject("Ship " + (i + 1)).transform; holder.SetParent(ships, false);
                holder.position = new Vector3(x, BaySea - 2f, z);
                var ship = Place(src, holder, holder.position, z > 250f ? 1.6f : 1.2f);
                ship.isStatic = false;
                ship.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // sailing along the bay
                var bob = ship.AddComponent<AmbientMotion>();
                bob.kind = AmbientMotion.Kind.Bob; bob.axis = Vector3.up; bob.amount = 0.35f; bob.speed = 0.6f;
                var sail = holder.gameObject.AddComponent<AmbientMotion>();
                sail.kind = AmbientMotion.Kind.Drift; sail.speed = Random.Range(2.5f, 5f); sail.a = new Vector3(x0, 0, 0); sail.b = new Vector3(x1, 0, 0);
            }

            // Gulls, and flags fluttering on the fort.
            Birds(group, x0, x1, new Color(0.95f, 0.95f, 0.95f), 6, new Vector2(10f, 40f), new Vector2(20f, 140f), 1f);
        }

        // ------------------------------------------------------------------ shared pieces

        /// <summary>A strip of ground behind the course, rows getting further apart into the distance.</summary>
        static void ValleyMesh(Transform parent, float x0, float x1, System.Func<float, float, float> height, Material m)
        {
            float[] rows = { HalfDepth, 5f, 8f, 13f, 21f, 34f, 52f, 78f, 115f, 165f, 230f };
            int cols = Mathf.CeilToInt((x1 - x0) / 3f) + 1;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < cols; c++)
                {
                    float x = x0 + c * 3f, z = rows[r];
                    v.Add(new Vector3(x, height(x, z), z));
                    uv.Add(new Vector2(x / 12f, z / 12f));
                }
            for (int r = 0; r < rows.Length - 1; r++)
                for (int c = 0; c < cols - 1; c++)
                {
                    int a = r * cols + c, b = a + 1, d = a + cols, e = d + 1;
                    tris.Add(a); tris.Add(d); tris.Add(b); tris.Add(b); tris.Add(d); tris.Add(e);
                }
            var mesh = new Mesh { name = V.key + "_Valley", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            CourseBuilder.SaveMesh(mesh, V.key + "_Valley");
            var go = new GameObject("Valley", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            go.isStatic = true;
        }

        /// <summary>A pack prefab as scenery (no colliders), turned and scaled.</summary>
        static GameObject Dress(string path, Transform parent, Vector3 pos, float yaw, float scale)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(path + ".prefab");
            if (src == null) { Debug.LogWarning("[BIKE//RUSH] Missing prefab " + path); return null; }
            var go = Place(src, parent, pos, scale);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            CourseBuilder.FixBrokenMaterials(go);
            return go;
        }

        static void Paint(GameObject go, Material m)
        {
            if (m == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int k = 0; k < mats.Length; k++) mats[k] = m;
                r.sharedMaterials = mats;
            }
        }

        static void Birds(Transform parent, float x0, float x1, Color colour, int flocks, Vector2 heights, Vector2 depths, float size)
        {
            var mat = CourseBuilder.SimpleMat("M_Bird_" + ColorUtility.ToHtmlStringRGB(colour), colour, Color.black, 0.1f);
            var birds = new GameObject("Birds").transform; birds.SetParent(parent, false);
            for (int f = 0; f < flocks; f++)
            {
                var flock = new GameObject("Flock " + (f + 1)).transform;
                flock.SetParent(birds, false);
                flock.position = new Vector3(Random.Range(x0, x1), Random.Range(heights.x, heights.y), Random.Range(depths.x, depths.y));
                flock.localScale = Vector3.one * size;
                var wings = new List<Transform>();
                int count = Random.Range(3, 7);
                for (int k = 0; k < count; k++)
                {
                    var bird = new GameObject("Bird").transform;
                    bird.SetParent(flock, false);
                    bird.localPosition = new Vector3(-k * 1.6f - Random.value, (k % 2) * 0.8f + Random.value * 0.4f, (k % 2 == 0 ? 1 : -1) * k * 0.9f);
                    Bar(bird, Vector3.zero, new Vector3(0.7f, 0.14f, 0.16f), mat).gameObject.layer = 0;
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var pivot = new GameObject(s < 0 ? "Wing R" : "Wing L").transform;
                        pivot.SetParent(bird, false);
                        Bar(pivot, new Vector3(0f, 0f, s * 0.55f), new Vector3(0.35f, 0.04f, 1.1f), mat).gameObject.layer = 0;
                        wings.Add(pivot);
                    }
                }
                var m = flock.gameObject.AddComponent<AmbientMotion>();
                m.kind = AmbientMotion.Kind.Fly; m.speed = Random.Range(3f, 7f); m.amount = Random.Range(0.8f, 2f);
                m.a = new Vector3(x0, 0, 0); m.b = new Vector3(x1, 0, 0);
                m.wings = wings.ToArray();
            }
        }
    }
}
