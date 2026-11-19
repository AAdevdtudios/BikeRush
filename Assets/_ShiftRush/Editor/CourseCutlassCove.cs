using ShiftRush.AirShow;
using ShiftRush.VFX;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShiftRush.EditorTools
{
    /// <summary>
    /// CUTLASS COVE: a pirate port on a Caribbean island at midday, turquoise water, white sand.
    ///
    /// The lap, in order:
    ///   Harbour Front  start on the quay: colonial houses inland, ships moored alongside, crates and cannons
    ///   Quay Hop       a kicker off the end of the quay, down onto the sand
    ///   Beach Sprint   flat out along the shore past a beached wreck (side kicker beside it)
    ///   Sea Arch       through the rock arch at the end of the beach, Skull Rock out in the bay
    ///   Jungle Climb   up into the trees, the canopy closing over the trail
    ///   Fort Lookout   along the battlements at the top, cannons facing the bay
    ///   Cliff Drop     off the headland in one go
    ///   Shanty Town    down through the stilt houses and washing lines, back to the harbour
    ///
    /// BIKE RUSH → Courses → Build Cutlass Cove. Synty POLYGON Pirates (paid, git-ignored).
    /// </summary>
    public static partial class CourseBuilder
    {
        const string Pir = "Assets/Synty/PolygonPirates/Prefabs/";
        const float SeaY = 6f;

        public static Spec CutlassCove()
        {
            var spec = new Spec
            {
                name = "Cutlass Cove", scene = Root + "Scenes/Course_CutlassCove.unity", laps = 2, seed = 57,
                points = new[]
                {
                    new Vector3(-160, 9, -62),     // 0  start, the quay heading east, sea on the right
                    new Vector3(-100, 9, -63),
                    new Vector3(-45, 9, -60),      // 2  end of the quay
                    new Vector3(10, 7.6f, -76),    // 3  onto the beach
                    new Vector3(80, 7.6f, -88),    // 4  the wreck
                    new Vector3(145, 8, -74),      // 5  sea arch
                    new Vector3(185, 12, -22),     // 6  turn inland
                    new Vector3(182, 18, 38),
                    new Vector3(142, 24, 84),      // 8  under the canopy
                    new Vector3(80, 29, 106),      // 9  fort lookout
                    new Vector3(20, 30, 112),
                    new Vector3(-36, 29.5f, 98),   // 11 cliff edge
                    new Vector3(-44, 21.5f, 86),   // 12 foot of the drop
                    new Vector3(-82, 16, 52),      // 13 shanty town
                    new Vector3(-122, 13, 22),
                    new Vector3(-165, 11, -8),
                    new Vector3(-188, 9.5f, -40),  // 16 round onto the quay
                },
                sharp = new[] { 11 },
                jumps = new[]
                {
                    new Jump { near = new Vector3(-45, 0, -60), along = 14f, lateral = 0f, width = 5f, length = 7f, height = 2.2f },
                    new Jump { near = new Vector3(80, 0, -88), along = -6f, lateral = 2.2f, width = 4.2f, length = 6f, height = 1.8f },
                    new Jump { near = new Vector3(50, 0, 109), along = 0f, lateral = -2f, width = 4.2f, length = 6f, height = 1.8f },
                    new Jump { near = new Vector3(-36, 0, 98), along = -4f, lateral = 0f, width = 8.6f, length = 3.6f, height = 0.7f },
                },
            };

            spec.theme = new Theme
            {
                ground = new LayerDef(Alpine + "Terrain/Grass_01.terrainlayer", new Color(0.95f, 1.3f, 0.62f), 3f),
                trail = new LayerDef(Desert + "Terrain/Sand_01.terrainlayer", new Color(1.25f, 1.12f, 0.95f), 3f),
                steep = new LayerDef(Alpine + "Terrain/RiverRocks_02.terrainlayer", new Color(1.2f, 1.1f, 0.98f), 4f),
                patch = new LayerDef(Desert + "Terrain/Sand_02.terrainlayer", new Color(1f, 0.96f, 0.86f), 6f),
                patchBelow = 8.8f,
                trailTint = new Color(1f, 0.92f, 0.78f),
                ambient = 1.15f,

                // Midday, sun high and behind-left of the riders on the quay: bright, saturated, crisp shadows.
                sunEuler = new Vector3(52f, 140f, 0f),
                sunColor = new Color(1f, 0.96f, 0.88f), sunIntensity = 1.35f,
                skyTint = new Color(0.36f, 0.55f, 0.78f), skyGround = new Color(0.62f, 0.8f, 0.88f),
                atmosphere = 0.8f, exposure = 1.35f, sunSize = 0.035f,
                fogColor = new Color(0.72f, 0.87f, 0.95f), fogStart = 160f, fogEnd = 900f,
                temperature = -4f, saturation = 18f, contrast = 10f, postExposure = 0.1f, vignette = 0.18f,
                filter = new Color(0.98f, 1f, 1f),
                air = TrackAmbience.Air.Motes, dust = new Color(0.94f, 0.88f, 0.74f, 0.6f), airDensity = 0.8f,
            };
            string E = Pir + "Environments/";
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Palms", count = 170, minDist = 6.5f, maxDist = 70f, minScale = 0.9f, maxScale = 1.3f, maxSlope = 30f, minY = SeaY + 0.6f, maxY = 13.5f,
                prefabs = new[] { E + "SM_Env_PalmTree_01", E + "SM_Env_PalmTree_02", E + "SM_Env_PalmTree_03", E + "SM_Env_PalmTree_04", E + "SM_Env_PalmTree_Tall_01", E + "SM_Env_PalmTree_Tall_02" },
                where = a => !spec.Between(a, 16, 2),
            });
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Jungle", count = 170, minDist = 9f, maxDist = 160f, minScale = 0.8f, maxScale = 1.15f, maxSlope = 34f, minY = 11f,
                prefabs = new[] { E + "SM_Env_Tree_Large_01", E + "SM_Env_Tree_Large_02", E + "SM_Env_Mangrove_Tree_01", E + "SM_Env_Mangrove_Tree_02", E + "SM_Env_PalmTree_Tall_01" },
            });
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Undergrowth", count = 460, minDist = 6f, maxDist = 45f, minScale = 1.1f, maxScale = 1.8f, maxSlope = 38f, minY = 8.6f, noShadows = true,
                prefabs = new[] { E + "SM_Env_PalmBush_03", E + "SM_Env_Bush_01", E + "SM_Env_Bush_02", E + "SM_Env_Plants_01", E + "SM_Env_Plants_02", E + "SM_Env_Plants_03", E + "SM_Env_SugarCane_01", E + "SM_Env_SugarCane_02" },
                where = a => !spec.Between(a, 16, 2),
            });
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Flowers and Grass", count = 320, minDist = 6f, maxDist = 30f, minScale = 1.2f, maxScale = 2f, minY = 8.6f, noShadows = true,
                prefabs = new[] { E + "SM_Env_Flowers_01", E + "SM_Env_Flowers_02", E + "SM_Env_Grass_01", E + "SM_Env_Grass_02", E + "SM_Env_Grass_03", E + "SM_Env_GrassPatch_01", E + "SM_Env_GrassPatch_02", E + "SM_Env_Fern_01" },
            });
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Rocks", count = 110, minDist = 7f, maxDist = 120f, minScale = 0.8f, maxScale = 1.8f, maxSlope = 50f, minY = SeaY - 0.5f,
                prefabs = new[] { E + "SM_Env_Rock_01", E + "SM_Env_Rock_02", E + "SM_Env_Rock_03", E + "SM_Env_Rocks_01", E + "SM_Env_Rocks_02", E + "SM_Env_Rocks_03", E + "SM_Env_Rock_Large_01", E + "SM_Env_Rock_Large_03", E + "SM_Env_Rock_Large_05", E + "SM_Env_SmallRock_01", E + "SM_Env_SmallRock_02" },
            });
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Beach", count = 80, minDist = 7f, maxDist = 28f, minScale = 0.9f, maxScale = 1.3f, minY = SeaY + 0.2f, maxY = 8.4f, noShadows = true,
                prefabs = new[] { E + "SM_Env_Beach_Pile_01", E + "SM_Env_Beach_Pile_02", E + "SM_Env_Beach_Pile_03", E + "SM_Env_Seaweed_01", E + "SM_Env_SmallRock_01", E + "SM_Env_SmallRock_02" },
            });

            spec.wideAt = a => spec.Between(a, 16, 2, 0f, 8f) ? 4.5f : spec.Between(a, 12, 15) ? 3f : 0f;
            // The quay ends in a wall straight down into the harbour.
            spec.blendAt = a => spec.Between(a, 16, 2, -14f, 0f) ? 1.5f : 16f;

            // The island: the land slopes to a beach at the shoreline and on under the water. Quay along the
            // south-west, a long beach along the south, sea round the east and west ends too.
            spec.shape = (x, z, roadY, dist, along, natural) =>
            {
                float quay = Mathf.InverseLerp(-20f, -45f, x); // 1 along the harbour
                float shore = Mathf.Lerp(Mathf.Lerp(74f, 102f, Mathf.InverseLerp(-30f, 10f, x)) + (Mathf.PerlinNoise(x * 0.02f, 3.3f) - 0.5f) * 8f, 71f, quay);
                float seaward = Mathf.Max(-z - shore, x - 228f, -x - 262f);
                float beachSlope = SeaY + 0.9f - seaward * 0.3f, harbourFloor = SeaY - 1.2f - seaward * 0.6f;
                float h = Mathf.Min(natural, Mathf.Lerp(beachSlope, harbourFloor, quay * (seaward > -1f ? 1f : 0f)));
                return Mathf.Max(h, 0.6f);
            };

            spec.heroAt = 215f;
            spec.dress = d => DressCove(d, spec);
            return spec;
        }

        [MenuItem("BIKE RUSH/Courses/Build Cutlass Cove")]
        public static void BuildCutlassCove() { Build(CutlassCove()); }

        static void DressCove(Dress d, Spec spec)
        {
            string B = Pir + "Buildings/", E = Pir + "Environments/", P = Pir + "Props/", V = Pir + "Vehicles/";

            // ---- The sea, all the way to the horizon.
            var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sea.name = "Sea";
            sea.transform.SetParent(d.root, false);
            var tc = d.terrain.transform.position + d.terrain.terrainData.size * 0.5f;
            sea.transform.position = new Vector3(tc.x, SeaY, tc.z);
            sea.transform.localScale = new Vector3(400f, 1f, 400f);
            Object.DestroyImmediate(sea.GetComponent<Collider>());
            var sr = sea.GetComponent<MeshRenderer>();
            sr.sharedMaterial = SeaMaterial();
            sr.shadowCastingMode = ShadowCastingMode.Off;

            // ---- Harbour Front: colonial houses inland, the quay's clutter, docks and moored ships on the sea side.
            var harbour = d.Group("Harbour Front");
            int h0 = d.Along(d.Cp(16), 12f), h1 = d.Along(d.Cp(2), -4f);
            int seed = 0;
            float span = Mathf.Repeat(d.Dist(h1) - d.Dist(h0), d.line.length);
            for (float a = 0f; a <= span; a += 6.4f)
            {
                int i = d.AtDistance(d.Dist(h0) + a);
                // Leave a gap for the town hall.
                if (Vector3.Distance(d.Point(i), d.Point(d.Along(d.Cp(1), 8f))) < 9f) continue;
                CubaHouse(d, harbour, i, -1f, 15.5f, seed++);
            }
            d.Side(B + "SM_Bld_Cuba_Hall_Tower_01", harbour, d.Along(d.Cp(1), 8f), -1f, 21f, 0f, 1.15f);
            d.Side(B + "SM_Bld_Cuba_Hall_Front_01", harbour, d.Along(d.Cp(1), 8f), -1f, 17f, 0f, 1.2f);
            string[] quay = { P + "SM_Prop_Barrel_01", P + "SM_Prop_Barrel_03", P + "SM_Prop_Crate_01", P + "SM_Prop_Crate_03", P + "SM_Prop_Crate_05", P + "SM_Prop_Sack_01", P + "SM_Prop_Sack_03", P + "SM_Prop_Stack_01", P + "SM_Prop_Stack_03", P + "SM_Prop_FishRack_01", P + "SM_Prop_Cart_01", P + "SM_Prop_CannonBalls_01", P + "SM_Prop_Crate_Cannonballs_01", P + "SM_Prop_LobsterPot_01" };
            d.Row(quay, harbour, h0, h1, 7f, -1f, 9.6f, 45f, 1f, 1.2f);
            d.Row(quay, harbour, h0, d.Along(d.Cp(2), -10f), 9f, 1f, 8.6f, 45f, 1f, 1.2f);
            // The wharf edge: dock sections along the water, cranes, and cannons facing out to sea.
            QuayWall(d, harbour, h0, d.Along(d.Cp(2), 6f), 1f, 11.6f);
            foreach (int at in new[] { d.Along(d.Cp(0), 32f), d.Along(d.Cp(1), 6f), d.Along(d.Cp(1), 44f) })
                Pier(d, harbour, at, 1f, 12.5f, 5);
            d.Side(P + "SM_Prop_Crane_01", harbour, d.Along(d.Cp(0), 22f), 1f, 10.5f, 180f, 1.2f);
            d.Side(P + "SM_Prop_Crane_02", harbour, d.Along(d.Cp(1), 30f), 1f, 10.5f, 180f, 1.2f);
            for (int k = 0; k < 4; k++) d.Side(P + "SM_Prop_Cannon_0" + (k % 3 + 1), harbour, d.Along(d.Cp(1), -30f + k * 14f), 1f, 9.5f, 180f, 1.2f);
            // Ships alongside, riding the swell.
            Ship(d, harbour, V + "SM_Veh_Boat_Warship_01_Hull_Pirate_Attachments", d.Along(d.Cp(1), -16f), 1f, 40f, SeaY - 2.4f, 1f);
            Ship(d, harbour, V + "SM_Veh_Boat_Medium_01_Hull_Pirate_Attachments", d.Along(d.Cp(0), 4f), 1f, 26f, SeaY - 1.2f, 1f);
            Ship(d, harbour, V + "SM_Veh_Boat_Rowing_01_Hull_Pirate_Attachments", d.Along(d.Cp(2), -14f), 1f, 18f, SeaY - 0.35f, 1f);
            Ship(d, harbour, V + "SM_Veh_Boat_Rowing_01_Hull_Attachments", d.Along(d.Cp(1), 26f), 1f, 18f, SeaY - 0.35f, 1f);
            // Flags at the start, both sides.
            for (int side = -1; side <= 1; side += 2)
                for (int k = 0; k < 2; k++)
                {
                    var pole = d.Side(P + "SM_Prop_Pole_01", harbour, d.Along(d.Cp(0), -10f + k * 12f), side, 8.4f, 0f, 0.75f);
                    if (pole == null) continue;
                    var flag = d.At(V + "SM_Flag_Pirate_0" + (1 + (k * 2 + (side > 0 ? 1 : 0)) % 4), harbour, pole.transform.position + Vector3.up * 7.8f, Quaternion.LookRotation(d.Fwd(d.Cp(0))).eulerAngles.y + 90f, 1.6f, true);
                    Flutter(flag);
                }
            // Torches along the inland side.
            d.Row(new[] { P + "SM_Prop_BottleTorch_01" }, harbour, h0, h1, 15f, -1f, 8.2f, 0f, 1.3f, 1.3f);

            // ---- Beach: the wreck, the sea arch, odds and ends washed up, Skull Rock out in the bay.
            var beach = d.Group("Beach");
            d.Side(P + "SM_Prop_Shipwreck_01", beach, d.Along(d.Cp(4), -2f), 1f, 27f, 35f, 1f, true, 2.5f);
            d.Side(P + "SM_Prop_Shipwreck_02", beach, d.Along(d.Cp(3), -6f), 1f, 22f, -60f, 0.9f, true, 1.5f);
            int archAt = d.AtDistance(spec.cpAlong[5] - 22f);
            d.Gateway(E + "SM_Env_Rock_Arch_01", beach, archAt, new[] { 3.2f, 3.4f, 3.7f, 4f }, 0f, 1.2f, 6f, HalfWidth + 0.9f);
            for (int side = -1; side <= 1; side += 2)
                d.Side(E + "SM_Env_Rock_Huge_0" + (side > 0 ? 1 : 3), beach, d.Along(archAt, 4f), side, 22f, Random.Range(0f, 360f), 0.7f);
            string[] washed = { P + "SM_Prop_Raft_01", P + "SM_Prop_Anchor_01", P + "SM_Prop_Chest_02", P + "SM_Prop_Barrel_Half_01", P + "SM_Prop_Skeleton_Ground_01", P + "SM_Prop_Debris_01", P + "SM_Prop_Debris_02", P + "SM_Prop_Liferaft_Stand_01", V + "SM_Veh_Boat_Rowing_01_Hull" };
            d.Row(washed, beach, d.Along(d.Cp(2), 30f), d.Along(d.Cp(5), -35f), 16f, 1f, 10f, 180f, 1f, 1.2f);
            d.At(E + "SM_Env_Rock_Skull_01", beach, new Vector3(55f, 0f, -178f), 175f, 2.6f, false, 3f);
            for (int k = 0; k < 7; k++)
            {
                var spot = new Vector3(-420f + k * 150f, 0f, -420f - (k % 3) * 70f);
                d.At(E + "SM_Env_Background_Island_0" + (k % 6 + 1), beach, new Vector3(spot.x, SeaY - 1.5f, spot.z), k * 61f, 4f + (k % 3), true);
            }
            d.At(E + "SM_Env_Background_Island_02", beach, new Vector3(520f, SeaY - 1.5f, -60f), 90f, 5f, true);
            d.At(E + "SM_Env_Background_Island_04", beach, new Vector3(-560f, SeaY - 1.5f, 20f), 270f, 5f, true);

            // ---- Jungle Climb: big trees close either side so the canopy meets over the trail.
            var jungle = d.Group("Canopy");
            float j0 = spec.cpAlong[6] - 10f, j1 = spec.cpAlong[8] + 25f;
            int n = 0;
            for (float a = j0; a < j1; a += 19f, n++)
                for (int side = -1; side <= 1; side += 2)
                {
                    int i = d.AtDistance(a + (side > 0 ? 9f : 0f));
                    d.Side(E + "SM_Env_Tree_Large_0" + ((n + side + 2) % 2 + 1), jungle, i, side, 8.8f + Random.Range(0f, 1.5f), Random.Range(0f, 360f), Random.Range(0.85f, 1.05f), false);
                }
            d.Row(new[] { E + "SM_Env_Mangrove_Tree_01", E + "SM_Env_Mangrove_Tree_02", E + "SM_Env_Mangrove_Roots_01" }, jungle, d.Along(d.Cp(5), 18f), d.Cp(6), 9f, -1f, 9f, 180f, 1f, 1.4f);

            // ---- Fort Lookout: the fort on the bay side, cannons along the battlement edge, flags.
            var fort = d.Group("Fort");
            int f0 = d.Along(d.Cp(9), 12f);
            var tower = d.Side(B + "SM_Bld_Fort_Tower_01", fort, f0, -1f, 26f, 0f, 1f);
            d.Side(B + "SM_Bld_Fort_Wall_01", fort, d.Along(f0, -16f), -1f, 21f, 0f, 1f);
            d.Side(B + "SM_Bld_Fort_Wall_02", fort, d.Along(f0, 16f), -1f, 21f, 0f, 1f);
            for (int k = 0; k < 6; k++) d.Side(P + "SM_Prop_Cannon_0" + (k % 3 + 1), fort, d.Along(d.Cp(9), -14f + k * 11f), -1f, 9f, 180f, 1.3f);
            d.Row(new[] { P + "SM_Prop_CannonBalls_01", P + "SM_Prop_CannonBalls_02", P + "SM_Prop_Crate_Cannonballs_01", P + "SM_Prop_Barrel_02" }, fort, d.Along(d.Cp(9), -8f), d.Cp(11), 11f, 1f, 8.6f, 60f, 1f, 1.2f);
            if (tower != null)
            {
                var top = Dress.Bounds(tower);
                var flag = d.At(V + "SM_Flag_Pirate_01", fort, new Vector3(top.center.x, top.max.y + 1.5f, top.center.z), 0f, 3f, true);
                Flutter(flag);
            }

            // ---- Shanty Town: stilt houses both sides, the rickety tower, torches, washing, the graveyard.
            var shanty = d.Group("Shanty Town");
            string[] huts = { B + "SM_Bld_Shanty_Preset_01", B + "SM_Bld_Shanty_Preset_02", B + "SM_Bld_Shanty_Preset_03", B + "SM_Bld_Shanty_Preset_04", B + "SM_Bld_Shanty_Preset_06", B + "SM_Bld_Shanty_Preset_07", B + "SM_Bld_Shanty_Preset_Bar_01", B + "SM_Bld_Rickety_House_01", B + "SM_Bld_Rickety_House_02" };
            d.Row(huts, shanty, d.Along(d.Cp(12), 18f), d.Along(d.Cp(15), 10f), 12f, -1f, 15f, 12f, 1f, 1.15f);
            d.Row(huts, shanty, d.Along(d.Cp(12), 24f), d.Along(d.Cp(15), 10f), 13f, 1f, 15f, 12f, 1f, 1.15f);
            string[] yard = { P + "SM_Prop_ClothesLine_01", P + "SM_Prop_FishRack_01", P + "SM_Prop_Barrel_04", P + "SM_Prop_Crate_02", P + "SM_Prop_BottleTorch_01", P + "SM_Prop_Sack_02", P + "SM_Prop_Cage_01", P + "SM_Prop_Campfire_Pot_01" };
            d.Row(yard, shanty, d.Along(d.Cp(12), 14f), d.Along(d.Cp(15), 6f), 7f, -1f, 8.8f, 60f, 1f, 1.2f);
            d.Row(yard, shanty, d.Along(d.Cp(12), 18f), d.Along(d.Cp(15), 6f), 8f, 1f, 8.8f, 60f, 1f, 1.2f);
            d.Side(B + "SM_Bld_Rickety_Tower_03", shanty, d.Cp(14), 1f, 19f, 0f, 1.1f);
            d.Side(P + "SM_Prop_Gallows_01", shanty, d.Along(d.Cp(13), -8f), 1f, 12f, 0f, 1.1f);
            for (int k = 0; k < 7; k++)
                d.Side(P + "SM_Prop_Grave_0" + (k % 5 + 1), shanty, d.Along(d.Cp(13), 14f + k * 2.6f), -1f, 22f + (k % 2) * 2.4f, Random.Range(-8f, 8f), 1.1f);

            // ---- Sky: a few big clouds drifting over.
            var clouds = d.Group("Clouds");
            for (int k = 0; k < 9; k++)
            {
                var c = d.At(E + "SM_Env_Cloud_0" + (k % 3 + 1), clouds, new Vector3(-500f + k * 125f, 130f + (k % 4) * 18f, -200f + (k * 97 % 420)), k * 40f, 1.4f + (k % 3) * 0.4f, true);
                if (c == null) continue;
                foreach (var t in c.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
                var m = c.AddComponent<AmbientMotion>();
                m.kind = AmbientMotion.Kind.Drift; m.speed = 2.2f; m.a = new Vector3(-650f, 0f, 0f); m.b = new Vector3(650f, 0f, 0f);
            }
        }

        /// <summary>A dressed-stone quay edge from the trail's level down into the water.</summary>
        static void QuayWall(Dress d, Transform parent, int from, int to, float side, float offset)
        {
            var root = new GameObject("Quay Wall").transform; root.SetParent(parent, false);
            var stone = d.Mat("M_QuayStone", new Color(0.72f, 0.66f, 0.56f), 0.08f);
            var cap = d.Mat("M_QuayCap", new Color(0.84f, 0.8f, 0.72f), 0.12f);
            float d0 = d.Dist(from), span = Mathf.Repeat(d.Dist(to) - d0, d.line.length);
            for (float a = 0f; a < span; a += 3f)
            {
                int i = d.AtDistance(d0 + a), j = d.AtDistance(d0 + a + 3f);
                Vector3 p = d.Point(i) + d.Right(i) * side * offset, q = d.Point(j) + d.Right(j) * side * offset;
                float top = Mathf.Max(d.Point(i).y, d.Point(j).y) + 0.1f, bottom = SeaY - 2.5f;
                var fwd = q - p; fwd.y = 0f;
                var mid = (p + q) * 0.5f;
                var block = Cube(root, "Wall", new Vector3(mid.x, (top + bottom) * 0.5f, mid.z), new Vector3(1.4f, top - bottom, fwd.magnitude + 0.08f), fwd, stone);
                block.isStatic = true;
                var c = Cube(root, "Cap", new Vector3(mid.x, top + 0.12f, mid.z), new Vector3(1.6f, 0.24f, fwd.magnitude + 0.08f), fwd, cap);
                Object.DestroyImmediate(c.GetComponent<Collider>());
            }
        }

        /// <summary>A plank pier straight out from the quay over the water, on posts, a boat tied at the end.</summary>
        static void Pier(Dress d, Transform parent, int i, float side, float start, int sections)
        {
            var root = new GameObject("Pier").transform; root.SetParent(parent, false);
            var plank = d.Mat("M_PierPlank", new Color(0.55f, 0.4f, 0.26f), 0.06f);
            var post = d.Mat("M_PierPost", new Color(0.3f, 0.21f, 0.14f), 0.05f);
            var r = d.Right(i) * side; var f = d.Fwd(i);
            float deckY = d.Point(i).y - 0.15f, length = sections * 3.8f;
            var c = d.Point(i) + r * (start + length * 0.5f); c.y = deckY;
            var deck = Cube(root, "Deck", c, new Vector3(3.4f, 0.22f, length), r, plank);
            for (int k = 0; k <= sections; k++)
                for (int s2 = -1; s2 <= 1; s2 += 2)
                {
                    var p = d.Point(i) + r * (start + k * 3.8f) + f * s2 * 1.5f;
                    var col = Cube(root, "Post", new Vector3(p.x, (deckY + 0.5f + SeaY - 3f) * 0.5f, p.z), new Vector3(0.28f, deckY + 0.5f - (SeaY - 3f), 0.28f), r, post);
                    Object.DestroyImmediate(col.GetComponent<Collider>());
                }
            var end = d.Point(i) + r * (start + length - 1.2f); end.y = deckY + 0.11f;
            d.At(Pir + "Props/SM_Prop_Barrel_02", root, end + f * 1f, 0f, 1f, true);
            d.At(Pir + "Props/SM_Prop_Crate_04", root, end - f * 0.9f - r * 2f, 20f, 1f, true);
            var boat = d.Point(i) + r * (start + length - 4f) + f * 3.6f; boat.y = SeaY - 0.35f;
            Ship(d, root, Pir + "Vehicles/SM_Veh_Boat_Rowing_01_Hull", d.AtDistance(d.Dist(i)), 0f, 0f, SeaY - 0.35f, 1f);
            var last = root.GetChild(root.childCount - 1);
            last.SetPositionAndRotation(boat, Quaternion.LookRotation(r));
        }

        /// <summary>A little colonial house: a room, often a second storey, a tiled roof.</summary>
        static void CubaHouse(Dress d, Transform parent, int i, float side, float offset, int seed)
        {
            const string B = Pir + "Buildings/";
            const float s = 1.25f;
            var r = d.Right(i);
            var p = d.Point(i) + r * side * offset;
            float yaw = Quaternion.LookRotation(-r * side).eulerAngles.y;
            var room = d.At(B + "SM_Bld_Cuba_Room_0" + (1 + seed % 7), parent, p, yaw, s);
            if (room == null) return;
            float y = room.transform.position.y;
            bool two = seed % 3 != 1;
            if (two) d.At(B + "SM_Bld_Cuba_Room_0" + (1 + (seed * 3 + 2) % 7), parent, new Vector3(p.x, y + 3.3f * s, p.z), yaw, s, true);
            d.At(B + "SM_Bld_Cuba_Roof_0" + (1 + seed % 4), parent, new Vector3(p.x, y + (two ? 6.6f : 3.3f) * s, p.z), yaw, s, true);
        }

        /// <summary>A boat on the water beside the trail, lying along it, bobbing.</summary>
        static void Ship(Dress d, Transform parent, string prefab, int i, float side, float offset, float y, float scale)
        {
            var r = d.Right(i);
            var p = d.Point(i) + r * side * offset; p.y = y;
            var go = d.At(prefab, parent, p, Quaternion.LookRotation(d.Fwd(i)).eulerAngles.y, scale, true);
            if (go == null) return;
            foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
            var m = go.AddComponent<AmbientMotion>();
            m.kind = AmbientMotion.Kind.Bob; m.axis = Vector3.up; m.amount = 0.18f; m.speed = 0.7f;
        }

        static void Flutter(GameObject flag)
        {
            if (flag == null) return;
            foreach (var t in flag.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
            var m = flag.AddComponent<AmbientMotion>();
            m.kind = AmbientMotion.Kind.Flutter; m.axis = Vector3.up; m.amount = 9f; m.speed = 0.6f;
        }

        internal static Material SeaMaterial()
        {
            string path = ArtDir + "M_Sea.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor("_BaseColor", new Color(0.04f, 0.55f, 0.62f, 0.74f));
            m.SetFloat("_Smoothness", 0.93f);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
