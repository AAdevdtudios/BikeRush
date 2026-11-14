using System.Collections.Generic;
using ShiftRush.Audio;
using ShiftRush.Core;
using ShiftRush.Race;
using ShiftRush.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ShiftRush.Stunts
{
    /// <summary>
    /// Stunts for the player's bike, played on timing rather than steered by hand.
    /// <list type="number">
    /// <item>In the air, if there's time to land one, time slows and a magenta diamond closes on the target.</item>
    /// <item>Press Shift while it's on the green and the rider throws a stunt sized to the height left
    /// (a 360 off a small drop, a backflip 360 or a double backflip off a building).</item>
    /// <item>Once it's done, the height is checked again: room for another and the diamond comes back; if not, the
    /// bike is levelled and set down on its wheels. Miss the press and the chain ends there.</item>
    /// </list>
    /// The bike is always levelled to the ground below it, so a stunt can't end in the dirt. Boost for the chain is
    /// paid on touchdown through <see cref="RushBonus"/>.
    /// Lives on Resources/SR_RushHUD (its HUD is built there by SHIFT RUSH → Build Rush Bonus HUD).
    /// </summary>
    public class StuntDirector : MonoBehaviour
    {
        struct Trick
        {
            public string name; public float yaw, pitch, seconds, value;
            public Trick(string n, float y, float p, float s, float v) { name = n; yaw = y; pitch = p; seconds = s; value = v; }
        }

        // yaw: spin about the ground's up. pitch: + front flip (nose over), - back flip. seconds of game time.
        static readonly Trick[] Tricks =
        {
            new Trick("360", 360f, 0f, 0.5f, 0.10f),
            new Trick("BACKFLIP", 0f, -360f, 0.6f, 0.16f),
            new Trick("FRONTFLIP", 0f, 360f, 0.6f, 0.16f),
            new Trick("720", 720f, 0f, 0.8f, 0.18f),
            new Trick("BACKFLIP 360", 360f, -360f, 0.85f, 0.24f),
            new Trick("FRONTFLIP 360", 360f, 360f, 0.85f, 0.24f),
            new Trick("DOUBLE BACKFLIP", 0f, -720f, 1.05f, 0.30f),
        };

        [Header("When a stunt is offered")]
        [Tooltip("Seconds off the ground before the first prompt can show (bumps and hops don't count).")]
        [SerializeField] float minAirTime = 0.2f;
        [Tooltip("Game seconds kept free after a stunt to level the bike for the landing.")]
        [SerializeField] float landMargin = 0.3f;

        [Header("The SHIFT prompt")]
        [Tooltip("Real seconds the diamond takes to close from start to end size.")]
        [SerializeField] float promptSeconds = 0.9f;
        [SerializeField, Range(0.05f, 1f)] float slowMotion = 0.25f;
        [SerializeField] float startScale = 3f;
        [SerializeField] float endScale = 0.4f;
        [Tooltip("How far from the target size (1) a press still lands the stunt; scaled by the difficulty's SHIFT window.")]
        [SerializeField] float window = 0.22f;
        [Tooltip("How far from the target size a press is PERFECT.")]
        [SerializeField] float perfectBand = 0.08f;

        [Header("Riding it out")]
        [Tooltip("How quickly the bike levels to the ground below it between stunts.")]
        [SerializeField] float levelFollow = 8f;
        [SerializeField] float chainStep = 0.25f;
        [SerializeField] float perfectMult = 1.5f;
        [SerializeField] float perfectKick = 4f;
        [Tooltip("The same stunt twice in a row pays this much.")]
        [SerializeField] float repeatFactor = 0.5f;
        [SerializeField] float bailTilt = 60f;
        [SerializeField] float bailSeconds = 1.6f;

        [Header("HUD")]
        [SerializeField] CanvasGroup landGroup;
        [SerializeField] Image targetDiamond;
        [SerializeField] Image closingDiamond;
        [SerializeField] TMP_Text trickText;
        [SerializeField] TMP_Text keyText;
        [SerializeField] Color accent = new Color32(255, 64, 217, 255);
        [SerializeField] Color green = new Color32(61, 255, 138, 255);

        /// <summary>A landing: the chain's name ("" for none), the grade and the boost paid.</summary>
        public static event System.Action<string, string, float> Landed;

        enum State { Ground, Free, Prompt, Trick }

        BikeVehicleAdapter bike;
        Rigidbody[] bodies;
        InputAction pressAction;
        State state = State.Ground;
        float airTime, promptT, trickT, nextPromptAt, flashUntil, baseFixed, bailUntil, nextFind, settling, arriveTilt, total;
        bool slowed, chainOver, allPerfect, perfect;
        int chain;
        Trick current;
        Quaternion trickStart;
        string lastName = "";
        readonly List<string> names = new List<string>();

        void Awake()
        {
            baseFixed = Time.fixedDeltaTime;
            if (landGroup != null) landGroup.alpha = 0f;
        }

        void OnDisable() { SetSlow(false); if (bike != null) bike.ReleasePose(); }

        BikeVehicleAdapter Player()
        {
            var race = RaceManager.Current;
            if (race != null && race.Player != null && race.Player.vehicle is BikeVehicleAdapter rb) return rb;
            if (bike != null && !bike.IsAIDriven) return bike;
            if (Time.unscaledTime < nextFind) return null;
            nextFind = Time.unscaledTime + 1f;
            foreach (var b in FindObjectsByType<BikeVehicleAdapter>(FindObjectsSortMode.None)) if (!b.IsAIDriven) return b;
            return null;
        }

        void Update()
        {
            var b = Player();
            if (b != bike)
            {
                if (bike != null) bike.ReleasePose();
                bike = b; bodies = b != null ? b.GetComponentsInChildren<Rigidbody>(true) : null;
                state = State.Ground; SetSlow(false);
            }
            if (pressAction == null && ControlBindings.Asset != null) pressAction = ControlBindings.Asset.FindAction("NOS");
            Hud();
            if (bike == null || bike.Body == null || Time.timeScale == 0f) return; // paused

            float dt = Time.deltaTime;
            bool air = !bike.AnyWheelGrounded;
            // After a bail: nothing counts until the bike is back up and has had its wheels down a moment.
            if (Time.time < bailUntil || settling > 0f)
            {
                if (Time.time >= bailUntil) settling = air ? 0.3f : settling - dt;
                state = State.Ground;
                return;
            }

            if (state == State.Ground)
            {
                if (!air) return;
                state = State.Free; airTime = 0f; chain = 0; total = 0f; allPerfect = true; chainOver = false; names.Clear();
            }
            if (!air) { Touchdown(); return; }
            airTime += dt;
            arriveTilt = Tilt();

            switch (state)
            {
                case State.Free:
                    if (!chainOver && airTime >= minAirTime && Time.time >= nextPromptAt
                        && Pick(TimeToImpact() - PromptGameSeconds() - landMargin, false).HasValue)
                        StartPrompt();
                    break;

                case State.Prompt:
                    promptT += Time.unscaledDeltaTime;
                    float miss = Mathf.Abs(PromptScale() - 1f), shiftWindow = Difficulty.Current.shiftWindow;
                    if (pressAction != null && pressAction.WasPressedThisFrame())
                    {
                        if (miss <= window * shiftWindow) StartTrick(miss <= perfectBand * shiftWindow);
                        else Miss("TOO EARLY");
                    }
                    else if (promptT >= promptSeconds) Miss("MISSED");
                    break;
            }
        }

        void FixedUpdate()
        {
            if (bike == null || bike.Body == null) return;
            if (state == State.Trick)
            {
                trickT += Time.fixedDeltaTime / current.seconds;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(trickT));
                // Spin about the ground's up, flip about the bike's own axle line: both whole turns, so it ends level.
                var target = Quaternion.AngleAxis(current.yaw * e, trickStart * Vector3.up) * trickStart * Quaternion.AngleAxis(current.pitch * e, Vector3.right);
                bike.HoldPose(target, 0f);
                if (trickT >= 1f) FinishTrick();
            }
            else if (state == State.Free || state == State.Prompt)
            {
                if (airTime >= minAirTime) bike.HoldPose(Upright(), levelFollow);
            }
        }

        // ---- the cycle

        void StartPrompt()
        {
            state = State.Prompt;
            promptT = 0f;
            SetSlow(true);
            GameSfx.Play(Sfx.ShiftOpen, 1.2f, 0.7f);
        }

        void StartTrick(bool isPerfect)
        {
            SetSlow(false);
            var pick = Pick(TimeToImpact() - landMargin, true);
            if (!pick.HasValue) { state = State.Free; chainOver = true; return; }
            current = pick.Value;
            perfect = isPerfect;
            trickStart = Upright();
            trickT = 0f;
            state = State.Trick;
            Flash(current.name, perfect ? "PERFECT" : "NICE");
            GameSfx.Play(Sfx.ShiftHit, perfect ? 1.4983f : 1.2599f);
        }

        void FinishTrick()
        {
            float v = current.value * (perfect ? perfectMult : 1f) * (1f + chainStep * chain);
            if (current.name == lastName) v *= repeatFactor;
            lastName = current.name;
            total += v;
            allPerfect &= perfect;
            names.Add(current.name);
            chain++;
            state = State.Free;
            nextPromptAt = Time.time + 0.12f; // a beat to see the stunt land before the next diamond
        }

        void Miss(string why)
        {
            SetSlow(false);
            chainOver = true;
            state = State.Free;
            Flash(chain > 0 ? names[names.Count - 1] : "", why);
            GameSfx.Play(Sfx.ShiftMiss, 1f, 0.7f);
        }

        void Touchdown()
        {
            SetSlow(false);
            bike.ReleasePose();
            bool wasStunt = state != State.Ground && airTime >= minAirTime;
            state = State.Ground;
            if (!wasStunt) return;

            if (arriveTilt > bailTilt) // only something like a wall gets you here: the bike is levelled otherwise
            {
                Bail();
                if (RushBonus.Instance != null) RushBonus.Instance.Pay(bike, "BAIL", "", 0f);
                Landed?.Invoke(string.Join(" + ", names), "BAIL", 0f);
                return;
            }

            string headline = chain == 0 ? (airTime > 1.2f ? "BIG AIR" : "")
                : chain == 1 ? names[0]
                : chain == 2 ? names[0] + " + " + names[1]
                : chain + "-STUNT COMBO";
            if (headline.Length == 0) return;
            float pay = chain == 0 ? 0.04f : total;
            string grade = chain == 0 ? "" : allPerfect ? "PERFECT" : "CLEAN";
            if (chain > 0 && allPerfect)
                foreach (var rb in bodies) if (rb != null) rb.AddForce(bike.Transform.forward * perfectKick, ForceMode.VelocityChange);
            if (RushBonus.Instance != null) RushBonus.Instance.Pay(bike, headline, grade, pay);
            Landed?.Invoke(string.Join(" + ", names), grade, pay);
        }

        // ---- choosing

        // The biggest stunts that fit the time left, one of the top few at random (not the last one again if avoidable).
        Trick? Pick(float seconds, bool random)
        {
            var fits = new List<Trick>();
            foreach (var t in Tricks) if (t.seconds <= seconds) fits.Add(t);
            if (fits.Count == 0) return null;
            if (!random) return fits[0];
            fits.Sort((x, y) => y.seconds.CompareTo(x.seconds));
            int top = Mathf.Min(3, fits.Count);
            int i = Random.Range(0, top);
            if (fits[i].name == lastName && top > 1) i = (i + 1) % top;
            return fits[i];
        }

        // Game seconds a full prompt takes (it runs in slow motion).
        float PromptGameSeconds() { return promptSeconds * slowMotion; }
        float PromptScale() { return Mathf.Lerp(startScale, endScale, promptT / promptSeconds); }

        // ---- the bike in the air

        // Level with the ground below (so a ramp's landing matches its slope), facing the way the bike is travelling.
        Quaternion Upright()
        {
            var rb = bike.Body;
            Vector3 n;
            if (bike.GroundBelow(rb.position + Vector3.up * 0.5f, 200f, out n) > 199f || Vector3.Angle(n, Vector3.up) > 35f) n = Vector3.up;
            Vector3 fwd = Vector3.ProjectOnPlane(rb.linearVelocity, n);
            if (fwd.sqrMagnitude < 1f) fwd = Vector3.ProjectOnPlane(bike.Transform.forward, n);
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(bike.Transform.up, n);
            return Quaternion.LookRotation(fwd.normalized, n);
        }

        float Tilt()
        {
            Vector3 n;
            bike.GroundBelow(bike.Body.position + Vector3.up * 0.5f, 30f, out n);
            return Vector3.Angle(bike.Transform.up, n);
        }

        // Seconds until the wheels meet the ground, from height and falling speed under gravity.
        float TimeToImpact()
        {
            var rb = bike.Body;
            Vector3 n;
            float d = bike.GroundBelow(rb.position + Vector3.up * 0.5f, 200f, out n);
            if (d > 199f) return 99f;
            float h = Mathf.Max(0f, d - 1.5f); // root sits about a metre above the tyres
            float vDown = -rb.linearVelocity.y, g = -Physics.gravity.y;
            if (h <= 0f) return 0f;
            return (-vDown + Mathf.Sqrt(Mathf.Max(0f, vDown * vDown + 2f * g * h))) / g;
        }

        void SetSlow(bool on)
        {
            if (slowed == on) return;
            slowed = on;
            if (baseFixed <= 0f) baseFixed = 0.02f;
            Time.timeScale = on ? slowMotion : 1f;
            Time.fixedDeltaTime = baseFixed * (on ? slowMotion : 1f);
        }

        // Down and out for a moment, then back on the wheels, upright, facing the way you were going.
        void Bail()
        {
            bailUntil = Time.time + bailSeconds + 0.1f;
            GameSfx.Play(Sfx.TrapHit, 0.85f);
            var status = bike.GetComponent<SBPScripts.BicycleStatus>();
            if (status != null && status.ragdollPrefab != null) status.dislodged = true;
            bike.SetControllable(false);
            Vector3 dir = Vector3.ProjectOnPlane(bike.Body.linearVelocity, Vector3.up);
            if (dir.sqrMagnitude < 1f) dir = Vector3.ProjectOnPlane(bike.Transform.forward, Vector3.up);
            StartCoroutine(GetUp(dir.normalized, status));
        }

        System.Collections.IEnumerator GetUp(Vector3 dir, SBPScripts.BicycleStatus status)
        {
            yield return new WaitForSeconds(bailSeconds);
            if (bike == null) yield break;
            if (status != null) status.dislodged = false;
            Vector3 p = bike.Body.position;
            Vector3 n;
            float d = bike.GroundBelow(p + Vector3.up * 3f, 30f, out n);
            if (d < 30f) p.y = p.y + 3f - d + 1.1f;
            bike.Teleport(p, Quaternion.LookRotation(dir, Vector3.up), 0f);
            bike.SetControllable(true);
            settling = 0.3f;
        }

        // ---- HUD

        void Flash(string trick, string key)
        {
            flashUntil = Time.unscaledTime + 0.7f;
            if (trickText != null) trickText.text = trick;
            if (keyText != null) keyText.text = key;
        }

        void Hud()
        {
            if (landGroup == null) return;
            bool prompt = state == State.Prompt;
            bool on = prompt || state == State.Trick || Time.unscaledTime < flashUntil;
            landGroup.alpha = Mathf.MoveTowards(landGroup.alpha, on ? 1f : 0f, Time.unscaledDeltaTime * (on ? 12f : 5f));
            if (closingDiamond != null) closingDiamond.enabled = prompt;
            if (targetDiamond != null) targetDiamond.enabled = prompt;
            if (!prompt) return;

            float scale = PromptScale();
            bool inside = Mathf.Abs(scale - 1f) <= window * Difficulty.Current.shiftWindow;
            if (closingDiamond != null)
            {
                closingDiamond.rectTransform.localScale = Vector3.one * scale;
                closingDiamond.color = inside ? green : accent;
            }
            if (targetDiamond != null) targetDiamond.color = inside ? green : Color.white;
            if (trickText != null) trickText.text = chain > 0 ? "x" + (chain + 1) : "";
            if (keyText != null)
            {
                string k = ControlBindings.KeyName("NOS", "keyboard", "positive");
                keyText.text = string.IsNullOrEmpty(k) ? "SHIFT" : k;
            }
        }
    }
}
