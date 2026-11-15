using UnityEngine;

namespace ShiftRush.Core
{
    /// <summary>
    /// The four set-up sliders on the Tune screen. Each is a trade (nothing is free): 0..10, 5 is the bike as it
    /// comes. Saved per bike. The maths lives here, apart from the physics, so the same set-up could drive any
    /// bike controller (a side-on 2.5D one included): <see cref="Apply"/> turns a bike's own numbers plus the
    /// four settings into the numbers it rides with.
    /// </summary>
    public static class BikeTuning
    {
        public const int Max = 10, Middle = 5, Count = 4;
        public const int Gearing = 0, Grip = 1, Suspension = 2, Boost = 3;

        public struct Slider
        {
            public string name, low, high, description;
        }

        public static readonly Slider[] Sliders =
        {
            new Slider { name = "GEARING", low = "LAUNCH", high = "TOP END",
                description = "Short gears jump off the line and out of every bend. Tall gears are slow to wind up but run away on the straights." },
            new Slider { name = "TYRES", low = "LOOSE", high = "PLANTED",
                description = "Loose tyres roll faster but the bike drifts wide. Planted tyres bite into the bends and cost a little top speed." },
            new Slider { name = "SUSPENSION", low = "AIR", high = "STICK",
                description = "Light and springy pops you higher off kickers and crests: more time for tricks, more chance a bump throws you. Stiff keeps you glued to the dirt." },
            new Slider { name = "BOOST", low = "BURST", high = "ENDURANCE",
                description = "Burst hits like a slingshot and empties fast. Endurance pulls softer and lasts much longer." },
        };

        /// <summary>What a bike rides with. km/h for speeds, m/s² for pulls, m/s for shoves.</summary>
        [System.Serializable]
        public struct Numbers
        {
            public float cruiseKmh, boostKmh, launchAccel, boostAccel, boostKick, burnPerSecond;
            public float carveRate, carveGrip, groundStick, takeoffPop;
        }

        /// <summary>-1..1 for a 0..10 setting (5 = 0).</summary>
        public static float Signed(int value) { return (Mathf.Clamp(value, 0, Max) - Middle) / (float)Middle; }

        public static Numbers Apply(Numbers b, int[] v)
        {
            float gear = Signed(Get(v, Gearing)), grip = Signed(Get(v, Grip)), susp = Signed(Get(v, Suspension)), boost = Signed(Get(v, Boost));
            var n = b;
            n.cruiseKmh = b.cruiseKmh + 6f * gear - 3f * grip;
            n.boostKmh = b.boostKmh + 6f * gear - 3f * grip;
            n.launchAccel = b.launchAccel * (1f - 0.3f * gear);
            n.carveRate = b.carveRate + 20f * grip;
            n.carveGrip = b.carveGrip + 40f * grip;
            n.groundStick = Mathf.Max(0f, b.groundStick + 8f * susp);
            n.takeoffPop = Mathf.Max(0f, b.takeoffPop + 1.6f * -susp);
            n.boostKick = b.boostKick * (1f - 0.5f * boost);
            n.boostAccel = b.boostAccel * (1f - 0.2f * boost);
            n.burnPerSecond = b.burnPerSecond * (1f - 0.35f * boost);
            return n;
        }

        static int Get(int[] v, int i) { return v != null && i < v.Length ? v[i] : Middle; }

        // ---- saved per bike

        static string Key(string bikeId, int slider) { return "BR_Tune_" + bikeId + "_" + slider; }

        public static int[] Load(string bikeId)
        {
            var v = new int[Count];
            for (int i = 0; i < Count; i++) v[i] = Mathf.Clamp(PlayerPrefs.GetInt(Key(bikeId, i), Middle), 0, Max);
            return v;
        }

        public static void Save(string bikeId, int[] v)
        {
            for (int i = 0; i < Count && i < v.Length; i++) PlayerPrefs.SetInt(Key(bikeId, i), Mathf.Clamp(v[i], 0, Max));
            PlayerPrefs.Save();
        }
    }
}
