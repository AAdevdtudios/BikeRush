using UnityEngine;

namespace ShiftRush.Core
{
    /// <summary>The three ways to ride, picked at the Start Gate before the trail.</summary>
    public enum RideMode { AirShow = 0, GhostRun = 1, PackRace = 2 }

    public static class RideModes
    {
        public struct Info
        {
            public RideMode mode;
            public string name, tag, description, badge;
            public Color accent;
            public bool ready; // false = still being built; the card says so
        }

        public static readonly Info[] All =
        {
            new Info { mode = RideMode.AirShow, name = "AIR SHOW", tag = "SIDE-ON  //  STUNTS",
                description = "The camera swings side-on over a stunt course. Fly through the boxes, hit Shift on the diamond, land it clean.",
                badge = "", accent = new Color32(255, 158, 46, 255), ready = true },
            new Info { mode = RideMode.GhostRun, name = "GHOST RUN", tag = "SOLO  //  AGAINST THE CLOCK",
                description = "Empty trail, one rider, one clock. Chase your best line and race the ghost of your best run.",
                badge = "", accent = new Color32(64, 224, 255, 255), ready = true },
            new Info { mode = RideMode.PackRace, name = "PACK RACE", tag = "6 RIDERS  //  FIRST OVER THE LINE",
                description = "Elbows out against five rivals. Draft, grab the boost pads and be first to the finish.",
                badge = "", accent = new Color32(255, 64, 217, 255), ready = true },
        };

        /// <summary>Air Show skips the trail list: it rides its own side-on venue.</summary>
        public const string AirShowScene = "Show_Cloudbreak", AirShowVenue = "CLOUDBREAK";

        static readonly string[] airTips =
        {
            "The Shift prompt only opens inside a glowing box. No box, no trick.",
            "Hit Shift as the diamond lands on the target: PERFECT pays half as much again.",
            "Boost into the big boxes: more speed, more air, bigger tricks.",
            "Each trick in one jump stacks the combo. Height buys you time for another.",
        };
        public static string AirShowTip() { return airTips[Random.Range(0, airTips.Length)]; }

        public static Info Get(RideMode m) { return All[Mathf.Clamp((int)m, 0, All.Length - 1)]; }
    }
}
