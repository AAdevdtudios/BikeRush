using ShiftRush.Vehicles;
using UnityEngine;

namespace ShiftRush.Race
{
    /// <summary>
    /// Ties the player's bike to the race line. The trail does the cornering: the bike always heads for a point
    /// down the line ahead, and the stick picks where across the trail that point is (left side, middle, right
    /// side). Let go and the bike holds its line through the bends; it never steers itself off the edge.
    /// Ride far off the trail (a shortcut, a crash) and the help lets go until you're back.
    /// </summary>
    public class TrackGuide : MonoBehaviour, ISteerGuide
    {
        /// <summary>Turned off from Settings for riders who want raw steering.</summary>
        public static bool Enabled = true;

        [Tooltip("How far ahead the bike aims (m), plus this many metres per m/s of speed.")]
        [SerializeField] float lookBase = 6f, lookPerSpeed = 0.55f;
        [Tooltip("How fast full steer moves your line across the trail (m/s).")]
        [SerializeField] float laneSpeed = 9f;
        [Tooltip("Extra aim offset at full steer (m), so the bike answers the stick at once.")]
        [SerializeField] float laneReach = 1.5f;
        [Tooltip("How hard the bike is pulled back onto your line when it drifts off it.")]
        [SerializeField] float holdLine = 2.5f;
        [Tooltip("Hands off, your line drifts back inside this share of the trail (the edges are for when you hold the stick).")]
        [SerializeField, Range(0f, 1f)] float restShare = 0.6f;
        [Tooltip("How fast (m/s) that drift back is.")]
        [SerializeField] float restSpeed = 1.5f;
        [Tooltip("Keep the aim this far in from the trail edge (m).")]
        [SerializeField] float edgeMargin = 1.3f;
        [Tooltip("Beyond this far outside the trail (m) the help lets go.")]
        [SerializeField] float letGoOutside = 5f;

        RaceLine line;
        int nearest = -1;
        float held; // your line across the trail (m, + = right); only the stick moves it
        bool engaged;

        public static TrackGuide Attach(GameObject bike, RaceLine line)
        {
            var g = bike.GetComponent<TrackGuide>();
            if (g == null) g = bike.AddComponent<TrackGuide>();
            g.line = line; g.engaged = false;
            return g;
        }

        public bool Aim(Vector3 position, float speed, float steer, out Vector3 direction)
        {
            direction = Vector3.zero;
            if (!Enabled || line == null || line.points == null || line.points.Length < 2) return false;
            nearest = line.Nearest(position, nearest);
            float lateral = line.Lateral(position, nearest);
            if (Mathf.Abs(lateral) > line.halfWidth + letGoOutside) { engaged = false; return false; }

            float edge = Mathf.Max(0.5f, line.halfWidth - edgeMargin);
            float rest = edge * restShare;
            if (!engaged) { held = Mathf.Clamp(lateral, -rest, rest); engaged = true; } // pick up near where the bike is
            float dt = Time.fixedDeltaTime;
            held = Mathf.Clamp(held + steer * laneSpeed * dt, -edge, edge);
            if (Mathf.Abs(steer) < 0.1f) held = Mathf.MoveTowards(held, Mathf.Clamp(held, -rest, rest), restSpeed * dt);
            float lane = held + steer * laneReach;
            // Follow the trail's own direction (a touch ahead), turned towards your line: no corner cutting.
            float d = line.distances[nearest];
            Vector3 along = line.Direction(line.IndexAt(d + 2f + speed * 0.15f));
            float look = Mathf.Clamp(lookBase + speed * lookPerSpeed, 7f, 30f);
            float correct = Mathf.Clamp(Mathf.Atan2((lane - lateral) * holdLine, look * 0.6f) * Mathf.Rad2Deg, -22f, 22f);
            direction = Quaternion.AngleAxis(correct, Vector3.up) * along;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) return false;
            direction.Normalize();
            return true;
        }
    }
}
