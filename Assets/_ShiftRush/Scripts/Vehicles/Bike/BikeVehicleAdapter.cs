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
        [SerializeField] float burnPerSecond = 0.16f;
        [Tooltip("Share of the tank the bike starts a race with.")]
        [SerializeField, Range(0f, 1f)] float startBoost = 0.35f;
        [Tooltip("Refill per second while not boosting (races and free roam): empty to full in about 15 s.")]
        [SerializeField] float regenPerSecond = 0.065f;

        [Header("Feel")]
        [Tooltip("How quickly steering follows the stick (the pack's own value is 5).")]
        [SerializeField] float steerResponse = 5f;
        [Tooltip("Tyre grip. The pack's ~0.5 lets the front wheel slide sideways on our trails, so the bike barely turns.")]
        [SerializeField] float tyreGrip = 1.6f;
        [Tooltip("Turn off the pack's own freestyle air rotation (stunts are played by the StuntDirector).")]
        [SerializeField] bool tricks = true;
        [Tooltip("Height above the ground (m) at which the pack's stunt mode starts (its default is 5).")]
        [SerializeField] float trickHeight = 1.8f;
        [Tooltip("Pedalling speed on the flat (km/h). The pack's own 32 felt like a stroll.")]
        [SerializeField] float cruiseKmh = 74f;
        [Tooltip("Top speed while boosting (km/h).")]
        [SerializeField] float boostKmh = 112f;
        [Tooltip("Arcade launch: extra acceleration (m/s²) while pedalling below cruise speed. The pack alone takes ~4 s to get going.")]
        [SerializeField] float launchAccel = 11f;
        [Tooltip("Acceleration (m/s²) while boosting, up to boost speed.")]
        [SerializeField] float boostAccel = 14f;
        [Tooltip("Instant speed (m/s) added the moment boost kicks in.")]
        [SerializeField] float boostKick = 4.5f;
        [Tooltip("Arcade carving: extra turn (degrees/second) at full steer on the ground. The pack alone manages about 0.35 g, so at race speed a bike can't hold a bend.")]
        [SerializeField] float carveRate = 70f;
        [Tooltip("How fast sideways slide is turned into forward speed (degrees/second): grip like a racing game.")]
        [SerializeField] float carveGrip = 120f;
        [Tooltip("With a track guide (the player in a race): how hard the bike turns to follow the trail.")]
        [SerializeField] float guideGain = 3.5f;
        [Tooltip("Fastest the guide will turn the bike (degrees/second).")]
        [SerializeField] float guideMaxRate = 75f;

        /// <summary>Track steering help (set on the player's bike by the race). Null = raw steering.</summary>
        public ISteerGuide Guide { get; set; }
        float guidedSteer;
        bool guided;

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
            if (bike.wheelFrictionSettings != null && tyreGrip > 0f)
            {
                bike.wheelFrictionSettings.fFriction = new Vector2(tyreGrip, tyreGrip);
                bike.wheelFrictionSettings.rFriction = new Vector2(tyreGrip, tyreGrip);
                bike.topSpeed = boostKmh / 3.6f;
                bike.relaxedSpeed = Mathf.Clamp01(cruiseKmh / boostKmh);
            }
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

            // On a guided bike the pack steers (and leans) by the turn the trail asks for, not the raw stick.
            float packSteer = guided ? guidedSteer : steerAxis;
            bike.customSteerAxis = packSteer;
            bike.customLeanAxis = Mathf.Clamp(packSteer, -1f, 1f);
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

            // Rivals don't do stunts (yet): off a jump they're levelled for the landing, so they ride it out.
            if (driven)
            {
                aiAir = AnyWheelGrounded ? 0f : aiAir + dt;
                if (aiAir > 0.25f) { HoldPose(LevelPose(), 6f); aiPosed = true; }
                else if (aiPosed) { ReleasePose(); aiPosed = false; }
            }
        }

        float aiAir;
        bool aiPosed;

        /// <summary>Level with the ground below (matching a slope), facing the way the bike is travelling.</summary>
        public Quaternion LevelPose()
        {
            Vector3 n;
            if (GroundBelow(body.position + Vector3.up * 0.5f, 200f, out n) > 199f || Vector3.Angle(n, Vector3.up) > 35f) n = Vector3.up;
            Vector3 fwd = Vector3.ProjectOnPlane(body.linearVelocity, n);
            if (fwd.sqrMagnitude < 1f) fwd = Vector3.ProjectOnPlane(transform.forward, n);
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(transform.up, n);
            return Quaternion.LookRotation(fwd.normalized, n);
        }

        /// <summary>
        /// In the air, hold the bike at an orientation (the flight path stays physics). follow 0 snaps to it each
        /// physics step (a stunt's exact turn); above 0 eases towards it at that rate (levelling for a landing).
        /// </summary>
        public void HoldPose(Quaternion target, float follow) { posed = true; poseTarget = target; poseFollow = follow; }
        public void ReleasePose() { posed = false; }

        void FixedUpdate()
        {
            guided = false; // Carve() sets it again while the trail is steering
            // Parked (the countdown, a bail): hold still. The pack's brake turns into reverse once the bike
            // starts rolling back, so on a slope the whole grid used to drift backwards before GO.
            if (!controllable && body != null && AnyWheelGrounded && body.linearVelocity.sqrMagnitude < 9f)
            {
                foreach (var rb in bodies)
                {
                    if (rb == null) continue;
                    rb.linearVelocity = new Vector3(0f, Mathf.Min(rb.linearVelocity.y, 0f), 0f);
                    if (rb != body) rb.angularVelocity = Vector3.zero;
                }
            }
            if (!posed) { Carve(); Drive(); return; }
            if (body == null || !controllable) return;
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

        bool kicked;

        /// <summary>Arcade pace: a quick launch up to cruise, a shove when boost lights, a hard pull while it burns.</summary>
        void Drive()
        {
            if (body == null || !controllable || !AnyWheelGrounded) { kicked = boosting && kicked; return; }
            float dt = Time.fixedDeltaTime;
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            float along = Vector3.Dot(body.linearVelocity, fwd);
            float accel = 0f;
            if (boosting)
            {
                if (!kicked) { kicked = true; foreach (var rb in bodies) if (rb != null) rb.linearVelocity += fwd * boostKick; }
                if (along < boostKmh / 3.6f) accel = boostAccel;
            }
            else
            {
                kicked = false;
                if (accelAxis > 0.1f && along > -0.5f && along < bike.topSpeed * bike.relaxedSpeed) accel = launchAccel * accelAxis;
            }
            if (accel > 0f) foreach (var rb in bodies) if (rb != null) rb.linearVelocity += fwd * accel * dt;
        }

        /// <summary>
        /// Arcade cornering on the ground: turn the frame and both wheels together about the frame's centre, and
        /// swing the momentum round with them, so steering carves a line instead of sliding wide.
        /// </summary>
        void Carve()
        {
            if (body == null || !controllable || !AnyWheelGrounded) return;
            float dt = Time.fixedDeltaTime;
            Vector3 v = body.linearVelocity;
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 vh = new Vector3(v.x, 0f, v.z);
            float speed = vh.magnitude;
            if (speed < 2f || Vector3.Dot(vh, fwd) < 0f) return; // not when crawling or rolling backwards

            float ramp = Mathf.InverseLerp(2f, 8f, speed);
            float yaw;
            Vector3 aim = Vector3.zero;
            guided = !driven && Guide != null && Guide.Aim(body.position, speed, steerAxis, out aim);
            if (guided)
            {
                float err = Vector3.SignedAngle(fwd, aim, Vector3.up);
                guidedSteer = Mathf.Clamp(err / 40f, -1f, 1f);
                float rate = Mathf.Clamp(err * guideGain, -guideMaxRate, guideMaxRate);
                yaw = rate * ramp * dt;
            }
            else yaw = steerAxis * carveRate * ramp * dt;
            Quaternion turn = Quaternion.AngleAxis(yaw, Vector3.up);
            fwd = turn * fwd;
            // Grip: bring the travel direction round to where the bike points.
            Vector3 vhNew = Vector3.RotateTowards(turn * vh, fwd * speed, carveGrip * Mathf.Deg2Rad * dt, 0f);
            Vector3 dv = vhNew - vh;
            Vector3 pivot = body.worldCenterOfMass;
            foreach (var rb in bodies)
            {
                if (rb == null) continue;
                if (yaw != 0f) { rb.position = pivot + turn * (rb.position - pivot); rb.rotation = turn * rb.rotation; }
                rb.linearVelocity += dv;
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
        /// <summary>Sets how fast this bike pedals without boost (km/h); boost scales with it.</summary>
        public void SetCruiseSpeed(float kmh)
        {
            if (bike == null || bike.relaxedSpeed <= 0f || kmh <= 0f) return;
            bike.topSpeed = kmh / 3.6f / bike.relaxedSpeed;
        }

        public void ReleaseDriverInput()
        {
            driven = false;
            if (aiPosed) { ReleasePose(); aiPosed = false; }
        }

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
