using System.Collections.Generic;
using ShiftRush.Vehicles;
using UnityEngine;

namespace ShiftRush.AirShow
{
    /// <summary>
    /// A glowing frame hanging over a jump. While the player's bike is inside it, the stunt prompt can open
    /// (see <see cref="AirShowRun"/>): fly through and hit Shift on the diamond. Idle it breathes; with the bike
    /// inside it burns bright; once a trick has been landed through it, it stays lit green.
    /// The trigger sits on Ignore Raycast so the bike's ground checks look straight through it.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class TrickBox : MonoBehaviour
    {
        static readonly List<TrickBox> all = new List<TrickBox>();

        public static bool PlayerInside
        {
            get { foreach (var b in all) if (b.inside) return true; return false; }
        }

        [SerializeField] Renderer[] edges;
        [SerializeField] Renderer fill;
        [SerializeField] Color idle = new Color32(255, 158, 46, 255);
        [SerializeField] Color hot = new Color32(255, 236, 150, 255);
        [SerializeField] Color done = new Color32(61, 255, 138, 255);

        readonly HashSet<Collider> touching = new HashSet<Collider>();
        MaterialPropertyBlock mpb;
        bool inside, cleared;
        float lastInside = -99f, heat;

        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }

        void Start()
        {
            mpb = new MaterialPropertyBlock();
            if (AirShowRun.Current != null) AirShowRun.Current.Scored += OnScored;
        }

        void OnDestroy() { if (AirShowRun.Current != null) AirShowRun.Current.Scored -= OnScored; }

        static bool IsPlayer(Collider c)
        {
            var b = c.GetComponentInParent<BikeVehicleAdapter>();
            return b != null && !b.IsAIDriven;
        }

        void OnTriggerEnter(Collider c) { if (IsPlayer(c)) touching.Add(c); }
        void OnTriggerExit(Collider c) { touching.Remove(c); }

        void OnScored(int points, string headline, string grade)
        {
            if (points > 0 && Time.time - lastInside < 2.5f) cleared = true;
        }

        void Update()
        {
            touching.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            inside = touching.Count > 0;
            if (inside) lastInside = Time.time;
            heat = Mathf.MoveTowards(heat, inside ? 1f : 0f, Time.unscaledDeltaTime * 6f);

            float breathe = 0.65f + 0.35f * Mathf.Sin(Time.time * 3f + transform.position.x * 0.1f);
            Color c = cleared ? done : Color.Lerp(idle, hot, heat);
            float glow = cleared ? 2.2f : Mathf.Lerp(1.6f * breathe, 5f, heat);
            Paint(edges, c, glow, 1f);
            // The fill is only a tint: any real glow on a surface this big floods the screen through bloom.
            if (fill != null) Paint(new[] { fill }, c, 0.05f, cleared ? 0.05f : Mathf.Lerp(0.04f, 0.12f, heat));
        }

        void Paint(Renderer[] rs, Color c, float glow, float alpha)
        {
            if (rs == null) return;
            foreach (var r in rs)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", new Color(c.r, c.g, c.b, alpha));
                mpb.SetColor("_EmissionColor", c * glow);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
