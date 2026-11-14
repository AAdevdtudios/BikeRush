using SBPScripts;
using ShiftRush.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftRush.Vehicles
{
    /// <summary>
    /// Puts a Simple Bicycle Physics bike into the game: the race, HUD, rivals, coach and bonuses talk to
    /// <see cref="IVehicle"/>, never to the pack. The player's bike reads the game's own controls (SR_Controls, so
    /// Settings rebinds work); an AI rider drives it through <see cref="IDriverInput"/> instead.
    /// Boost: the pack's sprint, gated by a tank that tricks, drafting and close calls fill (no free regen).
    /// The only pack changes this relies on are the marked "BIKE//RUSH patch" lines in BicycleController.
    /// </summary>
    [RequireComponent(typeof(BicycleController))]
    public class BikeVehicleAdapter : MonoBehaviour, IVehicle, IVehicleDash, INitroTank, IDriverInput
    {
        [Header("Boost")]
        [Tooltip("Share of the tank burned per second while boosting.")]
        [SerializeField] float burnPerSecond = 0.22f;
        [Tooltip("Share of the tank the bike starts a race with.")]
        [SerializeField, Range(0f, 1f)] float startBoost = 0.35f;
        [Tooltip("Slow refill per second when regeneration is on (free roam). Races turn it off.")]
        [SerializeField] float regenPerSecond = 0.05f;

        [Header("Feel")]
        [Tooltip("How quickly steering follows the stick (the pack's own value is 5).")]
        [SerializeField] float steerResponse = 5f;
        [Tooltip("Turn off the pack's own freestyle air rotation (stunts are played by the StuntDirector).")]
        [SerializeField] bool tricks = true;
        [Tooltip("Height above the ground (m) at which the pack's stunt mode starts (its default is 5).")]
        [SerializeField] float trickHeight = 1.8f;

        BicycleController bike;
        Rigidbody body;
        Rigidbody[] bodies;
        InputAction steerAction, throttleAction, brakeAction, hopAction, boostAction;

        float tank, steerAxis, accelAxis;
        bool posed;
        Quaternion poseTarget;
        float poseFollow;
        bool regen = true, boostInputOn = true, controllable = true, boosting, hopHeld;

        // AI driving (IDriverInput)
        bool driven;
        float aiSteer, aiThrottle, aiBrake, aiHop;
        bool aiBoost;

        public Transform Transform { get { return transform; } }
        public Rigidbody Body { get { return body; } }
        public float SpeedKmh { get { return body != null ? Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f : 0f; } }
        // Our own ground check from each wheel, ignoring the bike and rider (the pack's single ray from the frame
        // can hit the rider's own colliders once the bike is upside down mid-trick).
        public bool AnyWheelGrounded { get { return WheelDown(bike != null ? bike.fPhysicsWheel : null) || WheelDown(bike != null ? bike.rPhysicsWheel : null); } }
        public bool AllWheelsGrounded { get { return WheelDown(bike != null ? bike.fPhysicsWheel : null) && WheelDown(bike != null ? bike.rPhysicsWheel : null); } }

        readonly RaycastHit[] hits = new RaycastHit[12];

        bool WheelDown(GameObject wheel)
        {
            if (wheel == null) return false;
            var sc = wheel.GetComponent<SphereCollider>();
            float r = sc != null ? sc.radius * Mathf.Max(wheel.transform.lossyScale.x, wheel.transform.lossyScale.y) : 0.35f;
            return GroundBelow(wheel.transform.position, r + 0.25f, out _) < r + 0.25f;
        }

        /// <summary>Distance straight down from a point to the ground (not this bike or its rider); large if none.</summary>
        public float GroundBelow(Vector3 from, float maxDistance, out Vector3 normal)
        {
            normal = Vector3.up;
            int n = Physics.RaycastNonAlloc(from, Vector3.down, hits, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var t = hits[i].transform;
                if (t.root == transform.root || t.IsChildOf(transform)) continue;
                if (hits[i].distance < best) { best = hits[i].distance; normal = hits[i].normal; }
            }
            return best;
        }
        public float SteerInput { get { return steerAxis; } }
        public float PitchInput { get { return accelAxis; } }

        // Dashboard: a bike has no engine, so "rpm" is how close to top speed you are and the gear is a 1-7 band.
        public float Rpm01 { get { return bike != null && bike.topSpeed > 0f ? Mathf.Clamp01(Mathf.Abs(SpeedKmh) / (bike.topSpeed * 3.6f)) : 0f; } }
        public string GearLabel
        {
            get
            {
                float kmh = SpeedKmh;
                if (kmh < -1f) return "R";
                if (Mathf.Abs(kmh) < 1f) return "N";
                return (1 + Mathf.Min(6, Mathf.FloorToInt(Rpm01 * 7f))).ToString();
            }
        }
        public float Nitro01 { get { return tank; } }
        /// <summary>An AI rider is steering this bike (not the player).</summary>
        public bool IsAIDriven { get { return driven; } }
        public BicycleController Bike { get { return bike; } }
        public bool NitroActive { get { return boosting; } }

        void Awake()
        {
            bike = GetComponent<BicycleController>();
            body = GetComponent<Rigidbody>();
            bodies = GetComponentsInChildren<Rigidbody>(true);
            bike.externalInput = true;
            if (tricks && bike.airTimeSettings != null)
            {
                bike.airTimeSettings.freestyle = true;
                bike.airTimeSettings.heightThreshold = trickHeight;
                bike.airTimeSettings.airTimeRotationSensitivity = 0f; // the pack's per-step kick spins far too fast: we set the rate ourselves
            }
            tank = startBoost;
        }

        void Start()
        {
            ControlBindings.Apply();
            var a = ControlBindings.Asset;
            if (a != null)
            {
                steerAction = a.FindAction("Steering");
                throttleAction = a.FindAction("Throttle");
                brakeAction = a.FindAction("Brake");
                hopAction = a.FindAction("Handbrake");
                boostAction = a.FindAction("NOS");
            }
            // The bike's own sounds follow the Car & Effects volume.
            var sfx = AudioBoot.Group("SFX");
            if (sfx != null) foreach (var src in GetComponentsInChildren<AudioSource>(true)) src.outputAudioMixerGroup = sfx;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float steer, throttle, brake; bool hop, boost;
            if (driven) { steer = aiSteer; throttle = aiThrottle; brake = aiBrake; hop = aiHop > 0.5f; boost = aiBoost; }
            else
            {
                steer = Read(steerAction); throttle = Read(throttleAction); brake = Read(brakeAction);
                hop = Read(hopAction) > 0.5f; boost = boostInputOn && Read(boostAction) > 0.5f;
            }
            if (!controllable) { steer = 0f; throttle = 0f; brake = 1f; hop = false; boost = false; }

            // Same smoothing the pack applies to the keyboard, so it feels as designed.
            steerAxis = Mathf.MoveTowards(steerAxis, steer, steerResponse * Time.unscaledDeltaTime);
            float pedal = Mathf.Clamp(throttle - brake, -1f, 1f);
            accelAxis = Mathf.MoveTowards(accelAxis, pedal, Time.unscaledDeltaTime);

            bike.customSteerAxis = steerAxis;
            bike.customLeanAxis = Mathf.Clamp(steerAxis, -1f, 1f);
            bike.customAccelerationAxis = accelAxis;
            bike.rawCustomAccelerationAxis = pedal > 0.1f ? 1f : pedal < -0.1f ? -1f : 0f;

            // Hold to compress, release to hop (the pack's own stateful input).
            bike.bunnyHopInputState = hop ? 1 : (hopHeld ? -1 : 0);
            hopHeld = hop;

            // Boost: the pack's sprint, only while the tank has some, and only on the ground (Shift is the landing
            // press in the air).
            boosting = boost && tank > 0f && controllable && AnyWheelGrounded;
            if (boosting) tank = Mathf.Max(0f, tank - burnPerSecond * dt);
            else if (regen) tank = Mathf.Min(1f, tank + regenPerSecond * dt);
            bike.sprint = boosting;
        }

        /// <summary>
        /// In the air, hold the bike at an orientation (the flight path stays physics). follow 0 snaps to it each
        /// physics step (a stunt's exact turn); above 0 eases towards it at that rate (levelling for a landing).
        /// </summary>
        public void HoldPose(Quaternion target, float follow) { posed = true; poseTarget = target; poseFollow = follow; }
        public void ReleasePose() { posed = false; }

        void FixedUpdate()
        {
            if (!posed || body == null || !controllable) return;
            Quaternion want = poseFollow <= 0f ? poseTarget
                : Quaternion.Slerp(body.rotation, poseTarget, 1f - Mathf.Exp(-poseFollow * Time.fixedDeltaTime));
            Quaternion delta = want * Quaternion.Inverse(body.rotation);
            // Turn the frame and both joined wheels as one piece about the frame's centre, or the joints drag it back.
            Vector3 pivot = body.worldCenterOfMass, v = body.linearVelocity;
            foreach (var rb in bodies)
            {
                if (rb == null) continue;
                rb.position = pivot + delta * (rb.position - pivot);
                rb.rotation = delta * rb.rotation;
                rb.linearVelocity = v;
                rb.angularVelocity = rb == body ? Vector3.zero : delta * rb.angularVelocity; // wheels keep spinning
            }
        }

        static float Read(InputAction a) { return a != null && a.enabled ? a.ReadValue<float>() : 0f; }

        // ---- INitroTank
        public void SetNitro(float fill01) { tank = Mathf.Clamp01(fill01); }
        public void AddNitro(float fill01) { tank = Mathf.Clamp01(tank + fill01); }
        public void SetRegeneration(bool on) { regen = on; }
        public void SetNitroInput(bool on) { boostInputOn = on; }

        // ---- IDriverInput
        public void SetDriverInput(float steer, float throttle, float brake, float handbrake, bool nitro)
        {
            driven = true;
            aiSteer = Mathf.Clamp(steer, -1f, 1f); aiThrottle = Mathf.Clamp01(throttle); aiBrake = Mathf.Clamp01(brake);
            aiHop = handbrake; aiBoost = nitro;
        }
        public void ReleaseDriverInput() { driven = false; }

        // ---- IVehicle
        public void SetControllable(bool on) { controllable = on; }

        /// <summary>
        /// Moves the frame and both physics wheels together (they're joined, so moving only the frame would
        /// tear the joints), level and facing the given way, at a forward speed.
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation, float forwardSpeedKmh)
        {
            var root = transform;
            var local = new Vector3[bodies.Length];
            var localRot = new Quaternion[bodies.Length];
            for (int i = 0; i < bodies.Length; i++)
            {
                local[i] = root.InverseTransformPoint(bodies[i].position);
                localRot[i] = Quaternion.Inverse(root.rotation) * bodies[i].rotation;
            }
            root.SetPositionAndRotation(position, rotation);
            Vector3 v = rotation * Vector3.forward * (forwardSpeedKmh / 3.6f);
            for (int i = 0; i < bodies.Length; i++)
            {
                var rb = bodies[i];
                rb.position = root.TransformPoint(local[i]);
                rb.rotation = rotation * localRot[i];
                rb.transform.SetPositionAndRotation(rb.position, rb.rotation);
                rb.linearVelocity = v;
                rb.angularVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();
        }
    }
}
