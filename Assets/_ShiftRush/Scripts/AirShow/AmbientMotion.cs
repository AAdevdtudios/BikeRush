using UnityEngine;

namespace ShiftRush.AirShow
{
    /// <summary>
    /// Life in the Air Show backdrop. One small mover for everything that isn't the course: clouds drifting,
    /// balloons bobbing up, turbine blades turning, the cable car going back and forth, flags flapping and birds
    /// flying across. Runs on game time, so a stunt's slow motion slows the whole world with it.
    /// </summary>
    public class AmbientMotion : MonoBehaviour
    {
        public enum Kind { Drift, Bob, Spin, Shuttle, Flutter, Fly }

        public Kind kind;
        [Tooltip("Drift/Fly: units a second along X. Spin: degrees a second. Shuttle: units a second. Bob/Flutter: cycles.")]
        public float speed = 1f;
        [Tooltip("Bob: height of the bob. Flutter: degrees of swing. Shuttle: cable sag. Fly: bob height.")]
        public float amount = 1f;
        public Vector3 axis = Vector3.up;
        [Tooltip("Drift/Fly: X range to wrap in (x of a to x of b). Shuttle: the two ends of the cable.")]
        public Vector3 a, b;
        [Tooltip("Fly: the wings, flapped in opposite pairs.")]
        public Transform[] wings;
        [Tooltip("Bob: slow climb in units a second, wrapping back down after rising this high (in amount units).")]
        public float rise;

        Vector3 start;
        Quaternion startRot;
        float phase, t0;

        void Start()
        {
            start = transform.position;
            startRot = transform.localRotation;
            phase = Mathf.Repeat(start.x * 0.37f + start.z * 0.11f, Mathf.PI * 2f);
            t0 = Mathf.Repeat(start.x * 0.05f, 1f);
        }

        void Update()
        {
            float t = Time.time, dt = Time.deltaTime;
            switch (kind)
            {
                case Kind.Drift:
                {
                    var p = transform.position;
                    p.x += speed * dt;
                    if (p.x > b.x) p.x = a.x;
                    transform.position = p;
                    break;
                }
                case Kind.Bob:
                {
                    float climb = rise > 0f ? Mathf.Repeat(t * rise + phase * 3f, 30f) : 0f;
                    transform.position = start + axis * (Mathf.Sin(t * speed + phase) * amount + climb);
                    transform.localRotation = startRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * speed * 0.7f + phase) * 3f);
                    break;
                }
                case Kind.Spin:
                    transform.Rotate(axis, speed * dt, Space.Self);
                    break;
                case Kind.Shuttle:
                {
                    float len = Mathf.Max(0.1f, Vector3.Distance(a, b));
                    float k = Mathf.PingPong(t * speed / len + t0, 1f);
                    float e = Mathf.SmoothStep(0f, 1f, k);
                    transform.position = Vector3.Lerp(a, b, e) + Vector3.down * Mathf.Sin(e * Mathf.PI) * amount;
                    break;
                }
                case Kind.Flutter:
                    transform.localRotation = startRot * Quaternion.AngleAxis(Mathf.Sin(t * speed * Mathf.PI * 2f + phase) * amount
                        + Mathf.Sin(t * speed * 5.3f + phase) * amount * 0.3f, axis);
                    break;
                case Kind.Fly:
                {
                    var p = transform.position;
                    p.x += speed * dt;
                    if (p.x > b.x) p.x = a.x;
                    p.y = start.y + Mathf.Sin(t * 0.8f + phase) * amount;
                    transform.position = p;
                    if (wings != null)
                        for (int i = 0; i < wings.Length; i++)
                            if (wings[i] != null)
                                wings[i].localRotation = Quaternion.Euler((i % 2 == 0 ? 1f : -1f) * Mathf.Sin(t * 9f + phase) * 35f, 0f, 0f); // wings out along z, so they beat about x
                    break;
                }
            }
        }
    }
}
