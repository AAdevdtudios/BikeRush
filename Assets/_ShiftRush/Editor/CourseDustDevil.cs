using ShiftRush.AirShow;
using ShiftRush.VFX;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ShiftRush.EditorTools
{
    /// <summary>
    /// DUST DEVIL GULCH: an old frontier mining town at the mouth of a red canyon, late afternoon.
    ///
    /// The lap, in order:
    ///   Main Street   start between adobe houses, wagons and barrels, a straight to wind up to speed
    ///   Town Gate     out between two fort towers under the town's sign
    ///   Mesa Crest    a big kicker off the brow, the canyon opening up below
    ///   Sweeper       a long fast right-hander dropping to the canyon floor, spires on the skyline
    ///   Dry Wash      side kicker over the old riverbed, cattle bones and a wrecked wagon
    ///   Devil's Arch  under a natural rock arch, into the canyon
    ///   Narrows       red walls close in, a kicker in the tightest bit
    ///   Quarry        three switchbacks climbing out past the workings, the head-frame tower above
    ///   Rim Run       flat out along the quarry rim, fence on the drop side, the town below
    ///   Trestle Drop  off the rim beside the mine-cart trestle, land on the straight, sweep back onto Main Street
    ///
    /// BIKE RUSH → Courses → Build Dust Devil Gulch. Synty Arid Desert + Western Frontier (paid, git-ignored).
    /// </summary>
    public static partial class CourseBuilder
    {
        const string Desert = "Assets/Synty/PolygonNatureBiomes/PNB_Arid_Desert/";
        const string West = "Assets/Synty/PolygonWesternFrontier/Prefabs/";

        public static Spec DustDevilGulch()
        {
            var spec = new Spec
            {
                name = "Dust Devil Gulch", scene = Root + "Scenes/Course_DustDevilGulch.unity", laps = 2, seed = 41,
                points = new[]
                {
                    new Vector3(-150, 20, -40),   // 0  start, Main Street heading east
                    new Vector3(-95, 20, -40),
                    new Vector3(-40, 20, -40),    // 2  town gate
                    new Vector3(25, 19.5f, -36),  // 3  mesa crest
                    new Vector3(95, 15, -52),
                    new Vector3(150, 12, -92),    // 5  bottom of the sweeper
                    new Vector3(172, 10, -150),   // 6  dry wash
                    new Vector3(150, 10, -205),   // 7  beyond Devil's Arch
                    new Vector3(85, 11, -228),    // 8  narrows
                    new Vector3(20, 13, -222),
                    new Vector3(-45, 16, -225),   // 10 quarry floor
                    new Vector3(-92, 20, -200),   // 11 switchback
                    new Vector3(-58, 24, -165),   // 12 switchback
                    new Vector3(-95, 28, -132),   // 13 switchback
                    new Vector3(-150, 30.5f, -112), // 14 rim run
                    new Vector3(-196, 30.5f, -106), // 15 trestle edge: the drop points the way the rim runs (west)
                    new Vector3(-214, 22, -103),  // 16 foot of the drop
                    new Vector3(-240, 20.5f, -92), // 17 straight run-out where you land
                    new Vector3(-238, 20.3f, -60), // 18 long sweep round to the north
                    new Vector3(-206, 20, -39),   // 19 and right, into Main Street
                },
                sharp = new[] { 15 },
                jumps = new[]
                {
                    new Jump { near = new Vector3(-8, 0, -38), along = 0f, lateral = 0f, width = 5f, length = 7f, height = 2f },
                    new Jump { near = new Vector3(165, 0, -125), along = 0f, lateral = 2.2f, width = 4.2f, length = 6f, height = 1.8f },
                    new Jump { near = new Vector3(55, 0, -226), along = 0f, lateral = -2f, width = 4.2f, length = 6f, height = 1.8f },
                    new Jump { near = new Vector3(-196, 0, -106), along = -4f, lateral = 0f, width = 8.6f, length = 3.6f, height = 0.7f },
                },
            };

            spec.theme = new Theme
            {
                ground = new LayerDef(Desert + "Terrain/RedSand.terrainlayer", new Color(1.12f, 0.84f, 0.64f), 6f),
                trail = new LayerDef(Desert + "Terrain/Sand_01.terrainlayer", new Color(1.18f, 0.98f, 0.8f), 3f),
                steep = new LayerDef(Desert + "Textures/RockWall_Texture_01.png", new Color(1.05f, 0.66f, 0.46f), 9f),
                patch = new LayerDef(Desert + "Terrain/RedSand.terrainlayer", new Color(0.86f, 0.56f, 0.42f), 9f),
                trailTint = new Color(1f, 0.8f, 0.62f),
                brokenFallback = Desert + "Materials/Rock_Triplanar_Red_01.mat",
                ambient = 1.45f,

                // Late afternoon: low sun from the west behind the riders on Main Street, long shadows, orange haze.
                sunEuler = new Vector3(15f, 82f, 0f),
                sunColor = new Color(1f, 0.74f, 0.5f), sunIntensity = 1.5f,
                skyTint = new Color(0.62f, 0.5f, 0.42f), skyGround = new Color(0.55f, 0.4f, 0.3f),
                atmosphere = 1.7f, exposure = 1.2f, sunSize = 0.055f,
                fogColor = new Color(0.93f, 0.73f, 0.54f), fogStart = 70f, fogEnd = 560f,
                temperature = 16f, saturation = 6f, contrast = 8f, postExposure = 0.05f, vignette = 0.24f,
                filter = new Color(1f, 0.97f, 0.92f),
                air = TrackAmbience.Air.Motes, dust = new Color(0.86f, 0.62f, 0.42f, 0.75f), airDensity = 1.4f,
            };

            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Big Cactus", count = 150, minDist = 7f, maxDist = 120f, minScale = 1.1f, maxScale = 1.9f, maxSlope = 28f,
                prefabs = new[] { West + "Environments/SM_Env_Cactus_Large_01", West + "Environments/SM_Env_Cactus_Large_02", West + "Environments/SM_Env_Cactus_Large_03", West + "Environments/SM_Env_Cactus_Large_04", West + "Environments/SM_Env_Cactus_Large_05", West + "Environments/SM_Env_Cactus_Large_06", West + "Environments/SM_Env_Cactus_Large_07", West + "Environments/SM_Env_Cactus_Large_08" },
                where = a => !spec.Between(a, 19, 2, 10f, 10f), // not on Main Street
            });
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Scrub", count = 420, minDist = 6.5f, maxDist = 60f, minScale = 0.9f, maxScale = 1.5f, maxSlope = 32f, noShadows = true,
                prefabs = new[] { West + "Environments/SM_Env_Cactus_01", West + "Environments/SM_Env_Cactus_02", West + "Environments/SM_Env_Cactus_03", Desert + "Prefabs/SM_Env_Succulent_01", Desert + "Prefabs/SM_Env_Succulent_03", Desert + "Prefabs/SM_Env_Succulent_05", Desert + "Prefabs/SM_Env_Bush_Bramble_01", Desert + "Prefabs/SM_Env_Bush_Bramble_02", West + "Environments/SM_Env_Grass_Desert_01", West + "Environments/SM_Env_Grass_Shrub_01", West + "Environments/SM_Env_Shrub_01", West + "Environments/SM_Env_Shrub_03", Desert + "Prefabs/SM_Env_GroundCover_01", Desert + "Prefabs/SM_Env_GroundCover_02" },
            });
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Rocks", count = 200, minDist = 7f, maxDist = 140f, minScale = 0.8f, maxScale = 2.2f, maxSlope = 45f,
                prefabs = new[] { Desert + "Prefabs/SM_Env_Rock_01", Desert + "Prefabs/SM_Env_Rock_03", Desert + "Prefabs/SM_Env_Rock_05", Desert + "Prefabs/SM_Env_Rock_07", Desert + "Prefabs/SM_Env_Rock_09", Desert + "Prefabs/SM_Env_Rock_11", Desert + "Prefabs/SM_Env_Rock_13", Desert + "Prefabs/SM_Env_Rock_Small_01", Desert + "Prefabs/SM_Env_Rock_Small_03", Desert + "Prefabs/SM_Env_Rock_Small_05", Desert + "Prefabs/SM_Env_Rocks_Spikey_01", Desert + "Prefabs/SM_Env_Rocks_Spikey_03" },
            });
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Dead Trees", count = 40, minDist = 9f, maxDist = 90f, minScale = 0.9f, maxScale = 1.4f, maxSlope = 25f,
                prefabs = new[] { Desert + "Prefabs/SM_Env_Tree_Dead_01", Desert + "Prefabs/SM_Env_Tree_Dead_02", West + "Environments/SM_Env_Tree_Desert_01", West + "Environments/SM_Env_Tree_Desert_02", West + "Environments/SM_Env_Tree_Dead_01" },
                where = a => !spec.Between(a, 19, 2, 10f, 10f),
            });
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Bones and Weeds", count = 60, minDist = 6.5f, maxDist = 30f, minScale = 0.9f, maxScale = 1.2f, noShadows = true,
                prefabs = new[] { Desert + "Prefabs/SM_Prop_Tumbleweed_01", Desert + "Prefabs/Props/SM_Prop_Bones_01", Desert + "Prefabs/Props/SM_Prop_Bones_03", Desert + "Prefabs/Props/SM_Prop_Bones_05", Desert + "Prefabs/SM_Env_Rock_Pebbles_01", Desert + "Prefabs/SM_Env_Rock_Pebbles_03" },
                where = a => !spec.Between(a, 19, 2),
            });

            // The street is wide and flat; the quarry floor has room for the workings.
            spec.wideAt = a => spec.Between(a, 19, 2, 0f, 14f) ? 7f : spec.Between(a, 16, 19) ? 3f : spec.Between(a, 10, 11) ? 4f : 0f;

            // Red-rock country: everything above the trail steps up in ledges (mesas), and the canyon walls stand
            // up close either side between the arch and the quarry, tightest in the narrows.
            spec.shape = (x, z, roadY, dist, along, natural) =>
            {
                float h = natural;
                float canyon = spec.Weight(along, 6, 10, 40f);
                float narrows = spec.Weight(along, 8, 9, 25f);
                if (canyon > 0f)
                {
                    float wall = roadY + 24f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(9f - narrows * 2f, 26f - narrows * 10f, dist));
                    h = Mathf.Max(h, Mathf.Lerp(h, wall, canyon));
                }
                float above = h - roadY;
                if (above > 1.5f)
                {
                    const float step = 7f;
                    float t = above / step, f = Mathf.Floor(t), frac = t - f;
                    float ledge = (f + Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1f, frac))) * step;
                    h = roadY + Mathf.Lerp(above, ledge, Mathf.Clamp01((above - 1.5f) / 4f));
                }
                return h;
            };

            spec.heroAt = 52f;
            spec.dress = d => DressDustDevil(d, spec);
            // The pack's rocks are grey-tan; out here they're red.
            string rock = GulchRock();
            foreach (var m in new[] { "Rock_Triplanar_01", "Rock_Triplanar_02", "Rock_Triplanar_01_White", "Rock_Triplanar_Red_01" })
                spec.theme.recolour.Add(new System.Collections.Generic.KeyValuePair<string, string>(m, rock));
            spec.theme.brokenFallback = rock;
            return spec;
        }

        /// <summary>The pack's triplanar rock, tinted to canyon red (our material; the textures stay the pack's).</summary>
        static string GulchRock()
        {
            string path = ArtDir + "M_GulchRock.mat";
            var src = AssetDatabase.LoadAssetAtPath<Material>(Desert + "Materials/Rock_Triplanar_Red_01.mat");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (src == null) return Desert + "Materials/Rock_Triplanar_Red_01.mat";
            if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, path); }
            else m.CopyPropertiesFromMaterial(src);
            m.name = "M_GulchRock";
            if (m.HasProperty("_Color_Tint")) m.SetColor("_Color_Tint", new Color(1f, 0.6f, 0.44f));
            EditorUtility.SetDirty(m);
            return path;
        }

        [MenuItem("BIKE RUSH/Courses/Build Dust Devil Gulch")]
        public static void BuildDustDevilGulch() { Build(DustDevilGulch()); }

        static void DressDustDevil(Dress d, Spec spec)
        {
            string B = West + "Buildings/", E = West + "Environments/", P = West + "Props/", V = West + "Vehicles/", D = Desert + "Prefabs/";

            // ---- Main Street: adobe houses both sides, the clutter of a working town between them and the street.
            var street = d.Group("Main Street");
            string[] houses = { B + "SM_Bld_Mexican_01", B + "SM_Bld_Mexican_02", B + "SM_Bld_Mexican_04", B + "SM_Bld_Mexican_01", B + "SM_Bld_Cabin_01", B + "SM_Bld_Mexican_02" };
            string[] clutter = { P + "SM_Prop_Barrel_Dirt_01", P + "SM_Prop_Crate_01", P + "SM_Prop_Crate_02", P + "SM_Prop_WaterTrough_01", P + "SM_Prop_Cart_01", P + "SM_Prop_Stack_01", P + "SM_Prop_Stack_03", P + "SM_Prop_LogPile_01", P + "SM_Prop_PlantPot_01", P + "SM_Prop_PlantPot_03", P + "SM_Prop_Alcohol_Barrel_01", P + "SM_Prop_WagonWheel_01" };
            int s0 = d.Cp(19), s1 = d.Cp(2);
            for (int side = -1; side <= 1; side += 2)
            {
                d.Row(houses, street, d.Along(s0, 6f + side * 3f), d.Along(s1, -12f), 13f, side, 18.5f, 4f, 1.15f, 1.3f);
                d.Row(clutter, street, d.Along(s0, 10f), d.Along(s1, -8f), 9f, side, 9.6f, 40f, 1f, 1.15f);
            }
            // Wagons parked along the street, and a barn and the stables at the far end.
            d.Side(V + "SM_Veh_Wagon_Travel_01", street, d.Along(d.Cp(0), 38f), 1f, 10.5f, 90f, 1.1f);
            d.Side(V + "SM_Veh_Wagon_Shop_01", street, d.Along(d.Cp(1), 12f), -1f, 10.5f, -90f, 1.1f);
            d.Side(V + "SM_Veh_Wagon_Jail_01", street, d.Along(d.Cp(0), -30f), -1f, 10.5f, 90f, 1.1f);
            d.Side(B + "SM_Bld_Barn_01", street, d.Along(d.Cp(19), -4f), -1f, 20f, 0f, 1f);
            d.Side(P + "SM_Prop_Fountain_01", street, d.Along(d.Cp(1), -10f), 1f, 10f, 0f, 1.1f);
            // Flags at the start, both sides.
            for (int side = -1; side <= 1; side += 2)
                for (int k = 0; k < 3; k++) d.Side(P + "SM_Prop_FlagPole_01", street, d.Along(d.Cp(0), -14f + k * 9f), side, 8.2f, 90f, 1.2f);

            // ---- Town Gate: two fort towers, walls running away from them, the town's name across the top.
            var gate = d.Group("Town Gate");
            int g = d.Cp(2);
            var gp = d.Point(g); var gr = d.Right(g); var gf = d.Fwd(g);
            float gyaw = Quaternion.LookRotation(gf).eulerAngles.y;
            for (int side = -1; side <= 1; side += 2)
            {
                d.At(B + "SM_Bld_Fort_Tower_01", gate, gp + gr * side * 9.2f, gyaw, 1.25f);
                for (int k = 0; k < 6; k++)
                    d.At(B + "SM_Bld_Fort_Wall_0" + (k % 2 + 1), gate, gp + gr * side * (14f + k * 6.1f) + gf * 0.6f, gyaw, 1.22f);
                d.At(B + "SM_Bld_Fort_TowerCorner_01", gate, gp + gr * side * (14f + 6 * 6.1f + 1.5f), gyaw, 1.25f);
            }
            var wood = d.Mat("M_GulchSignWood", new Color(0.36f, 0.22f, 0.13f), 0.05f);
            float gy = d.Ground(gp);
            var beam = Cube(gate, "Sign Board", gp + Vector3.up * 8.6f + Vector3.up * (gy - gp.y), new Vector3(16.5f, 1.9f, 0.35f), gf, wood);
            Object.DestroyImmediate(beam.GetComponent<Collider>());
            SignText(gate, "DUST DEVIL GULCH", gp + Vector3.up * (gy - gp.y + 8.6f) - gf * 0.2f, gf, new Color(0.98f, 0.86f, 0.62f), 1.25f);
            SignText(gate, "EST. 1871  •  RIDE FAST", gp + Vector3.up * (gy - gp.y + 8.6f) + gf * 0.2f, -gf, new Color(0.98f, 0.86f, 0.62f), 0.9f);

            // ---- Mesa Crest and the Sweeper: spires and buttes on the skyline you aim at all the way down.
            var sky = d.Group("Skyline");
            string[] spires = { D + "SM_Env_Rocks_Spikes_Large_01", D + "SM_Env_Rocks_Spikes_Large_02", D + "SM_Env_Rocks_Spikes_Large_03" };
            var spireSpots = new[] { new Vector3(235, 0, -10), new Vector3(275, 0, -95), new Vector3(250, 0, -205), new Vector3(95, 0, -310), new Vector3(-60, 0, -320), new Vector3(-290, 0, -150), new Vector3(-250, 0, 60), new Vector3(60, 0, 75) };
            for (int k = 0; k < spireSpots.Length; k++)
                d.AtClear(spires[k % spires.Length], sky, spireSpots[k], k * 47f, 3.2f + (k % 3) * 0.7f, 2f);
            for (int k = 0; k < 12; k++)
            {
                float a = k / 12f * Mathf.PI * 2f;
                var c = new Vector3(-10f, 0f, -135f);
                var pos = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 430f;
                d.At(E + "SM_Env_BackgroundCard_0" + (k % 3 + 1), sky, pos, Quaternion.LookRotation(c - pos).eulerAngles.y, 5.5f, false, -6f);
            }

            // ---- Dry Wash: the old riverbed crossing, the wagon that didn't make it.
            var wash = d.Group("Dry Wash");
            int w = d.AtDistance(spec.cpAlong[5] + (spec.cpAlong[6] - spec.cpAlong[5]) * 0.55f);
            d.Side(P + "SM_Prop_Wagon_Destroyed_01", wash, d.Along(w, 8f), -1f, 10f, 35f, 1.1f);
            d.Side(D + "Props/SM_Prop_Bones_07", wash, d.Along(w, -4f), -1f, 8f, 0f, 1.3f);
            d.Side(D + "Props/SM_Prop_Bones_09", wash, d.Along(w, 2f), 1f, 9f, 60f, 1.3f);
            d.Side(D + "SM_Env_Tree_Dead_02", wash, d.Along(w, 16f), 1f, 12f, 0f, 1.4f);
            d.Side(P + "SM_Prop_Barricade_Wood_01", wash, d.Along(w, -14f), 1f, 8.4f, 0f, 1f);
            d.Side(D + "Props/SM_Prop_Sign_01", wash, d.Along(w, -24f), -1f, 8.4f, 20f, 1.1f);

            // ---- Devil's Arch: the natural arch the trail runs under, rock walls either side so it reads as a gap.
            var arch = d.Group("Devils Arch");
            int ai = d.AtDistance(spec.cpAlong[6] + (spec.cpAlong[7] - spec.cpAlong[6]) * 0.45f);
            d.Gateway(D + "SM_Env_Rock_Arch_01", arch, ai, new[] { 2.4f, 2.7f, 3f, 3.3f, 3.6f }, 0f, 1.2f, 6f, HalfWidth + 0.9f);
            string[] cliffs = { D + "SM_Env_Rock_Cliff_02", D + "SM_Env_Rock_Cliff_05", D + "SM_Env_Rock_Cliff_08", D + "SM_Env_Rock_Cliff_11", D + "SM_Env_Rock_Cliff_14" };
            for (int side = -1; side <= 1; side += 2)
                for (int k = 0; k < 3; k++)
                    d.Side(cliffs[(k + (side > 0 ? 2 : 0)) % cliffs.Length], arch, d.Along(ai, -10f + k * 9f), side, 14f + k * 2f, Random.Range(-30f, 30f), 1.6f);

            // ---- The Narrows: rock faces lining the canyon.
            var narrows = d.Group("Narrows");
            d.Row(cliffs, narrows, d.Along(d.Cp(7), 15f), d.Cp(10), 15f, -1f, 12f, 40f, 1.3f, 1.9f);
            d.Row(cliffs, narrows, d.Along(d.Cp(7), 22f), d.Cp(10), 15f, 1f, 12f, 40f, 1.3f, 1.9f);

            // ---- The Quarry: walls round the outside of each hairpin, the workings on the floor, the head-frame tower.
            var quarry = d.Group("Quarry");
            // The head-frame stands south of the floor, seen all the way up the narrows.
            d.Side(B + "SM_Bld_Quarry_Tower_01", quarry, d.Cp(10), -1f, 24f, 30f, 1.2f);
            d.Side(B + "SM_Bld_Quarry_Chimney_01", quarry, d.Along(d.Cp(9), 25f), -1f, 21f, 0f, 1.3f);
            d.Side(B + "SM_Bld_Quarry_01", quarry, d.Along(d.Cp(10), -18f), 1f, 12f, 0f, 1.15f);
            d.Side(B + "SM_Bld_Quarry_03", quarry, d.Along(d.Cp(10), 8f), 1f, 13f, 0f, 1.15f);
            d.Side(B + "SM_Bld_Quarry_05", quarry, d.Along(d.Cp(9), 10f), -1f, 12f, 0f, 1.15f);
            d.Side(B + "SM_Bld_Quarry_Framing_01", quarry, d.Along(d.Cp(10), -30f), -1f, 11f, 0f, 1.2f);
            d.Side(E + "SM_Env_Quarry_Wall_Mine_Entrance_01", quarry, d.Cp(11), -1f, 12f, 0f, 1.3f);
            d.Side(E + "SM_Env_Quarry_Wall_Round_01", quarry, d.Cp(12), 1f, 12f, 0f, 1.3f);
            d.Side(E + "SM_Env_Quarry_Wall_Curved_01", quarry, d.Cp(13), 1f, 12f, 0f, 1.3f);
            string[] works = { P + "SM_Prop_Quarry_Machine_01", P + "SM_Prop_Quarry_Machine_02", P + "SM_Prop_Tnt_Barrel_01", P + "SM_Prop_Tnt_Box", P + "SM_Prop_Quarry_Planks_01", P + "SM_Prop_Quarry_Planks_02", P + "SM_Prop_Quarry_Iron_Panel_01", P + "SM_Prop_Pipes_01", P + "SM_Prop_Cart_02", P + "SM_Prop_WoodPile_01", E + "SM_Env_Quarry_Rocks_01", E + "SM_Env_Quarry_Rocks_02" };
            d.Row(works, quarry, d.Along(d.Cp(9), 18f), d.Cp(13), 11f, 1f, 9f, 50f, 1f, 1.2f);
            d.Row(works, quarry, d.Along(d.Cp(9), 24f), d.Cp(13), 13f, -1f, 9.5f, 50f, 1f, 1.2f);
            // The miners' camp on the floor.
            foreach (var t in new[] { B + "SM_Bld_Tent_01", B + "SM_Bld_Tent_02", B + "SM_Bld_Tent_01" })
                d.Side(t, quarry, d.Along(d.Cp(10), Random.Range(-12f, 12f)), -1f, Random.Range(15f, 22f), Random.Range(-40f, 40f), 1.1f);
            d.Side(P + "SM_Prop_Campfire_01", quarry, d.Cp(10), -1f, 13f, 0f, 1f);

            // ---- Rim Run: a plank fence along the drop side, the town below to the right.
            var rim = d.Group("Rim Run");
            d.Row(new[] { P + "SM_Prop_PlankFence_01", P + "SM_Prop_PlankFence_02", P + "SM_Prop_PlankFence_03" }, rim, d.Along(d.Cp(13), 10f), d.Along(d.Cp(15), -6f), 1.15f, 1f, 8f, 3f, 1f, 1f, false, 0f);
            d.Side(P + "SM_Prop_Tnt_Detonator_01", rim, d.Along(d.Cp(14), -6f), -1f, 8f, 0f, 1.2f);
            d.Side(B + "SM_Bld_Outhouse_01", rim, d.Along(d.Cp(14), 14f), -1f, 10f, 0f, 1.1f);

            // ---- Trestle Drop: a mine-cart trestle stays level while the trail drops away beside it.
            Trestle(d, d.Along(d.Cp(14), 6f), d.Along(d.Cp(16), 8f), -1f, 14f, d.Point(d.Cp(15)).y + 0.4f);
        }

        /// <summary>A mine-cart line on trestle frames at a fixed height, following the trail at an offset.</summary>
        static void Trestle(Dress d, int from, int to, float side, float offset, float railY)
        {
            var root = d.Group("Trestle");
            string frame = West + "Environments/SM_Env_Mine_Track_Frame_01", track = West + "Environments/SM_Env_Mine_Track_Straight_01";
            var pts = new System.Collections.Generic.List<Vector3>();
            float d0 = d.Dist(from), d1 = d.Dist(to);
            if (d1 < d0) d1 += d.line.length;
            for (float a = d0; a <= d1; a += 3f)
            {
                int i = d.AtDistance(a);
                var p = d.Point(i) + d.Right(i) * side * offset; p.y = railY;
                pts.Add(p);
            }
            for (int k = 0; k < pts.Count - 1; k++)
            {
                var a = pts[k]; var b = pts[k + 1];
                var dir = (b - a); dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f) continue;
                float yaw = Quaternion.LookRotation(dir).eulerAngles.y;
                var mid = (a + b) * 0.5f;
                d.At(track, root, mid + Vector3.up * 0.9f, yaw, 1f, true);
                if (k % 2 == 0)
                {
                    // Frames 9.6 m tall hang down from the rail; stack another wherever the ground is further away.
                    float ground = d.Ground(mid), top = railY;
                    for (int n = 0; n < 4; n++)
                    {
                        d.At(frame, root, new Vector3(mid.x, top, mid.z), yaw + 90f, 1f, true);
                        if (top - 8.7f <= ground) break;
                        top -= 9.6f;
                    }
                }
            }
            if (pts.Count > 1)
            {
                var e = pts[pts.Count - 1]; var dir = e - pts[pts.Count - 2]; dir.y = 0f;
                d.At(West + "Environments/SM_Env_Mine_Track_Ending_Block_01", root, e + Vector3.up * 0.9f, Quaternion.LookRotation(dir).eulerAngles.y, 1f, true);
            }
        }

        /// <summary>Painted lettering on a sign, in the stencil face.</summary>
        internal static TextMeshPro SignText(Transform parent, string text, Vector3 pos, Vector3 facing, Color color, float size)
        {
            var go = new GameObject("Sign Text");
            go.transform.SetParent(parent, false);
            // TMP's front is -Z: point that at whoever reads it.
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(facing, Vector3.up));
            var t = go.AddComponent<TextMeshPro>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_ShiftRush/Art/Fonts/Rough/BlackOpsOne-Regular SDF.asset");
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size * 10f;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.rectTransform.sizeDelta = new Vector2(30f, 3f);
            return t;
        }
    }
}
