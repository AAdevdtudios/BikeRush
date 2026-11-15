using ShiftRush.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShiftRush.AirShow
{
    /// <summary>
    /// The Air Show HUD (Resources/BR_AirShowHUD, built by BIKE RUSH → Menu → Build Air Show HUD): the score
    /// rolling up top left, the run clock and best top right, a course strip along the top with the bike's
    /// spot on it, a points pop for each landing, the countdown, and the results card with a rank.
    /// </summary>
    public class AirShowHUD : MonoBehaviour
    {
        const string MenuScene = "MainMenu";

        [SerializeField] TMP_Text scoreValue, timeValue, bestValue, venueText;
        [SerializeField] RectTransform progressFill, progressBike;
        [SerializeField] CanvasGroup popGroup;
        [SerializeField] TMP_Text popPoints, popName, popGrade;
        [SerializeField] TMP_Text countdownText, hintText;
        [SerializeField] CanvasGroup hintGroup;
        [SerializeField] CanvasGroup finishGroup, playGroup;
        [SerializeField] TMP_Text finScore, finRank, finBest, finStats, finTitle;
        [Header("Rank stamp")]
        [SerializeField] RectTransform rankRoot;
        [SerializeField] Image rankFlash;
        [SerializeField] TMP_Text rankLabel;
        [SerializeField] float countSeconds = 1.3f;
        [SerializeField] float stampSeconds = 0.17f;
        [SerializeField] AudioClip stamp, tick;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip beep, go;
        [SerializeField] Color accent = new Color32(255, 158, 46, 255);
        [SerializeField] Color green = new Color32(61, 255, 138, 255);
        [SerializeField] Color red = new Color32(255, 90, 110, 255);

        AirShowRun run;
        float shownScore, popUntil, popPunch, countPunch, finishAt = -1f, hintUntil;
        float revealAt = -1f, nextTick;
        bool stamped;
        Color rankColour;
        Vector2 finHome, rankHome;

        void Start()
        {
            run = AirShowRun.Current;
            if (run == null) { gameObject.SetActive(false); return; }
            run.Scored += OnScored;
            run.CountdownTick += OnCount;
            run.Finished += OnFinished;
            if (finishGroup != null) { finishGroup.alpha = 0f; finishGroup.blocksRaycasts = false; finHome = ((RectTransform)finishGroup.transform).anchoredPosition; }
            if (rankRoot != null) rankHome = rankRoot.anchoredPosition;
            if (popGroup != null) popGroup.alpha = 0f;
            if (countdownText != null) countdownText.text = "";
            if (venueText != null) venueText.text = run.Venue + "  <color=#9AA0B4>//  AIR SHOW</color>";
            if (hintText != null)
            {
                string k = ShiftRush.Vehicles.ControlBindings.KeyName("NOS", "keyboard", "positive");
                if (string.IsNullOrEmpty(k)) k = "SHIFT";
                hintText.text = "FLY THROUGH THE <color=#FF9E2E>BOXES</color>  //  HIT <color=#FF9E2E>" + k.ToUpperInvariant() + "</color> ON THE DIAMOND";
            }
            hintUntil = Time.unscaledTime + 7f;
        }

        void OnDestroy()
        {
            if (run == null) return;
            run.Scored -= OnScored;
            run.CountdownTick -= OnCount;
            run.Finished -= OnFinished;
        }

        void OnCount(int n)
        {
            countPunch = 1f;
            if (countdownText != null)
            {
                countdownText.text = n > 0 ? n.ToString() : "SHOWTIME";
                countdownText.color = n > 0 ? Color.white : accent;
            }
            if (audioSource != null) audioSource.PlayOneShot(n > 0 ? beep : go, n > 0 ? 0.8f : 1f);
        }

        void OnScored(int points, string headline, string grade)
        {
            bool bail = grade == "BAIL";
            if (popPoints != null)
            {
                popPoints.text = bail ? "BAIL" : "+" + points.ToString("N0");
                popPoints.color = bail ? red : grade == "PERFECT" ? green : accent;
            }
            if (popName != null) popName.text = headline;
            if (popGrade != null) popGrade.text = bail ? "" : grade;
            popUntil = Time.unscaledTime + 1.8f;
            popPunch = 1f;
        }

        void OnFinished()
        {
            finishAt = Time.unscaledTime + 1.2f;
            if (countdownText != null) countdownText.text = "";
            if (finTitle != null) finTitle.text = run.NewBest && run.Score > 0 ? "NEW HIGH SCORE" : "SHOW'S OVER";
            if (finScore != null) finScore.text = run.Score.ToString("N0");
            string rank = run.Rank(run.Score);
            rankColour = rank == "S" ? green : rank == "A" ? accent : rank == "B" ? Color.white : new Color(0.6f, 0.63f, 0.7f);
            if (finRank != null) { finRank.text = rank; SetRank(0f, 3f, -22f); }
            if (rankLabel != null) rankLabel.alpha = 0f;
            if (finScore != null) finScore.text = "0";
            if (finBest != null) finBest.text = run.Best.ToString("N0");
            if (finStats != null)
                finStats.text = "<color=#9AA0B4>TRICKS LANDED</color>    " + run.Tricks
                    + "\n<color=#9AA0B4>PERFECT</color>    <color=#3DFF8A>" + run.Perfects + "</color>"
                    + "\n<color=#9AA0B4>BIGGEST COMBO</color>    x" + run.BestCombo
                    + "\n<color=#9AA0B4>BAILS</color>    " + run.Bails
                    + "\n<color=#9AA0B4>TIME</color>    " + Clock(run.RunTime);
        }

        void Update()
        {
            if (run == null) return;
            float dt = Time.unscaledDeltaTime;
            shownScore = Mathf.MoveTowards(shownScore, run.Score, Mathf.Max(400f, Mathf.Abs(run.Score - shownScore) * 5f) * dt);
            if (scoreValue != null) scoreValue.text = Mathf.RoundToInt(shownScore).ToString("N0");
            if (timeValue != null) timeValue.text = Clock(run.RunTime);
            if (bestValue != null) bestValue.text = run.Best > 0 ? run.Best.ToString("N0") : "--";

            float prog = run.Progress01;
            if (progressFill != null) progressFill.anchorMax = new Vector2(prog, 1f);
            if (progressBike != null) { progressBike.anchorMin = progressBike.anchorMax = new Vector2(prog, 0.5f); progressBike.anchoredPosition = Vector2.zero; }

            popPunch = Mathf.MoveTowards(popPunch, 0f, dt * 4f);
            if (popGroup != null)
            {
                popGroup.alpha = Mathf.MoveTowards(popGroup.alpha, Time.unscaledTime < popUntil ? 1f : 0f, dt * 6f);
                popGroup.transform.localScale = Vector3.one * (1f + 0.25f * popPunch * popPunch);
            }

            countPunch = Mathf.MoveTowards(countPunch, 0f, dt * 3f);
            if (countdownText != null)
            {
                countdownText.transform.localScale = Vector3.one * (1f + 0.5f * countPunch * countPunch);
                bool show = run.CurrentState == AirShowRun.State.Countdown || (run.CountdownNumber == 0 && run.RunTime < 0.9f);
                var c = countdownText.color; c.a = show && run.CountdownNumber >= 0 && run.CountdownNumber < 4 ? 1f : Mathf.MoveTowards(c.a, 0f, dt * 4f);
                countdownText.color = c;
            }
            if (hintGroup != null) hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, Time.unscaledTime < hintUntil && run.CurrentState != AirShowRun.State.Finished ? 1f : 0f, dt * 2f);

            if (finishAt < 0f || Time.unscaledTime < finishAt) return;
            if (finishGroup != null) finishGroup.alpha = Mathf.MoveTowards(finishGroup.alpha, 1f, dt * 3f);
            if (playGroup != null) playGroup.alpha = Mathf.MoveTowards(playGroup.alpha, 0.2f, dt * 3f);
            if (finishGroup != null && finishGroup.alpha < 0.9f) return;
            if (revealAt < 0f) revealAt = Time.unscaledTime;
            bool done = Reveal(Time.unscaledTime - revealAt);
            if (SceneLoader.IsLoading || ShiftRush.UI.PauseMenu.IsPaused) return;
            var kb = Keyboard.current; var pad = Gamepad.current;
            bool again = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            bool menu = (kb != null && kb.mKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame);
            if (!done) { if (again) revealAt -= 10f; return; } // first press skips to the stamp
            if (again) RideAgain();
            else if (menu) SceneLoader.Load(MenuScene, "MAIN MENU", "BIKE//RUSH", "");
        }

        /// <summary>
        /// The results reveal: the score rolls up from zero with a ticking counter, a beat, then the rank letter
        /// drops out of the sky huge and tilted, slams down onto the card (flash, thud, the card jolts) and
        /// settles with a little bounce, left at a rubber-stamp angle. True once it has all landed.
        /// </summary>
        bool Reveal(float t)
        {
            // 1. Count up.
            float k = Mathf.Clamp01(t / countSeconds);
            float eased = 1f - Mathf.Pow(1f - k, 3f);
            int shown = Mathf.RoundToInt(run.Score * eased);
            if (finScore != null) finScore.text = shown.ToString("N0");
            if (k < 1f && run.Score > 0 && Time.unscaledTime >= nextTick)
            {
                nextTick = Time.unscaledTime + Mathf.Lerp(0.045f, 0.12f, eased);
                if (audioSource != null && tick != null) audioSource.PlayOneShot(tick, 0.35f);
            }

            // 2. The stamp: falls for stampSeconds, accelerating, then bounces.
            float s = t - countSeconds - 0.3f;
            if (s < 0f) { SetRank(0f, 3f, -22f); return false; }
            if (s < stampSeconds)
            {
                float f = s / stampSeconds, e = f * f; // speeding up into the card
                SetRank(Mathf.Clamp01(f * 3f), Mathf.Lerp(3f, 0.88f, e), Mathf.Lerp(-22f, -9f, e));
                return false;
            }
            if (!stamped)
            {
                stamped = true;
                if (audioSource != null)
                {
                    if (stamp != null) audioSource.PlayOneShot(stamp, 1f);
                    if (beep != null) audioSource.PlayOneShot(beep, 0.6f);
                }
            }
            float a = s - stampSeconds;
            // Bounce up from the squash and settle.
            float bounce = 1f - 0.12f * Mathf.Cos(a * 22f) * Mathf.Exp(-a * 9f); // a damped spring out of the squash
            SetRank(1f, bounce, -9f + 1.5f * Mathf.Clamp01(a / 0.28f));
            // Flash ring and the card's jolt.
            if (rankFlash != null)
            {
                float fl = Mathf.Clamp01(1f - a / 0.45f);
                rankFlash.color = new Color(rankColour.r, rankColour.g, rankColour.b, 0.75f * fl * fl);
                rankFlash.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.9f, 1f - fl * fl);
            }
            float shake = Mathf.Clamp01(1f - a / 0.32f);
            Vector2 jolt = new Vector2(Mathf.Sin(a * 90f), Mathf.Cos(a * 71f)) * 16f * shake * shake;
            if (finishGroup != null) ((RectTransform)finishGroup.transform).anchoredPosition = finHome + jolt * 0.5f;
            if (rankRoot != null) rankRoot.anchoredPosition = rankHome + jolt;
            if (rankLabel != null) rankLabel.alpha = Mathf.Clamp01(a / 0.3f);
            return a > 0.3f;
        }

        void SetRank(float alpha, float scale, float angle)
        {
            if (finRank == null) return;
            var c = rankColour; c.a = alpha;
            finRank.color = c;
            finRank.rectTransform.localScale = Vector3.one * scale;
            finRank.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        public static void RideAgain()
        {
            string scene = SceneManager.GetActiveScene().name;
            var car = RaceSession.Car;
            SceneLoader.Load(scene, RideModes.AirShowVenue, (car != null ? car.displayName.ToUpperInvariant() + "  //  " : "") + "AIR SHOW", RideModes.AirShowTip());
        }

        static string Clock(float t)
        {
            int m = (int)(t / 60f);
            float s = t - m * 60f;
            return m.ToString("00") + ":" + s.ToString("00.00");
        }
    }
}
