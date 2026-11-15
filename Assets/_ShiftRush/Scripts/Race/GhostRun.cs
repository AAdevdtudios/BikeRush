using System.Collections.Generic;
using System.IO;
using ShiftRush.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShiftRush.Race
{
    /// <summary>
    /// Ghost Run: a solo run against the clock. Records the player's ride from GO (position, rotation and how far
    /// round the run they are, 20 times a second) and, when a run beats the best on this trail, saves it. The saved
    /// run rides alongside as a see-through ghost on the bike it was set on, and <see cref="Delta"/> says how far
    /// ahead (negative) or behind the player is at this point of the trail. Added by <see cref="RaceManager"/>.
    /// </summary>
    public class GhostRun : MonoBehaviour
    {
        const float Step = 0.05f;
        const int Version = 1;

        struct Sample { public float t, score; public Vector3 pos; public Quaternion rot; }

        public static GhostRun Current { get; private set; }

        /// <summary>A saved ghost is riding.</summary>
        public bool HasGhost { get { return ghost.Count > 1; } }
        /// <summary>Seconds behind the ghost at the player's spot (negative = ahead). Null before it means anything.</summary>
        public float? Delta { get; private set; }
        /// <summary>The best full run on this trail, 0 when there's none yet.</summary>
        public float BestTotal { get; private set; }
        /// <summary>Set when the run finishes: the time the old best was (0 if none) and whether this beat it.</summary>
        public float PreviousBest { get; private set; }
        public bool NewBest { get; private set; }
        public string GhostBikeName { get; private set; }
        public Transform GhostTransform { get { return ghostVisual != null ? ghostVisual.transform : null; } }

        RaceManager race;
        readonly List<Sample> recording = new List<Sample>();
        readonly List<Sample> ghost = new List<Sample>();
        string ghostBikeId;
        GameObject ghostVisual;
        Material ghostMat;
        float nextSample;
        bool settled;
        int cursor;

        string Path { get { return System.IO.Path.Combine(Application.persistentDataPath, "ghost_" + SceneManager.GetActiveScene().name + ".bin"); } }

        public static GhostRun Attach(RaceManager race)
        {
            var g = race.gameObject.AddComponent<GhostRun>();
            g.race = race;
            return g;
        }

        void Awake() { Current = this; }
        void OnDestroy() { if (Current == this) Current = null; if (ghostMat != null) Destroy(ghostMat); }

        void Start()
        {
            Load();
            if (race != null) race.Finished += OnFinished;
            if (HasGhost) MakeGhost();
        }

        void LateUpdate()
        {
            if (race == null || race.Player == null) return;
            var p = race.Player;
            float t = race.CurrentState == RaceManager.State.Countdown ? 0f : race.RaceTime;

            if (race.CurrentState == RaceManager.State.Racing && !p.finished && t >= nextSample)
            {
                var tr = p.vehicle.Transform;
                float score = p.Score(race.Line.length);
                if (recording.Count > 0) score = Mathf.Max(score, recording[recording.Count - 1].score); // never backwards
                recording.Add(new Sample { t = t, score = score, pos = tr.position, rot = tr.rotation });
                nextSample = t + Step;
            }

            if (!HasGhost) { Delta = null; return; }
            PlaceGhost(t);
            Delta = p.started && race.CurrentState != RaceManager.State.Countdown && !p.finished ? DeltaAt(p.Score(race.Line.length), t) : Delta;
        }

        void PlaceGhost(float t)
        {
            if (ghostVisual == null) return;
            while (cursor < ghost.Count - 2 && ghost[cursor + 1].t <= t) cursor++;
            var a = ghost[cursor];
            var b = ghost[Mathf.Min(cursor + 1, ghost.Count - 1)];
            float k = b.t > a.t ? Mathf.Clamp01((t - a.t) / (b.t - a.t)) : 0f;
            ghostVisual.transform.SetPositionAndRotation(Vector3.Lerp(a.pos, b.pos, k), Quaternion.Slerp(a.rot, b.rot, k));

            // Fade it right down when it's on top of the player, so it never hides the trail ahead.
            var me = race.Player.vehicle.Transform.position;
            float d = Vector3.Distance(me, ghostVisual.transform.position);
            bool done = t > ghost[ghost.Count - 1].t + 1.5f;
            float alpha = done ? 0f : Mathf.Lerp(0.08f, 0.5f, Mathf.InverseLerp(2f, 9f, d));
            ghostMat.SetFloat("_Alpha", alpha);
        }

        /// <summary>When the ghost was where the player is now (by distance round the run); minus now.</summary>
        float DeltaAt(float score, float now)
        {
            if (score <= ghost[0].score) return 0f;
            int lo = 0, hi = ghost.Count - 1;
            if (score >= ghost[hi].score) return now - ghost[hi].t;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (ghost[mid].score < score) lo = mid; else hi = mid;
            }
            float k = Mathf.InverseLerp(ghost[lo].score, ghost[hi].score, score);
            return now - Mathf.Lerp(ghost[lo].t, ghost[hi].t, k);
        }

        void OnFinished(RaceManager.Racer r) { Finish(r); }

        /// <summary>Settles the run (best or not, saved or not). Safe to call more than once; the HUD calls it
        /// so the results screen can read the outcome straight away.</summary>
        public void Finish(RaceManager.Racer r)
        {
            if (r == null || !r.isPlayer || settled) return;
            settled = true;
            PreviousBest = BestTotal;
            NewBest = BestTotal <= 0f || r.finishTime < BestTotal;
            Delta = BestTotal > 0f ? r.finishTime - BestTotal : (float?)null;
            if (!NewBest) return;
            BestTotal = r.finishTime;
            var tr = r.vehicle.Transform;
            recording.Add(new Sample { t = r.finishTime, score = TotalScore(), pos = tr.position, rot = tr.rotation });
            Save(RaceSession.Car != null ? RaceSession.Car.id : "");
        }

        float TotalScore() { return race.TotalLaps * race.Line.length; }

        // ---- saved runs

        void Save(string bikeId)
        {
            try
            {
                using (var w = new BinaryWriter(File.Open(Path, FileMode.Create)))
                {
                    w.Write(Version);
                    w.Write(bikeId ?? "");
                    w.Write(BestTotal);
                    w.Write(recording.Count);
                    foreach (var s in recording)
                    {
                        w.Write(s.t); w.Write(s.score);
                        w.Write(s.pos.x); w.Write(s.pos.y); w.Write(s.pos.z);
                        w.Write(s.rot.x); w.Write(s.rot.y); w.Write(s.rot.z); w.Write(s.rot.w);
                    }
                }
            }
            catch (System.Exception e) { Debug.LogWarning("[Ghost Run] Couldn't save the ghost: " + e.Message); }
        }

        void Load()
        {
            ghost.Clear();
            BestTotal = 0f;
            if (!File.Exists(Path)) return;
            try
            {
                using (var r = new BinaryReader(File.OpenRead(Path)))
                {
                    if (r.ReadInt32() != Version) return;
                    ghostBikeId = r.ReadString();
                    BestTotal = r.ReadSingle();
                    int n = r.ReadInt32();
                    for (int i = 0; i < n; i++)
                        ghost.Add(new Sample
                        {
                            t = r.ReadSingle(), score = r.ReadSingle(),
                            pos = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()),
                            rot = new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle()),
                        });
                }
            }
            catch (System.Exception e) { Debug.LogWarning("[Ghost Run] Couldn't read the ghost: " + e.Message); ghost.Clear(); BestTotal = 0f; }
            Mend();
        }

        /// <summary>
        /// Ghosts saved before the lap-line fix can hold a moment where the run read a whole lap behind or ahead.
        /// Put any such jump back by a lap and keep the distance from ever going backwards, so the gap stays true.
        /// </summary>
        void Mend()
        {
            float len = race != null && race.Line != null ? race.Line.length : 0f;
            for (int i = 1; i < ghost.Count; i++)
            {
                var s = ghost[i];
                float prev = ghost[i - 1].score;
                if (len > 0f)
                {
                    while (s.score - prev > len * 0.5f) s.score -= len;
                    while (prev - s.score > len * 0.5f) s.score += len;
                }
                s.score = Mathf.Max(s.score, prev);
                ghost[i] = s;
            }
        }

        // ---- the see-through rider

        void MakeGhost()
        {
            var cat = RaceCatalog.Instance;
            RaceCatalog.Car car = null;
            if (cat != null) foreach (var c in cat.cars) if (c.id == ghostBikeId) car = c;
            if (car == null) car = RaceSession.Car;
            if (car == null || car.prefab == null) return;
            GhostBikeName = car.displayName.ToUpperInvariant();

            var shader = Shader.Find("BikeRush/Ghost");
            var template = Resources.Load<Material>("BR_Ghost");
            ghostMat = template != null ? new Material(template) : shader != null ? new Material(shader) : null;
            if (ghostMat == null) return;

            // Made inside a switched-off holder so none of the bike's own scripts wake up, then stripped to looks.
            var holder = new GameObject("Ghost Holder");
            holder.SetActive(false);
            var g = Instantiate(car.prefab, holder.transform);
            Strip(g);
            foreach (var rend in g.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[rend.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = ghostMat;
                rend.sharedMaterials = mats;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }
            g.name = "Ghost";
            g.transform.SetParent(null, false);
            SceneManager.MoveGameObjectToScene(g, gameObject.scene);
            Destroy(holder);
            g.SetActive(true);
            ghostVisual = g;
            PlaceGhost(0f);
        }

        /// <summary>Everything but meshes and bones (and the rider's animator, for its pose).</summary>
        static void Strip(GameObject g)
        {
            foreach (var j in g.GetComponentsInChildren<Joint>(true)) DestroyImmediate(j);
            foreach (var ps in g.GetComponentsInChildren<ParticleSystem>(true)) ps.gameObject.SetActive(false);
            foreach (var tr in g.GetComponentsInChildren<TrailRenderer>(true)) tr.enabled = false;
            foreach (var l in g.GetComponentsInChildren<Light>(true)) l.enabled = false;
            foreach (var a in g.GetComponentsInChildren<AudioSource>(true)) DestroyImmediate(a);
            // Scripts, least-depended-on first, so nothing is removed while another script needs it.
            for (int pass = 0; pass < 8; pass++)
            {
                var all = g.GetComponentsInChildren<MonoBehaviour>(true);
                if (all.Length == 0) break;
                bool removed = false;
                foreach (var mb in all)
                {
                    if (mb == null || Needed(mb, all)) continue;
                    DestroyImmediate(mb);
                    removed = true;
                }
                if (!removed) { foreach (var mb in all) if (mb != null) mb.enabled = false; break; }
            }
            foreach (var c in g.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
            foreach (var rb in g.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(rb);
            foreach (var an in g.GetComponentsInChildren<Animator>(true)) an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        static bool Needed(Component c, Component[] all)
        {
            var t = c.GetType();
            foreach (var o in all)
            {
                if (o == null || o == c || o.gameObject != c.gameObject) continue;
                foreach (RequireComponent rc in o.GetType().GetCustomAttributes(typeof(RequireComponent), true))
                    if ((rc.m_Type0 != null && rc.m_Type0.IsAssignableFrom(t)) || (rc.m_Type1 != null && rc.m_Type1.IsAssignableFrom(t)) || (rc.m_Type2 != null && rc.m_Type2.IsAssignableFrom(t)))
                        return true;
            }
            return false;
        }
    }
}
