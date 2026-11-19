using System.Collections.Generic;
using ShiftRush.Audio;
using ShiftRush.Core;
using ShiftRush.Stunts;
using ShiftRush.Vehicles;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShiftRush.AirShow
{
    /// <summary>
    /// Air Show: a side-on (2.5D) stunt run for points. The course runs along +X; the bike is held to that line
    /// (it steers itself, A/D does nothing) so the player only thinks about speed, boost and timing. Trick boxes
    /// hang over every jump: the Shift prompt (<see cref="StuntDirector"/>) only opens while the bike is inside one.
    /// Every landed trick scores; the run ends at the finish arch with a score, a rank and a saved best.
    /// Sits in the venue scene (built by BIKE RUSH → Courses → Build Cloudbreak); spawns its own HUD.
    /// </summary>
    public class AirShowRun : MonoBehaviour
    {
        public enum State { Countdown, Riding, Finished }

        [SerializeField] string venue = "CLOUDBREAK";
        [Tooltip("X where the run ends (the finish arch).")]
        [SerializeField] float finishX = 600f;
        [Tooltip("Below this height the bike is put back at the last checkpoint.")]
        [SerializeField] float killY = -45f;
        [Tooltip("Respawn spots in course order (the start, then before each box).")]
        [SerializeField] Transform[] checkpoints;
        [Tooltip("Scores for S, A and B; anything below is C.")]
        [SerializeField] int[] rankScores = { 20000, 14000, 8000 };
        [SerializeField] float countdownStep = 1f;
        [SerializeField] float startBoost = 0.35f;

        public static AirShowRun Current { get; private set; }

        public State CurrentState { get; private set; }
        public string Venue { get { return venue; } }
        public int CountdownNumber { get; private set; } = -1;
        public float RunTime { get; private set; }
        public int Score { get; private set; }
        public int Best { get; private set; }
        public bool NewBest { get; private set; }
        public int Tricks { get; private set; }
        public int Perfects { get; private set; }
        public int BestCombo { get; private set; }
        public int Bails { get; private set; }
        public float StartX { get; private set; }
        public float FinishX { get { return finishX; } }
        public BikeVehicleAdapter Bike { get; private set; }

        /// <summary>Points, headline ("BACKFLIP + 360"), grade ("PERFECT", "CLEAN", "BAIL", "").</summary>
        public event System.Action<int, string, string> Scored;
        public event System.Action<int> CountdownTick;
        public event System.Action Finished;

        float countdownStart, stuck;
        Rigidbody[] bodies;
        string BestKey { get { return "BR_AirShowBest_" + SceneManager.GetActiveScene().name; } }

        void Awake()
        {
            Current = this;
            Best = PlayerPrefs.GetInt(BestKey, 0);
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            StuntDirector.Landed -= OnLanded;
            StuntDirector.PromptGate = null;
        }

        void Start()
        {
            foreach (var b in FindObjectsByType<BikeVehicleAdapter>(FindObjectsSortMode.None)) if (!b.IsAIDriven) { Bike = b; break; }
            if (Bike != null)
            {
                bodies = Bike.GetComponentsInChildren<Rigidbody>(true);
                Bike.Guide = new SideOnGuide();
                var choice = RaceSession.Car;
                if (choice != null) Bike.ApplyTuning(BikeTuning.Load(choice.id));
                Bike.SetRegeneration(true);
                Bike.RegenScale = Core.Difficulty.Current.boostRefill;
                Bike.SetNitro(startBoost);
                Bike.SetControllable(false);
                StartX = Bike.Transform.position.x;
                // The course drops well below the usual "fallen off the world" height.
                var reset = Bike.GetComponent<VehicleReset>();
                if (reset != null) reset.KillHeight = killY - 10f;
            }
            StuntDirector.PromptGate = () => TrickBox.PlayerInside;
            StuntDirector.Landed += OnLanded;
            var hud = Resources.Load<AirShowHUD>("BR_AirShowHUD");
            if (hud != null) { var h = Instantiate(hud); h.name = "BR_AirShowHUD"; }
            CurrentState = State.Countdown;
            countdownStart = Time.time;
        }

        void Update()
        {
            if (Bike == null) return;
            if (CurrentState == State.Countdown)
            {
                Bike.SetControllable(false);
                float since = Time.time - countdownStart;
                int n = since < 0.8f ? 4 : 3 - Mathf.FloorToInt((since - 0.8f) / countdownStep);
                if (n <= 3 && n > 0 && n != CountdownNumber) { CountdownNumber = n; CountdownTick?.Invoke(n); }
                if (n <= 0)
                {
                    CountdownNumber = 0;
                    CurrentState = State.Riding;
                    Bike.SetControllable(true);
                    CountdownTick?.Invoke(0);
                }
                return;
            }
            if (CurrentState != State.Riding) return;
            RunTime += Time.deltaTime;
            var p = Bike.Transform.position;
            // Stuck still (wedged on a lip, hung up after a landing): back on the line after a few seconds.
            stuck = Mathf.Abs(Bike.SpeedKmh) < 3f && !ShiftRush.UI.PauseMenu.IsPaused ? stuck + Time.deltaTime : 0f;
            if (p.x >= finishX) Finish();
            else if (p.y < killY) Respawn();
            else if (stuck > 3f) Respawn(false);
        }

        /// <summary>
        /// Keeps the bike on the course's line in the air (on the ground the side-on guide steers it back, and
        /// side walls stop it leaving the course). Gentle: holding every body hard to the line scrubs the speed.
        /// </summary>
        void FixedUpdate()
        {
            if (bodies == null || Bike == null || Bike.AnyWheelGrounded) return;
            foreach (var rb in bodies)
            {
                if (rb == null || rb.isKinematic) continue;
                var v = rb.linearVelocity;
                v.z = Mathf.Lerp(v.z, -rb.position.z * 1.2f, 0.08f);
                rb.linearVelocity = v;
            }
        }

        void Finish()
        {
            CurrentState = State.Finished;
            Bike.SetControllable(false);
            NewBest = Score > Best;
            if (NewBest) { Best = Score; PlayerPrefs.SetInt(BestKey, Best); PlayerPrefs.Save(); }
            GameSfx.Play(Sfx.ShiftHit, 1f, 1f);
            Finished?.Invoke();
        }

        void Respawn(bool bail = true)
        {
            stuck = 0f;
            Transform spot = null;
            float x = Bike.Transform.position.x;
            if (checkpoints != null)
                foreach (var c in checkpoints) if (c != null && c.position.x < x - 2f) spot = c;
            Vector3 pos = spot != null ? spot.position : new Vector3(StartX, 20f, 0f);
            Bike.Teleport(pos, Quaternion.LookRotation(Vector3.right, Vector3.up), spot != null ? 30f : 0f);
            if (bail) { Bails++; Scored?.Invoke(0, "BACK ON THE LINE", "BAIL"); }
        }

        void OnLanded(string names, string grade, float pay)
        {
            if (CurrentState != State.Riding) return;
            if (grade == "BAIL") { Bails++; Scored?.Invoke(0, string.IsNullOrEmpty(names) ? "WIPEOUT" : names, "BAIL"); return; }
            var list = string.IsNullOrEmpty(names) ? new string[0] : names.Split(new[] { " + " }, System.StringSplitOptions.RemoveEmptyEntries);
            int count = list.Length;
            int pts = Mathf.RoundToInt(pay * 1000f) * 10;
            if (pts <= 0) return;
            Tricks += count;
            if (grade == "PERFECT") Perfects += count;
            BestCombo = Mathf.Max(BestCombo, count);
            Score += pts;
            string headline = count == 0 ? "BIG AIR" : count == 1 ? list[0] : count == 2 ? list[0] + " + " + list[1] : count + "-TRICK COMBO";
            Scored?.Invoke(pts, headline, grade);
        }

        public string Rank(int score)
        {
            if (score >= rankScores[0]) return "S";
            if (score >= rankScores[1]) return "A";
            if (score >= rankScores[2]) return "B";
            return "C";
        }

        /// <summary>0..1 how far along the course the bike is.</summary>
        public float Progress01
        {
            get
            {
                if (Bike == null) return 0f;
                return Mathf.Clamp01((Bike.Transform.position.x - StartX) / Mathf.Max(1f, finishX - StartX));
            }
        }

        /// <summary>Points the bike along +X, nudging it back onto z = 0.</summary>
        class SideOnGuide : ISteerGuide
        {
            public bool Aim(Vector3 position, float speed, float steer, out Vector3 direction)
            {
                direction = new Vector3(1f, 0f, Mathf.Clamp(-position.z * 0.3f, -0.5f, 0.5f)).normalized;
                return true;
            }
        }
    }
}
