using UnityEngine;

namespace ShiftRush.Core
{
    /// <summary>
    /// The three race levels: CRUISE (gentle), RUSH (the race as designed, default) and OVERDRIVE (no mercy).
    /// Chosen on the track select screen or in Settings → Gameplay (one saved value, Heat selector style).
    /// A first race with the tutorial on always runs on CRUISE.
    /// </summary>
    public static class Difficulty
    {
        public const string Key = "SR_Difficulty";
        public const int Default = 1;
        public static readonly string[] Labels = { "CRUISE", "RUSH", "OVERDRIVE" };

        public struct Tuning
        {
            /// <summary>Rivals' top speed and cornering grip, × this.</summary>
            public float pace;
            /// <summary>Rubber band, rivals behind you: up to this much faster (0.15 = +15%) when far back.</summary>
            public float push;
            /// <summary>Rubber band, rivals ahead of you: up to this much slower when far ahead (0 = they never wait).</summary>
            public float ease;
            /// <summary>Rivals' cars: engine torque, top speed and tyre grip, × this (their real ability, not just intent).</summary>
            public float carBoost;
            /// <summary>Nitro rivals gain per second on top of SHIFT ramps (share of a tank).</summary>
            public float rivalNitro;
            /// <summary>Your start: 0 = pole, 5 = last of six.</summary>
            public int gridSlot;
            /// <summary>The SHIFT timing window, × this.</summary>
            public float shiftWindow;
            /// <summary>Where a cleared SHIFT world drops you: a random place from warpBest to warpWorst.</summary>
            public int warpBest, warpWorst;
            /// <summary>Slipstream, close call, wall skim and stunt boost, × this.</summary>
            public float bonusNitro;
            /// <summary>How fast your boost tank refills by itself, × the bike's own rate.</summary>
            public float boostRefill;
            public string blurb;
        }

        static readonly Tuning[] Levels =
        {
            // Boost refill × the bike's own 6.5% a second: CRUISE about 17 s for a full tank, RUSH about 34 s,
            // OVERDRIVE about 70 s, so on the hard levels boost comes from stunts, drafting and close calls.
            new Tuning { pace = 0.95f, push = 0.05f, ease = 0.1f, carBoost = 1f, rivalNitro = 0f, gridSlot = 2, warpBest = 1, warpWorst = 2, shiftWindow = 1.3f, bonusNitro = 1.1f, boostRefill = 0.9f,
                blurb = "Gentler rivals, a wider landing window, boost comes back quickly. You start 3rd." },
            new Tuning { pace = 1.08f, push = 0.1f, ease = 0.02f, carBoost = 1.06f, rivalNitro = 0.01f, gridSlot = 4, warpBest = 1, warpWorst = 3, shiftWindow = 1f, bonusNitro = 0.8f, boostRefill = 0.45f,
                blurb = "Rivals who race you properly. Boost refills slowly: stunts and drafting top it up. You start 5th." },
            new Tuning { pace = 1.2f, push = 0.2f, ease = 0f, carBoost = 1.18f, rivalNitro = 0.04f, gridSlot = 5, warpBest = 2, warpWorst = 4, shiftWindow = 0.75f, bonusNitro = 0.6f, boostRefill = 0.22f,
                blurb = "Fast rivals who never wait, a tight landing window, boost barely refills on its own. Earn every bit. You start last." },
        };

        /// <summary>The level the player picked.</summary>
        public static int Chosen
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt("HorizontalSelector_" + Key, Default), 0, Levels.Length - 1); }
            set { PlayerPrefs.SetInt("HorizontalSelector_" + Key, Mathf.Clamp(value, 0, Levels.Length - 1)); }
        }

        /// <summary>
        /// Still learning this mode: the tutorial is on and its lessons aren't finished yet. Only then does a race
        /// run on CRUISE whatever was picked (it used to be whenever the tutorial switch was on, which is always
        /// by default, so the harder levels never applied).
        /// </summary>
        public static bool LearningNow
        {
            get { return GameSettings.Tutorial && PlayerPrefs.GetInt("BR_Tutorial_" + RaceSession.Mode + "_Learnt", 0) != 1; }
        }

        /// <summary>The level this race runs on: CRUISE while learning the mode, else the pick.</summary>
        public static int Level { get { return LearningNow ? 0 : Chosen; } }
        public static Tuning Current { get { return Levels[Level]; } }
        public static string Label { get { return Labels[Level]; } }
        public static Tuning Of(int level) { return Levels[Mathf.Clamp(level, 0, Levels.Length - 1)]; }
    }
}
