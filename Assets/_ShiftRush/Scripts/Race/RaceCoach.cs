using ShiftRush.AirShow;
using ShiftRush.Audio;
using ShiftRush.Core;
using ShiftRush.UI;
using ShiftRush.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShiftRush.Race
{
    /// <summary>
    /// The tutorial for first-timers: a coach card along the bottom of the screen (Resources/SR_Coach, built with the
    /// menu). No tutorial level; each lesson shows up when it matters and clears when the player does it. Every mode
    /// has its own short list, taught once:
    /// <list type="bullet">
    /// <item>PACK RACE: pedal, boost, boost pads.</item>
    /// <item>GHOST RUN: pedal, reading the ghost gap, boost.</item>
    /// <item>AIR SHOW: pedal, fly through the boxes (the stunt timing itself is taught by
    /// <see cref="Stunts.StuntDirector"/>'s own lesson).</item>
    /// </list>
    /// A mode's lessons are saved as they're done and the mode counts as learnt after its first finished run, so nobody
    /// sees them twice. A "stuck? press R" tip can still show. Settings → Gameplay → Tutorial off silences it all;
    /// switching it back on replays every mode's lessons.
    /// </summary>
    public class RaceCoach : MonoBehaviour
    {
        enum Lesson { Pedal, Boost, Pads, Ghost, Boxes, Stuck, Done, None }

        const string KeyPrefix = "BR_Tutorial_", SwitchMemo = "BR_TutorialSwitchWas";

        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform card;
        [SerializeField] Image frame, keyCap;
        [SerializeField] TMP_Text keyText, tagText, headline, detail;
        [SerializeField] Image[] pips;
        [SerializeField] Color accent = new Color32(255, 64, 217, 255);
        [SerializeField] Color green = new Color32(61, 255, 138, 255);
        [Tooltip("Metres ahead a boost pad has to be for the pads lesson to come up.")]
        [SerializeField] float padWarning = 70f;

        /// <summary>Kept for the old SHIFT ramps: never on in BIKE//RUSH.</summary>
        public static bool AssistShift { get { return false; } }

        RaceManager race;
        AirShowRun show;
        IVehicle vehicle;
        IVehicleDash dash;
        RideMode mode;
        Lesson[] lessons;
        int progress;            // a bit per lesson index in `lessons`
        bool active;
        Lesson showing = Lesson.None;
        float shownAt, clearAt = -1f, cleared, appear, slowTime, boostHeld, ridden, hideAt = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded += (s, m) => { if (m == LoadSceneMode.Single) Spawn(s); };
            Spawn(SceneManager.GetActiveScene());
        }

        static void Spawn(Scene scene)
        {
            if (FindFirstObjectByType<RaceCoach>() != null) return;
            if (FindFirstObjectByType<RaceLine>() == null && FindFirstObjectByType<AirShowRun>() == null) return;
            var prefab = Resources.Load<RaceCoach>("SR_Coach");
            if (prefab == null) return;
            var coach = Instantiate(prefab);
            coach.name = "SR_Coach";
            SceneManager.MoveGameObjectToScene(coach.gameObject, scene);
        }

        void Awake()
        {
            if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
        }

        void OnEnable() { BoostPad.Hit += OnPad; VehicleReset.Respawned += OnRespawned; }
        void OnDisable() { BoostPad.Hit -= OnPad; VehicleReset.Respawned -= OnRespawned; }

        void Start()
        {
            show = FindFirstObjectByType<AirShowRun>();
            mode = show != null ? RideMode.AirShow : RaceSession.Mode == RideMode.GhostRun ? RideMode.GhostRun : RideMode.PackRace;
            lessons = mode == RideMode.AirShow ? new[] { Lesson.Pedal, Lesson.Boxes }
                    : mode == RideMode.GhostRun ? new[] { Lesson.Pedal, Lesson.Ghost, Lesson.Boost }
                    : new[] { Lesson.Pedal, Lesson.Boost, Lesson.Pads };
            CheckSwitch();
            progress = PlayerPrefs.GetInt(ModeKey, 0);
            active = GameSettings.Tutorial && !Learnt;
            RefreshPips();
        }

        string ModeKey { get { return KeyPrefix + mode; } }
        int AllBits { get { return (1 << lessons.Length) - 1; } }
        bool Learnt { get { return progress == AllBits || PlayerPrefs.GetInt(ModeKey + "_Learnt", 0) == 1; } }

        /// <summary>Tutorial switched back on (it was off): every mode's lessons come back, the stunt lesson too.</summary>
        void CheckSwitch()
        {
            int now = GameSettings.Tutorial ? 1 : 0;
            int was = PlayerPrefs.GetInt(SwitchMemo, 1);
            if (now == 1 && was == 0)
            {
                foreach (RideMode m in System.Enum.GetValues(typeof(RideMode)))
                {
                    PlayerPrefs.DeleteKey(KeyPrefix + m);
                    PlayerPrefs.DeleteKey(KeyPrefix + m + "_Learnt");
                }
                PlayerPrefs.DeleteKey("BR_StuntLessons");
            }
            PlayerPrefs.SetInt(SwitchMemo, now);
        }

        int IndexOf(Lesson l) { for (int i = 0; i < lessons.Length; i++) if (lessons[i] == l) return i; return -1; }
        bool Counted(Lesson l) { return IndexOf(l) >= 0; }
        bool IsDone(Lesson l) { int i = IndexOf(l); return i < 0 || (progress & (1 << i)) != 0; }
        int DoneCount { get { int n = 0; for (int i = 0; i < lessons.Length; i++) if ((progress & (1 << i)) != 0) n++; return n; } }

        // ------------------------------------------------------------------ events

        void OnPad(BikeVehicleAdapter bike, BoostPad pad)
        {
            if (!active || bike == null || bike.IsAIDriven) return;
            Complete(Lesson.Pads);
        }

        void OnRespawned(VehicleReset r)
        {
            if (showing == Lesson.Stuck) Hide();
            slowTime = 0f;
        }

        // ------------------------------------------------------------------ the run, whichever mode

        bool Riding { get { return race != null ? race.CurrentState == RaceManager.State.Racing : show != null && show.CurrentState == AirShowRun.State.Riding; } }
        bool Over { get { return race != null ? race.CurrentState == RaceManager.State.Finished : show != null && show.CurrentState == AirShowRun.State.Finished; } }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (race == null && RaceManager.Current != null) race = RaceManager.Current;
            if (vehicle == null)
            {
                if (race != null && race.Player != null) vehicle = race.Player.vehicle;
                else if (show != null && show.Bike != null) vehicle = show.Bike;
                dash = vehicle as IVehicleDash;
            }
            if (PauseMenu.IsPaused && lessons != null) FollowSetting(); // the pause menu has the switch too
            if (hideAt > 0f && Time.unscaledTime >= hideAt) { hideAt = -1f; Hide(); }
            if (vehicle != null && lessons != null) Think();
            Animate(dt);
        }

        void FollowSetting()
        {
            bool want = GameSettings.Tutorial;
            if (!want) { if (active) { active = false; Hide(); } PlayerPrefs.SetInt(SwitchMemo, 0); return; }
            if (active) return;
            CheckSwitch();
            progress = PlayerPrefs.GetInt(ModeKey, 0);
            active = !Learnt;
            RefreshPips();
        }

        void Think()
        {
            float speed = Mathf.Abs(vehicle.SpeedKmh);
            if (Over)
            {
                if (showing != Lesson.None && showing != Lesson.Done) Hide();
                Learn(); // one finished run and this mode is learnt
                return;
            }
            if (!active) return;
            if (Riding) ridden += Time.deltaTime;

            if (clearAt > 0f)
            {
                if (Time.unscaledTime >= clearAt) { clearAt = -1f; if (showing != Lesson.Done) Hide(); }
                return;
            }
            switch (showing)
            {
                case Lesson.Pedal:
                    if (Riding && speed > 25f) Complete(Lesson.Pedal);
                    return;
                case Lesson.Boost:
                    if (dash != null && dash.NitroActive) boostHeld += Time.unscaledDeltaTime;
                    if (boostHeld > 0.5f) Complete(Lesson.Boost);
                    return;
                case Lesson.Pads:
                    if (Time.unscaledTime - shownAt > 8f) Hide(); // rode past: it comes back at the next one
                    return;
                case Lesson.Ghost:
                    if (Time.unscaledTime - shownAt > 7f) Complete(Lesson.Ghost);
                    return;
                case Lesson.Boxes:
                    if (TrickBox.PlayerInside) Complete(Lesson.Boxes);
                    return;
                case Lesson.Stuck:
                    if (speed > 20f) Hide();
                    return;
                case Lesson.Done:
                    return;
            }

            // Nothing showing: what's next?
            if (!IsDone(Lesson.Pedal)) { Show(Lesson.Pedal); return; }
            if (!Riding) return;
            if (!IsDone(Lesson.Ghost) && ridden > 1.5f) { Show(Lesson.Ghost); return; }
            if (!IsDone(Lesson.Boxes) && ridden > 0.5f) { Show(Lesson.Boxes); return; }
            if (!IsDone(Lesson.Pads) && PadAhead()) { Show(Lesson.Pads); return; }
            if (!IsDone(Lesson.Boost) && ridden > 6f && dash != null && dash.Nitro01 > 0.3f && vehicle.AnyWheelGrounded) { Show(Lesson.Boost); return; }

            // Stuck: barely moving for a few seconds while riding (scaled time, so the pause doesn't count).
            slowTime = !PauseMenu.IsPaused && speed < 5f ? slowTime + Time.deltaTime : 0f;
            if (slowTime > 3.5f || (race != null && race.WrongWay && race.Player.wrongWayTime > 4f)) { slowTime = 0f; Show(Lesson.Stuck); }
        }

        bool PadAhead()
        {
            var t = vehicle.Transform;
            foreach (var pad in BoostPad.All)
            {
                if (pad == null) continue;
                Vector3 to = pad.transform.position - t.position;
                float ahead = Vector3.Dot(to, t.forward);
                if (ahead > 10f && ahead < padWarning && Vector3.ProjectOnPlane(to, t.forward).magnitude < 12f) return true;
            }
            return false;
        }

        void Complete(Lesson l)
        {
            int i = IndexOf(l);
            if (i >= 0 && (progress & (1 << i)) == 0)
            {
                progress |= 1 << i;
                PlayerPrefs.SetInt(ModeKey, progress);
                PlayerPrefs.Save();
            }
            if (showing != l) return;
            cleared = 1f;
            clearAt = Time.unscaledTime + 0.9f;
            if (keyCap != null) keyCap.color = green;
            if (keyText != null) { keyText.text = "OK"; keyText.color = Color.black; }
            RefreshPips();
            GameSfx.Play(Sfx.RingPass, 1.4983f, 0.8f);
            if (progress == AllBits) Invoke(nameof(SignOff), 1.1f);
        }

        void SignOff()
        {
            clearAt = -1f;
            Show(Lesson.Done);
            hideAt = Time.unscaledTime + 5f;
            Learn();
            GameSfx.Play(Sfx.BestLap, 1f, 0.8f);
        }

        /// <summary>This mode is learnt: its lessons never show again (unless the Tutorial switch is turned back on).</summary>
        void Learn()
        {
            if (!active) return;
            active = false;
            progress = AllBits;
            PlayerPrefs.SetInt(ModeKey, AllBits);
            PlayerPrefs.SetInt(ModeKey + "_Learnt", 1);
            PlayerPrefs.Save();
        }

        // ------------------------------------------------------------------ card

        void Show(Lesson l)
        {
            if (showing == l) return;
            showing = l;
            shownAt = Time.unscaledTime;
            appear = 0f;
            cleared = 0f;
            clearAt = -1f;
            hideAt = l == Lesson.Stuck ? Time.unscaledTime + 6f : -1f;
            if (l == Lesson.Boost) boostHeld = 0f;
            if (keyCap != null) keyCap.color = new Color(1f, 1f, 1f, 0.1f);
            if (keyText != null) keyText.color = Color.white;

            string key, head, body;
            Words(l, out key, out head, out body);
            if (keyText != null) keyText.text = key;
            if (headline != null) headline.text = head;
            if (detail != null) detail.text = body;
            if (tagText != null)
            {
                string name = RideModes.Get(mode).name;
                tagText.text = l == Lesson.Done ? name + "  //  YOU'VE GOT IT"
                    : Counted(l) ? name + "  //  " + (DoneCount + 1).ToString("00") + " / " + lessons.Length.ToString("00")
                    : name + "  //  TIP";
            }
            RefreshPips();
            GameSfx.Play(Sfx.Overtake, 0.75f, 1.4f);
        }

        void Hide()
        {
            showing = Lesson.None;
            clearAt = -1f;
            hideAt = -1f;
        }

        void Words(Lesson l, out string key, out string head, out string body)
        {
            const string M = "<color=#FF40D9>", E = "</color>";
            string pedal = Key("Throttle", "wasd", "positive", "W"), brake = Key("Brake", "wasd", "positive", "S");
            string hop = Key("Handbrake", "keyboard", "positive", "SPACE"), boost = Key("NOS", "keyboard", "positive", "SHIFT");
            switch (l)
            {
                case Lesson.Pedal:
                    if (GameSettings.AutoAccelerate) { key = "AUTO"; head = "YOUR RIDER PEDALS FOR YOU"; }
                    else { key = pedal; head = "HOLD " + pedal + " TO PEDAL"; }
                    body = mode == RideMode.AirShow
                        ? "The bike rides its own line here: you handle speed and timing.  " + M + brake + E + " brakes  //  " + M + boost + E + " on the ground boosts"
                        : "The trail steers you round the bends  //  " + M + brake + E + " brakes  //  " + M + hop + E + " hold, let go to hop";
                    return;
                case Lesson.Boost:
                    key = boost;
                    head = "HOLD " + boost + " TO BOOST";
                    body = "The bar bottom right is your tank. It refills as you ride, faster with tricks and boost pads.";
                    return;
                case Lesson.Pads:
                    key = ">>>";
                    head = "RIDE OVER THE ARROWS";
                    body = "Boost pads fling you forward. Rivals go for them too, so pick your lane early.";
                    return;
                case Lesson.Ghost:
                    bool ghost = GhostRun.Current != null && GhostRun.Current.HasGhost;
                    key = ghost ? "GAP" : "REC";
                    head = ghost ? "BEAT YOUR GHOST" : "SET A TIME";
                    body = ghost
                        ? "Top left is your gap: <color=#0E8FB0>minus</color> means you're ahead, <color=#C8283C>plus</color> behind. Your best run is the see-through rider."
                        : "No rivals, just the clock. Finish and this run becomes your ghost to chase next time.";
                    return;
                case Lesson.Boxes:
                    key = "BOX";
                    head = "FLY THROUGH THE GLOWING BOXES";
                    body = "Hit the kickers fast. Inside a box, time slows down and you get a shot at a trick.";
                    return;
                case Lesson.Stuck:
                    key = "R";
                    head = "STUCK? PRESS R";
                    body = "Puts you back on the trail, facing the right way.";
                    return;
                case Lesson.Done:
                    key = "GO";
                    head = "THAT'S IT. GO GET IT.";
                    body = "No more hints in " + RideModes.Get(mode).name + ". Bring them back in Settings  >  Gameplay  >  Tutorial.";
                    return;
            }
            key = head = body = "";
        }

        static string Key(string action, string composite, string part, string fallback)
        {
            string k = ControlBindings.KeyName(action, composite, part);
            return string.IsNullOrEmpty(k) ? fallback : k;
        }

        void RefreshPips()
        {
            if (pips == null || lessons == null) return;
            for (int i = 0; i < pips.Length; i++)
            {
                if (pips[i] == null) continue;
                bool used = i < lessons.Length;
                pips[i].gameObject.SetActive(used);
                if (!used) continue;
                bool done = (progress & (1 << i)) != 0;
                bool now = lessons[i] == showing;
                pips[i].color = done ? green : now ? accent : new Color(1f, 1f, 1f, 0.18f);
            }
        }

        void Animate(float dt)
        {
            if (group == null) return;
            bool visible = showing != Lesson.None && !PauseMenu.IsPaused;
            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, dt * (visible ? 6f : 4f));
            appear = Mathf.MoveTowards(appear, 1f, dt * 4f);
            float e = 1f - (1f - appear) * (1f - appear) * (1f - appear);
            if (card != null) card.anchoredPosition = new Vector2(0f, Mathf.Lerp(-40f, 0f, e));
            cleared = Mathf.MoveTowards(cleared, 0f, dt * 1.5f);
            if (frame != null) frame.color = Color.Lerp(accent, green, cleared);
            if (card != null) card.localScale = Vector3.one * (1f + 0.04f * cleared);
        }
    }
}
