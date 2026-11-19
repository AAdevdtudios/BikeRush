using ShiftRush.AirShow;
using ShiftRush.VFX;
using UnityEditor;
using UnityEngine;

namespace ShiftRush.EditorTools
{
    /// <summary>
    /// FROSTLIGHT: the Christmas trail. A snowed-in mountain village at blue hour, warm lights against cold snow.
    ///
    /// The lap, in order:
    ///   Village Square   start beside the giant lit tree, string lights across the street, MERRY CHRISTMAS overhead
    ///   Candy Cane Gate  out of the village under two giant candy canes and the trail's name
    ///   Frozen Lake      a flat ice road across the lake, fishing huts and neon snowflakes, off the jetty kicker
    ///   Lantern Forest   climbing through the pines between lantern posts, a side kicker on the way up
    ///   Sleigh Jump      the big air of the lap, the reindeer team parked by the landing, a neon sleigh overhead
    ///   Toy Workshop     straight through the workshop, presents stacked to the rafters
    ///   Ridge Run        flat out along the ridge under the northern lights
    ///   Switchbacks      three hairpins down, candy canes on the apexes, a sweep back into the square
    ///
    /// BIKE RUSH → Courses → Build Frostlight. Synty Holidays 2025 + Alpine + Western Frontier (paid, git-ignored).
    /// </summary>
    public static partial class CourseBuilder
    {
        const string Holiday = "Assets/Synty/PolygonHolidays2025/Prefabs/";
        const string AlpineP = "Assets/Synty/PolygonNatureBiomes/PNB_Alpine_Mountain/Prefabs/";
        const string AlpineFx = "Assets/Synty/PolygonNatureBiomes/PNB_Alpine_Mountain/FX/FX_Prefabs/";
        const float LakeY = 18f;
        const string SnowLayer = "layer_snow_albedosnow_normalb05780438ad4031";

        public static Spec Frostlight()
        {
            var spec = new Spec
            {
                name = "Frostlight", scene = Root + "Scenes/Course_Frostlight.unity", laps = 2, seed = 1225,
                points = new[]
                {
                    new Vector3(-140, 22, 0),     // 0  start, the village street heading east
                    new Vector3(-90, 22, 0),      // 1  MERRY CHRISTMAS banner
                    new Vector3(-40, 22, 2),      // 2  candy cane gate
                    new Vector3(20, 21, -10),     // 3  down to the shore
                    new Vector3(80, LakeY, -40),  // 4  onto the ice
                    new Vector3(140, LakeY, -62),
                    new Vector3(195, LakeY, -42), // 6  off the jetty kicker
                    new Vector3(232, 21, 10),     // 7  into the forest
                    new Vector3(238, 26, 70),
                    new Vector3(208, 30, 120),    // 9  top of the forest
                    new Vector3(150, 32, 140),    // 10 sleigh jump lip
                    new Vector3(95, 26, 150),     // 11 landing
                    new Vector3(40, 27, 146),     // 12 toy workshop
                    new Vector3(-20, 31, 140),    // 13 ridge run
                    new Vector3(-80, 32, 126),
                    new Vector3(-125, 30, 95),    // 15 switchback
                    new Vector3(-85, 27, 72),     // 16 switchback
                    new Vector3(-132, 24, 48),    // 17 switchback
                    new Vector3(-172, 22.5f, 30), // 18 sweep
                    new Vector3(-184, 22, 6),     // 19 into the square
                },
                sharp = new int[0],
                jumps = new[]
                {
                    new Jump { near = new Vector3(178, 0, -54), along = 0f, lateral = 0f, width = 5f, length = 6.5f, height = 1.8f },    // jetty
                    new Jump { near = new Vector3(236, 0, 40), along = 0f, lateral = 2.2f, width = 4.2f, length = 6f, height = 1.6f },  // forest side kicker
                    new Jump { near = new Vector3(160, 0, 137), along = 0f, lateral = 0f, width = 5.4f, length = 7.5f, height = 2.4f }, // sleigh jump
                },
            };

            string snow = FrostSnowMat(), ice = FrostIceMat();
            spec.theme = new Theme
            {
                // All snow: packed and grey-blue on the trail, shaded on the steep, a cold blue sheen on the lake.
                ground = new LayerDef(AlpineT(SnowLayer), new Color(1.02f, 1.04f, 1.1f), 7f),
                trail = new LayerDef(AlpineT(SnowLayer), new Color(0.74f, 0.76f, 0.84f), 3f),
                steep = new LayerDef(AlpineT(SnowLayer), new Color(0.84f, 0.88f, 0.98f), 14f),
                patch = new LayerDef(AlpineT(SnowLayer), new Color(0.62f, 0.8f, 1.05f), 16f),
                patchBelow = LakeY + 0.35f,
                trailTint = new Color(0.78f, 0.8f, 0.88f),
                brokenFallback = snow,
                ambient = 1.0f,

                // Blue hour: the sun just under the hills behind the start, a deep blue sky, cold fog, and every
                // warm light in the village standing out against it.
                sunEuler = new Vector3(4f, 255f, 0f),
                sunColor = new Color(0.95f, 0.55f, 0.7f), sunIntensity = 0.45f,
                skyTint = new Color(0.17f, 0.22f, 0.48f), skyGround = new Color(0.16f, 0.18f, 0.28f),
                atmosphere = 1.1f, exposure = 0.72f, sunSize = 0.02f,
                fogColor = new Color(0.33f, 0.39f, 0.6f), fogStart = 55f, fogEnd = 400f,
                temperature = -12f, saturation = 12f, contrast = 12f, postExposure = 0f, vignette = 0.3f,
                filter = new Color(0.96f, 0.98f, 1.04f),
                air = TrackAmbience.Air.Snow, dust = new Color(0.95f, 0.97f, 1f, 0.8f), airDensity = 1.6f,
            };
            foreach (var m in new[] { "Snow_Rock_Tri", "Snow_Rock_Tri_Background", "Snow_Rock_Tri_Background_02", "Triplanar_Snow_01", "Triplanar_Snow_02", "Snow" })
                spec.theme.recolour.Add(new System.Collections.Generic.KeyValuePair<string, string>(m, snow));
            spec.theme.recolour.Add(new System.Collections.Generic.KeyValuePair<string, string>("Triplanar_Ice_01", ice));

            System.Func<float, bool> wild = a => !spec.Between(a, 19, 2, 12f, 12f) && !spec.Between(a, 3, 7, 0f, 0f) && !spec.Between(a, 11, 12, -10f, 20f);
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Pines", count = 320, minDist = 7.5f, maxDist = 140f, minScale = 0.9f, maxScale = 1.6f, maxSlope = 32f, minY = LakeY + 0.8f,
                prefabs = Paths(AlpineP, "SM_Env_Pine_01", "SM_Env_Pine_02", "SM_Env_Pine_03", "SM_Env_Pine_04", "SM_Env_Pine_05"),
                where = wild,
            });
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Forest Pines", count = 160, minDist = 6.5f, maxDist = 40f, minScale = 0.9f, maxScale = 1.3f, maxSlope = 35f, minY = LakeY + 0.8f,
                prefabs = Paths(AlpineP, "SM_Env_Pine_01", "SM_Env_Pine_02", "SM_Env_Pine_03", "SM_Env_Pine_04"),
                where = a => spec.Between(a, 7, 10, 0f, 0f),
            });
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Rocks", count = 140, minDist = 7f, maxDist = 120f, minScale = 0.8f, maxScale = 2f, maxSlope = 45f, minY = LakeY + 0.6f,
                prefabs = Paths(AlpineP, "SM_Env_Rock_01", "SM_Env_Rock_03", "SM_Env_Rock_05", "SM_Env_Rock_07", "SM_Env_Rock_09"),
                where = wild,
            });
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Snow Mounds", count = 160, minDist = 6.5f, maxDist = 50f, minScale = 1.2f, maxScale = 2.6f, maxSlope = 30f, noShadows = true,
                prefabs = Paths(AlpineP, "SM_Env_Snow_Mound_01", "SM_Env_Snow_Mound_02", "SM_Env_Snow_Mound_03", "SM_Env_Snow_Mound_04"),
                where = a => !spec.Between(a, 19, 2, 6f, 6f),
            });
            spec.theme.scatter.Add(new ScatterDef
            {
                name = "Dead Pines and Stumps", count = 40, minDist = 8f, maxDist = 70f, minScale = 0.9f, maxScale = 1.3f, maxSlope = 30f, minY = LakeY + 0.8f,
                prefabs = Paths(AlpineP, "SM_Env_Pine_NoLeaves_01", "SM_Env_Pine_Stump_01", "SM_Env_Branch_01", "SM_Env_Branch_02"),
                where = wild,
            });

            // The street and the plaza round the tree are wide; the ice road has room either side.
            spec.wideAt = a => spec.Between(a, 19, 2, 0f, 12f) ? 7f : spec.Between(a, 4, 6) ? 4f : 0f;

            // The lake: everything within reach of the ice road sits just under the trail, flat as a rink, and rises
            // back to the hills beyond. The ridge stands proud: the land drops away either side of it.
            spec.shape = (x, z, roadY, dist, along, natural) =>
            {
                float h = natural;
                float lake = spec.Weight(along, 4, 6, 40f);
                if (lake > 0f)
                {
                    float flat = Mathf.Lerp(LakeY - 0.2f, natural, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(55f, 95f, dist)));
                    h = Mathf.Lerp(h, Mathf.Min(h, flat), lake);
                }
                float ridge = spec.Weight(along, 13, 15, 25f);
                if (ridge > 0f && dist > 14f)
                    h = Mathf.Lerp(h, Mathf.Min(h, roadY - (dist - 14f) * 0.35f), ridge);
                return h;
            };

            spec.heroAt = 72f;
            spec.dress = d => DressFrostlight(d, spec);
            return spec;
        }

        [MenuItem("BIKE RUSH/Courses/Build Frostlight")]
        public static void BuildFrostlight() { Build(Frostlight()); }

        static string AlpineT(string layer) { return "Assets/Synty/PolygonNatureBiomes/PNB_Alpine_Mountain/Terrain/" + layer + ".terrainlayer"; }

        static string[] Paths(string dir, params string[] names)
        {
            var r = new string[names.Length];
            for (int i = 0; i < names.Length; i++) r[i] = dir + names[i];
            return r;
        }

        // Plain URP Lit stand-ins for the pack's triplanar snow and ice: that shader samples more textures than a
        // browser allows (see RedRockMaterial).
        static string FrostSnowMat() { SimpleMat("M_FrostSnow", new Color(0.93f, 0.95f, 1f), Color.black, 0.18f); return ArtDir + "M_FrostSnow.mat"; }
        static string FrostIceMat() { SimpleMat("M_FrostIce", new Color(0.66f, 0.82f, 0.95f), Color.black, 0.82f); return ArtDir + "M_FrostIce.mat"; }

        static readonly Color[] BulbColours =
        {
            new Color(1f, 0.25f, 0.2f), new Color(0.3f, 1f, 0.45f), new Color(1f, 0.78f, 0.3f), new Color(0.35f, 0.6f, 1f), new Color(1f, 0.45f, 0.85f),
        };

        static void DressFrostlight(Dress d, Spec spec)
        {
            string H = Holiday, A = AlpineP, Ap = AlpineP + "Props/", WB = West + "Buildings/";
            var postMat = d.Mat("M_FrostPost", new Color(0.12f, 0.1f, 0.1f), 0.3f);
            var red = d.Mat("M_FrostRed", new Color(0.62f, 0.08f, 0.08f), 0.25f);
            var roofSnow = d.Mat("M_FrostRoof", new Color(0.94f, 0.96f, 1f), 0.15f);
            var warm = d.Mat("M_FrostWarm", new Color(1f, 0.82f, 0.5f), 0.5f, new Color(3.2f, 2.1f, 0.9f));
            var bulbs = new Material[BulbColours.Length];
            for (int k = 0; k < bulbs.Length; k++) bulbs[k] = d.Mat("M_FrostBulb" + k, BulbColours[k], 0.6f, BulbColours[k] * 4f);

            // ---- Village Square: log cabins both sides, string lights strung across the street, icicle lights on posts.
            var village = d.Group("Village Square");
            string[] cabins = { WB + "SM_Bld_Cabin_01", Ap + "SM_Prop_Cabin_01", WB + "SM_Bld_Cabin_01", Ap + "SM_Prop_Cabin_01", WB + "SM_Bld_Barn_01" };
            int s0 = d.Cp(19), s1 = d.Cp(2);
            for (int side = -1; side <= 1; side += 2)
            {
                d.Row(cabins, village, d.Along(s0, 8f + side * 4f), d.Along(s1, -10f), 14f, side, 19f, 6f, 1.05f, 1.2f);
                d.Row(new[] { H + "SM_Bld_Pillar_Lights_Icicles_01" }, village, d.Along(s0, 12f), d.Along(s1, -6f), 14f, side, 10.2f, 0f, 1.3f, 1.3f, false, 0f);
                d.Row(new[] { H + "SM_Prop_Present_01", Ap + "SM_Prop_Wood_Pile_01", H + "SM_Prop_Present_01", Ap + "SM_Prop_Bench_01", H + "SM_Prop_Snowman_01" }, village, d.Along(s0, 16f), d.Along(s1, -12f), 11f, side, 13f, 40f, 0.9f, 1.15f);
            }
            for (float a = d.Dist(s0) + 6f; a < d.Dist(s1) - 4f; a += 15f) StreetLights(d, village, d.AtDistance(a), 11.6f, postMat, bulbs);

            // The giant tree on the plaza, presents round its foot and a light of its own.
            int ti = d.Along(d.Cp(0), 22f);
            var tree = d.Side(A + "SM_Env_Pine_02", village, ti, 1f, 15.5f, 0f, 0.78f);
            if (tree != null)
            {
                var tp = tree.transform.position;
                // The pack's light tree is a cone of light strings: stretched round the pine it's the tree's lights.
                var lights = d.At(H + "SM_Prop_Light_Tree_01", village, tp + Vector3.up * 0.6f, 0f, 1f, true, 0f);
                if (lights != null) lights.transform.localScale = new Vector3(1.75f, 3.1f, 1.75f);
                for (int k = 0; k < 16; k++)
                {
                    float ht = Random.Range(2.5f, 10.5f), rad = Mathf.Lerp(3.1f, 0.7f, (ht - 2.5f) / 8f), ang = Random.Range(0f, Mathf.PI * 2f);
                    d.At(H + "SM_Prop_Bauble_01", village, tp + new Vector3(Mathf.Cos(ang) * rad, ht, Mathf.Sin(ang) * rad), Random.Range(0f, 360f), 1.1f, true, 0f);
                }
                d.At(H + "SM_Prop_Neon_Snowflake_02", village, tp + Vector3.up * 13.4f, Quaternion.LookRotation(-d.Fwd(ti)).eulerAngles.y, 2.2f, true, 0f);
                for (int k = 0; k < 9; k++)
                {
                    float ang = k / 9f * Mathf.PI * 2f;
                    d.At(H + "SM_Prop_Present_01", village, tp + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * Random.Range(4.4f, 5.6f), Random.Range(0f, 360f), Random.Range(0.9f, 1.4f));
                }
                PointLight(village, tp + Vector3.up * 5f, new Color(1f, 0.75f, 0.45f), 18f, 3.2f);
                d.Keep(tp, 8f);
            }
            d.Side(H + "SM_Prop_Snowman_01", village, d.Along(d.Cp(0), 4f), -1f, 12f, 30f, 1.2f);
            d.Side(H + "SM_Prop_Reindeer_01", village, d.Along(d.Cp(1), 14f), -1f, 12.5f, 90f, 1.15f);

            // MERRY CHRISTMAS across the street on a beam between two posts.
            Banner(d, village, d.Cp(1), 8.6f, postMat, red, new[] { H + "SM_Prop_Neon_Text_Merry_01", H + "SM_Prop_Neon_Text_Christmas_01" }, null);

            // ---- Candy Cane Gate: two canes curling in over the trail, the trail's name between them.
            var gate = d.Group("Candy Cane Gate");
            int g = d.Cp(2);
            var gp = d.Point(g); var gr = d.Right(g); var gf = d.Fwd(g);
            float gy = d.Ground(gp);
            for (int side = -1; side <= 1; side += 2)
            {
                var foot = gp + gr * side * (HalfWidth + 3.4f); foot.y = d.Ground(foot);
                // The hook curls toward the cane's +Z: point it in over the trail.
                d.At(H + "SM_Prop_Candy_Cane_01", gate, foot, Quaternion.LookRotation(-gr * side).eulerAngles.y, 5.4f, true, 0f);
                d.At(H + "SM_Prop_Neon_Snowflake_01", gate, foot + Vector3.up * 3.2f - gf * 0.4f, Quaternion.LookRotation(-gf).eulerAngles.y, 1.6f, true, 0f);
                PointLight(gate, foot + Vector3.up * 4f - gf * 2f, new Color(1f, 0.55f, 0.55f), 12f, 2.2f);
            }
            var board = Cube(gate, "Gate Board", new Vector3(gp.x, gy + 6.3f, gp.z), new Vector3(9.4f, 1.3f, 0.3f), gf, red);
            Object.DestroyImmediate(board.GetComponent<Collider>());
            SignText(gate, "FROSTLIGHT", new Vector3(gp.x, gy + 6.3f, gp.z) - gf * 0.18f, gf, new Color(1f, 0.95f, 0.85f), 1.15f);
            SignText(gate, "RIDE SAFE  •  MERRY CHRISTMAS", new Vector3(gp.x, gy + 6.3f, gp.z) + gf * 0.18f, -gf, new Color(1f, 0.95f, 0.85f), 0.7f);

            // ---- Frozen Lake: fishing huts out on the ice, a camp on the shore, neon snowflakes along the ice road.
            var lake = d.Group("Frozen Lake");
            d.Side(Ap + "SM_Prop_Jetty_01", lake, d.Along(d.Cp(4), -6f), -1f, 9f, 0f, 1.3f);
            d.Side(Ap + "SM_Prop_Ice_Fishing_Hut_01", lake, d.Along(d.Cp(4), 30f), 1f, 22f, 25f, 1.3f);
            d.Side(Ap + "SM_Prop_Ice_Fishing_Hut_01", lake, d.Cp(5), -1f, 28f, -40f, 1.3f);
            d.Side(Ap + "SM_Prop_Ice_Fishing_Hut_01", lake, d.Along(d.Cp(5), 34f), 1f, 34f, 70f, 1.3f);
            d.Side(Ap + "SM_Prop_Canoe_01", lake, d.Along(d.Cp(4), -14f), 1f, 12f, 60f, 1.2f);
            int camp = d.Along(d.Cp(4), -20f);
            var fire = d.Side(Ap + "SM_Prop_Campfire_01", lake, camp, -1f, 16f, 0f, 1.4f);
            if (fire != null)
            {
                d.At(Ap + "SM_Prop_Bench_01", lake, fire.transform.position + d.Fwd(camp) * 2.2f, Quaternion.LookRotation(-d.Fwd(camp)).eulerAngles.y, 1.1f);
                d.At(AlpineFx + "FX_Fire_01", lake, fire.transform.position + Vector3.up * 0.2f, 0f, 1.2f, true, 0f);
                PointLight(lake, fire.transform.position + Vector3.up * 1.2f, new Color(1f, 0.55f, 0.25f), 12f, 2.6f);
            }
            string[] sheets = Paths(A, "SM_Env_Ice_Sheet_01", "SM_Env_Ice_Sheet_02", "SM_Env_Ice_Sheet_03", "SM_Env_Ice_Sheet_04", "SM_Env_Ice_Sheet_05", "SM_Env_Ice_Sheet_06");
            d.Row(sheets, lake, d.Along(d.Cp(4), 10f), d.Along(d.Cp(6), -10f), 16f, -1f, 16f, 180f, 1.2f, 2f);
            d.Row(sheets, lake, d.Along(d.Cp(4), 18f), d.Along(d.Cp(6), -10f), 18f, 1f, 18f, 180f, 1.2f, 2f);
            for (float a = d.Dist(d.Cp(4)) + 8f; a < d.Dist(d.Cp(6)) - 14f; a += 22f)
            {
                int i = d.AtDistance(a);
                float side = (Mathf.Round(a / 22f) % 2 == 0) ? 1f : -1f;
                LightPost(d, lake, i, side, HalfWidth + 6.2f, postMat, null, H + "SM_Prop_Neon_Snowflake_0" + (1 + (int)Mathf.Repeat(a / 22f, 3f)));
            }
            d.Side(H + "SM_Prop_Snowman_01", lake, d.Along(d.Cp(6), -20f), 1f, 12f, -30f, 1.3f);

            // ---- Lantern Forest: lantern posts climbing through the pines, alternating sides.
            var forest = d.Group("Lantern Forest");
            int lanterns = 0;
            for (float a = d.Dist(d.Cp(7)) + 6f; a < d.Dist(d.Cp(10)) - 18f; a += 17f)
                LightPost(d, forest, d.AtDistance(a), (lanterns++ % 2 == 0) ? 1f : -1f, HalfWidth + 2.6f, postMat, warm, null);
            d.Side(Ap + "SM_Prop_Wood_Pile_02", forest, d.Along(d.Cp(8), 10f), -1f, 9f, 20f, 1.2f);
            d.Side(Ap + "SM_Prop_Lookout_01", forest, d.Along(d.Cp(9), -12f), -1f, 14f, 0f, 1.25f);

            // ---- Sleigh Jump: the reindeer team parked beside the landing, a neon sleigh riding the sky above the gap.
            var sleigh = d.Group("Sleigh Jump");
            int land = d.Along(d.Cp(10), 26f);
            for (int k = 0; k < 4; k++)
                d.Side(H + "SM_Prop_Reindeer_01", sleigh, d.Along(land, k * 2.4f), -1f, 9.5f + (k % 2) * 1.6f, 0f, 1.35f);
            d.Side(H + "SM_Prop_Neon_Sleigh_02", sleigh, d.Along(land, -6f), -1f, 13.5f, 0f, 1.1f);
            int gap = d.Along(d.Cp(10), 10f);
            var sky = d.Point(gap) + d.Right(gap) * 6f; sky.y = d.Point(gap).y + 17f;
            var big = d.At(H + "SM_Prop_Neon_Sleigh_01", sleigh, sky, Quaternion.LookRotation(d.Right(gap)).eulerAngles.y, 2.6f, true, 0f);
            if (big != null)
            {
                foreach (var t in big.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
                var m = big.AddComponent<AmbientMotion>();
                m.kind = AmbientMotion.Kind.Bob; m.axis = Vector3.up; m.amount = 0.9f; m.speed = 0.5f;
            }
            for (int side = -1; side <= 1; side += 2)
                d.Side(H + "SM_Prop_Candy_Cane_01", sleigh, d.Along(d.Cp(10), -10f), side, HalfWidth + 1.6f, side > 0 ? 180f : 0f, 2.2f, false, 0f);

            // ---- Toy Workshop: a long red shed the trail runs straight through, presents stacked along the walls.
            Workshop(d, d.Along(d.Cp(12), -14f), d.Along(d.Cp(12), 12f), red, roofSnow, postMat, warm, bulbs);

            // ---- Ridge Run: the northern lights over the far valley, a few lights along the edge.
            var ridge = d.Group("Ridge Run");
            var centre = new Vector3(20f, 0f, 40f);
            var aur = new[] { AlpineFx + "FX_Aurora_Mesh_01", AlpineFx + "FX_Aurora_Mesh_02" };
            for (int k = 0; k < 4; k++)
            {
                var pos = new Vector3(-180f + k * 120f, 150f + (k % 2) * 25f, 420f + (k % 2) * 40f);
                var go = d.At(aur[k % 2], ridge, pos, 90f + (k - 1.5f) * 12f, 7f, true, 0f);
                if (go != null) foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
            }
            for (int k = 0; k < 3; k++)
                LightPost(d, ridge, d.Along(d.Cp(13), -10f + k * 32f), 1f, HalfWidth + 3f, postMat, null, H + "SM_Prop_Neon_Tree_01");

            // ---- Switchbacks: candy canes round the outside of each hairpin, snow banked up behind them.
            var hairpins = d.Group("Switchbacks");
            foreach (int cp in new[] { 15, 16, 17 })
            {
                int i = d.Cp(cp);
                // The outside of the turn is away from the next control point's side.
                var toNext = spec.points[(cp + 1) % spec.points.Length] - d.Point(i);
                float outside = Vector3.Dot(toNext, d.Right(i)) > 0f ? -1f : 1f;
                for (int k = -2; k <= 2; k++)
                    d.Side(H + "SM_Prop_Candy_Cane_01", hairpins, d.Along(i, k * 4f), outside, HalfWidth + 2.2f, outside > 0 ? 180f : 0f, 2.4f, false, 0f);
                d.Side(A + "SM_Env_Snow_Mound_03", hairpins, i, outside, HalfWidth + 5.5f, 0f, 3f);
            }

            // ---- Backdrop: snowy ranges all round, a ring of far forest in front of them.
            var back = d.Group("Backdrop");
            var c = new Vector3(20f, 0f, 40f);
            for (int k = 0; k < 14; k++)
            {
                float ang = k / 14f * Mathf.PI * 2f;
                var pos = c + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 470f;
                d.At(A + "SM_Env_MountainRange_0" + (k % 4 + 1), back, pos, Quaternion.LookRotation(c - pos).eulerAngles.y + k * 23f, 9f, false, -2f);
            }
            for (int k = 0; k < 22; k++)
            {
                float ang = k / 22f * Mathf.PI * 2f + 0.07f;
                var pos = c + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 300f;
                d.At(A + "SM_Env_Background_Trees_0" + (k % 2 + 1), back, pos, Quaternion.LookRotation(c - pos).eulerAngles.y, 2.4f, false, -1f);
            }
        }

        /// <summary>Two dark posts either side and a sagging string of coloured bulbs across the street between them.</summary>
        static void StreetLights(Dress d, Transform parent, int i, float halfSpan, Material post, Material[] bulbs)
        {
            var p = d.Point(i); var r = d.Right(i);
            var left = p - r * halfSpan; left.y = d.Ground(left);
            var right = p + r * halfSpan; right.y = d.Ground(right);
            const float top = 6.6f;
            foreach (var foot in new[] { left, right })
            {
                var pole = Cube(parent, "Light Pole", foot + Vector3.up * top * 0.5f, new Vector3(0.22f, top, 0.22f), d.Fwd(i), post);
                Object.DestroyImmediate(pole.GetComponent<Collider>());
            }
            const int n = 13;
            for (int k = 1; k < n; k++)
            {
                float t = k / (float)n;
                var q = Vector3.Lerp(left, right, t);
                q.y = Mathf.Lerp(left.y, right.y, t) + top - 1.1f * Mathf.Sin(t * Mathf.PI) - 0.15f;
                Ball(parent, q, 0.17f, bulbs[(k + i) % bulbs.Length]);
            }
        }

        /// <summary>A dark post at the trailside with a warm lamp on top, or a neon sign hung from it instead.</summary>
        static void LightPost(Dress d, Transform parent, int i, float side, float offset, Material post, Material lamp, string sign)
        {
            var foot = d.Point(i) + d.Right(i) * side * offset; foot.y = d.Ground(foot);
            const float h = 3.4f;
            var pole = Cube(parent, "Lantern Post", foot + Vector3.up * h * 0.5f, new Vector3(0.18f, h, 0.18f), d.Fwd(i), post);
            Object.DestroyImmediate(pole.GetComponent<Collider>());
            if (lamp != null)
            {
                var box = Cube(parent, "Lantern", foot + Vector3.up * (h + 0.25f), new Vector3(0.42f, 0.5f, 0.42f), d.Fwd(i), lamp);
                Object.DestroyImmediate(box.GetComponent<Collider>());
                var cap = Cube(parent, "Lantern Cap", foot + Vector3.up * (h + 0.56f), new Vector3(0.56f, 0.12f, 0.56f), d.Fwd(i), post);
                Object.DestroyImmediate(cap.GetComponent<Collider>());
            }
            if (!string.IsNullOrEmpty(sign))
                d.At(sign, parent, foot + Vector3.up * (h + 0.7f) + d.Right(i) * -side * 0.1f, Quaternion.LookRotation(-d.Fwd(i)).eulerAngles.y, 1.3f, true, 0f);
        }

        /// <summary>A beam across the trail on two posts, neon pieces mounted along it facing the oncoming riders.</summary>
        static void Banner(Dress d, Transform parent, int i, float height, Material post, Material board, string[] neon, string text)
        {
            var p = d.Point(i); var r = d.Right(i); var f = d.Fwd(i);
            float span = HalfWidth + 7.6f;
            foreach (int side in new[] { -1, 1 })
            {
                var foot = p + r * side * span; foot.y = d.Ground(foot);
                var pole = Cube(parent, "Banner Post", foot + Vector3.up * height * 0.5f, new Vector3(0.35f, height, 0.35f), f, post);
                Object.DestroyImmediate(pole.GetComponent<Collider>());
            }
            float gy = d.Ground(p);
            var beam = Cube(parent, "Banner Beam", new Vector3(p.x, gy + height, p.z), new Vector3(span * 2f + 0.6f, 0.9f, 0.25f), f, board);
            Object.DestroyImmediate(beam.GetComponent<Collider>());
            float x0 = -(neon.Length - 1) * 2.4f;
            for (int k = 0; k < neon.Length; k++)
            {
                var pos = new Vector3(p.x, gy + height + 1.4f, p.z) + r * (x0 + k * 4.8f) - f * 0.2f;
                d.At(neon[k], parent, pos, Quaternion.LookRotation(-f).eulerAngles.y, 2.2f, true, 0f);
            }
            if (!string.IsNullOrEmpty(text)) SignText(parent, text, new Vector3(p.x, gy + height, p.z) - f * 0.14f, f, Color.white, 0.8f);
        }

        /// <summary>A long red shed straddling the trail between two points: walls, a snowy roof, presents and lights inside.</summary>
        static void Workshop(Dress d, int from, int to, Material walls, Material roof, Material post, Material warm, Material[] bulbs)
        {
            var root = d.Group("Toy Workshop");
            float halfIn = HalfWidth + 2.8f;
            const float wallH = 5.2f, step = 3f;
            float d0 = d.Dist(from), d1 = d.Dist(to);
            int n = 0;
            for (float a = d0; a < d1; a += step, n++)
            {
                int i = d.AtDistance(a + step * 0.5f);
                var p = d.Point(i); var r = d.Right(i); var f = d.Fwd(i);
                float gy = d.Ground(p);
                foreach (int side in new[] { -1, 1 })
                {
                    var wp = p + r * side * halfIn; wp.y = gy + wallH * 0.5f - 0.2f;
                    Cube(root, "Wall", wp, new Vector3(0.35f, wallH, step + 0.45f), f, walls);
                    // Roof pitch: two slabs meeting over the middle.
                    var rp = p + r * side * halfIn * 0.5f; rp.y = gy + wallH + 0.9f;
                    var slab = Cube(root, "Roof", rp, new Vector3(halfIn + 1.2f, 0.3f, step + 0.45f), f, roof);
                    Object.DestroyImmediate(slab.GetComponent<Collider>());
                    slab.transform.rotation = Quaternion.LookRotation(f, Vector3.up) * Quaternion.Euler(0f, 0f, side * -22f);
                    if (n % 2 == 0)
                    {
                        // Presents stacked against the wall, inside.
                        var foot = p + r * side * (halfIn - 1.1f); foot.y = gy;
                        d.At(Holiday + "SM_Prop_Present_01", root, foot, Random.Range(0f, 360f), Random.Range(1f, 1.5f));
                        d.At(Holiday + "SM_Prop_Present_01", root, foot + f * 0.9f + Vector3.up * 0.9f, Random.Range(0f, 360f), Random.Range(0.8f, 1.1f), true, 0f);
                    }
                }
                // A string of bulbs along the ridge line inside.
                Ball(root, p + Vector3.up * (gy - p.y + wallH + 0.2f), 0.16f, bulbs[n % bulbs.Length]);
            }
            int mid = d.AtDistance((d0 + d1) * 0.5f);
            PointLight(root, d.Point(mid) + Vector3.up * (d.Ground(d.Point(mid)) - d.Point(mid).y + 4f), new Color(1f, 0.8f, 0.55f), 16f, 2.8f);
            // The name over the way in.
            var ip = d.Point(from); var ifwd = d.Fwd(from); float iy = d.Ground(ip);
            var board = Cube(root, "Workshop Sign", new Vector3(ip.x, iy + wallH + 0.4f, ip.z) - ifwd * 0.3f, new Vector3(7.5f, 1f, 0.25f), ifwd, post);
            Object.DestroyImmediate(board.GetComponent<Collider>());
            SignText(root, "TOY WORKSHOP", new Vector3(ip.x, iy + wallH + 0.4f, ip.z) - ifwd * 0.45f, ifwd, new Color(1f, 0.85f, 0.45f), 0.95f);
            d.Keep(d.Point(mid), (d1 - d0) * 0.5f + 6f);
        }

        static void Ball(Transform parent, Vector3 pos, float radius, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Bulb";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * radius * 2f;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.isStatic = true;
        }

        static void PointLight(Transform parent, Vector3 pos, Color c, float range, float intensity)
        {
            var go = new GameObject("Warm Light");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
        }
    }
}
