using SBPScripts;
using UnityEngine;

namespace ShiftRush.Vehicles
{
    /// <summary>
    /// Makes speed read on screen. The pack's chase camera sits far back on a 40° lens, which flattens everything
    /// into a slow crawl. This pulls the camera in on a wide lens that opens further as the bike speeds up, punches
    /// out when boost kicks in, and adds a fine high-speed shake. Installs itself on any camera with a BicycleCamera.
    /// </summary>
    [DefaultExecutionOrder(200)] // after BicycleCamera has placed the camera this frame
    public class BikeSpeedFeel : MonoBehaviour
    {
        [SerializeField] float baseFov = 68f;
        [SerializeField] float fastFov = 92f;
        [Tooltip("Speed (km/h) at which the lens is fully open.")]
        [SerializeField] float fastKmh = 86f;
        [SerializeField] float boostKick = 6f;
        [SerializeField] float chaseDistance = 3.6f;
        [SerializeField] float chaseHeight = 1.85f;
        [Tooltip("Shake (metres) at full speed.")]
        [SerializeField] float shake = 0.05f;

        [Header("Speed lines")]
        [Tooltip("Speed lines start at this speed (km/h) and are thickest at top boost speed.")]
        [SerializeField] float linesFromKmh = 50f, linesFullKmh = 90f;
        [SerializeField] float linesPerSecond = 180f;

        Camera cam;
        ParticleSystem lines;
        UnityEngine.Rendering.Universal.MotionBlur blur;

        [Header("Post")]
        [Tooltip("Camera motion blur at top speed (0..1). Most of the sense of speed at 70+ km/h.")]
        [SerializeField] float blurAtSpeed = 0.55f;
        [SerializeField] float bloom = 0.7f;
        float lineDebt;
        BicycleCamera chase;
        BikeVehicleAdapter bike;
        float fov, kick;
        bool wasBoosting;

        // On the first scene and on every scene loaded after it (a race picked from the menu): the start-up hook
        // alone only ever saw the first scene, so races loaded from the menu ran without this.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            AddToCameras();
        }

        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) { AddToCameras(); }

        static void AddToCameras()
        {
            foreach (var c in FindObjectsByType<BicycleCamera>(FindObjectsSortMode.None))
                if (c.GetComponent<BikeSpeedFeel>() == null) c.gameObject.AddComponent<BikeSpeedFeel>();
        }

        // The pack's chase camera (stunt mode) follows a "Follow" point it parents to the bike without copying the
        // bike's facing, so if the bike isn't facing world +Z at that moment the camera rides alongside it, side-on.
        // Keep that point square to the bike: the camera always sits behind.
        void Update()
        {
            if (chase == null || chase.target == null) return;
            var t = chase.target;
            if (t.parent != null && t.name == "Follow" && t.localRotation != Quaternion.identity) t.localRotation = Quaternion.identity;
        }

        void Start()
        {
            cam = GetComponent<Camera>();
            chase = GetComponent<BicycleCamera>();
            if (chase != null) { chase.distance = chaseDistance; chase.height = chaseHeight; }
            // No mouse-look in a race: the pack's free-look swung the camera around whenever the mouse moved.
            var look = GetComponent<PerfectMouseLook>();
            if (look != null) { look.movement = false; look.enabled = false; }
            fov = baseFov;
            lines = MakeLines();
            MakePost();
        }

        /// <summary>
        /// Always behind the rider: if the camera ends up well off to the side or facing back (a reset, a spin-out),
        /// put it straight back behind the bike instead of letting it swing slowly round.
        /// </summary>
        void SnapBehind()
        {
            if (chase == null || chase.target == null) return;
            var target = chase.target;
            Vector3 fwd = target.forward; fwd.y = 0f;
            Vector3 camFwd = transform.forward; camFwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f || camFwd.sqrMagnitude < 0.01f) return;
            if (Vector3.Angle(fwd, camFwd) < 70f) return;
            fwd.Normalize();
            transform.position = target.position - fwd * chase.distance + Vector3.up * (chase.height + 0.2f);
            transform.LookAt(target.position + Vector3.up * chase.lookAtHeight);
        }

        /// <summary>A global volume of our own: motion blur that grows with speed, and a soft bloom for the glows.</summary>
        void MakePost()
        {
            var data = GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (data != null) data.renderPostProcessing = true;
            var go = new GameObject("Speed Post");
            var vol = go.AddComponent<UnityEngine.Rendering.Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;
            var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
            blur = profile.Add<UnityEngine.Rendering.Universal.MotionBlur>(true);
            blur.intensity.value = 0f;
            blur.clamp.value = 0.06f;
            blur.quality.value = UnityEngine.Rendering.Universal.MotionBlurQuality.Medium;
            var b = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
            b.intensity.value = bloom;
            b.threshold.value = 1.1f;
            b.scatter.value = 0.6f;
            vol.sharedProfile = profile;
        }

        ParticleSystem MakeLines()
        {
            var go = new GameObject("Speed Lines");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.26f;
            main.startSpeed = 0f;
            main.maxParticles = 400;
            main.playOnAwake = false;
            var emission = ps.emission; emission.enabled = false;
            // Fade out before they reach the lens, so none sweep past as big beams.
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            var shape = ps.shape; shape.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.09f;
            r.lengthScale = 1f;
            r.sharedMaterial = Resources.Load<Material>("M_SpeedLines");
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        void EmitLines(float kmh, bool boosting)
        {
            if (lines == null) return;
            float t = Mathf.InverseLerp(linesFromKmh, linesFullKmh, kmh);
            if (boosting) t = Mathf.Max(t, 0.5f);
            if (t <= 0f || Time.timeScale < 0.9f) { lineDebt = 0f; return; }
            lineDebt += t * linesPerSecond * (boosting ? 1.6f : 1f) * Time.deltaTime;
            var e = new ParticleSystem.EmitParams();
            while (lineDebt >= 1f)
            {
                lineDebt -= 1f;
                Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(2.6f, 6.5f);
                e.position = new Vector3(ring.x * 1.7f, ring.y * 0.75f + 0.6f, 26f); // wide oval: more at the sides than top and bottom
                e.velocity = new Vector3(0f, 0f, -Random.Range(55f, 75f));
                e.startSize = Random.Range(0.025f, 0.05f);
                float a = Random.Range(0.25f, 0.55f);
                e.startColor = boosting && Random.value < 0.4f ? new Color(1f, 0.3f, 0.85f, a + 0.2f) : new Color(1f, 1f, 1f, a);
                lines.Emit(e, 1);
            }
        }

        void LateUpdate()
        {
            if (cam == null) return;
            // The pack's camera follows whichever bike it finds first: in a race, always the player's.
            var race = ShiftRush.Race.RaceManager.Current;
            if (chase != null && race != null && race.Player != null && race.Player.vehicle != null)
            {
                var me = race.Player.vehicle.Transform;
                if (me != null && (chase.target == null || chase.target.root != me.root)) { chase.target = me; bike = null; }
            }
            if (bike == null && chase != null && chase.target != null) bike = chase.target.GetComponentInParent<BikeVehicleAdapter>();
            SnapBehind();
            float kmh = bike != null ? Mathf.Abs(bike.SpeedKmh) : 0f;
            float t = Mathf.Clamp01(kmh / fastKmh);
            bool boosting = bike != null && bike.NitroActive;
            if (boosting && !wasBoosting) kick = 1f;
            wasBoosting = boosting;
            kick = Mathf.MoveTowards(kick, boosting ? 0.6f : 0f, Time.unscaledDeltaTime * 1.5f);

            float want = Mathf.Lerp(baseFov, fastFov, t * t) + kick * boostKick;
            fov = Mathf.Lerp(fov, want, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
            cam.fieldOfView = fov;
            EmitLines(kmh, boosting);
            if (blur != null) blur.intensity.value = blurAtSpeed * Mathf.InverseLerp(30f, fastKmh, kmh) * (boosting ? 1.25f : 1f);

            if (Time.timeScale > 0.9f && t > 0.4f)
            {
                float a = shake * Mathf.InverseLerp(0.4f, 1f, t) * (boosting ? 1.6f : 1f);
                float n = Time.time * 23f;
                transform.position += transform.right * (Mathf.PerlinNoise(n, 0.3f) - 0.5f) * 2f * a
                                    + transform.up * (Mathf.PerlinNoise(0.7f, n) - 0.5f) * 2f * a;
            }
        }
    }
}
