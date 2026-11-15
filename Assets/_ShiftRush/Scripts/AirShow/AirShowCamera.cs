using UnityEngine;

namespace ShiftRush.AirShow
{
    /// <summary>
    /// The side-on camera: off to the bike's left, looking across the course, leading a little ahead of the bike
    /// so the next box is in view. Pulls back and widens as the bike gets big air, and eases in at the finish.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class AirShowCamera : MonoBehaviour
    {
        [SerializeField] float distance = 15f;
        [SerializeField] float height = 2.2f;
        [SerializeField] float lead = 0.35f;
        [SerializeField] float maxLead = 9f;
        [SerializeField] float fov = 38f;
        [SerializeField] float airFov = 48f;
        [SerializeField] float follow = 5f;
        [SerializeField] float verticalFollow = 3f;

        Camera cam;
        Vector3 focus;
        float shownLead, air, finishPush;
        bool placed;

        void Awake() { cam = GetComponent<Camera>(); }

        void LateUpdate()
        {
            var run = AirShowRun.Current;
            var bike = run != null ? run.Bike : null;
            if (bike == null || bike.Body == null) return;
            float dt = Time.unscaledDeltaTime;

            Vector3 p = bike.Body.position;
            Vector3 v = bike.Body.linearVelocity;
            bool flying = !bike.AnyWheelGrounded;
            air = Mathf.MoveTowards(air, flying ? 1f : 0f, dt * (flying ? 1.2f : 0.8f));
            if (run.CurrentState == AirShowRun.State.Finished) finishPush = Mathf.MoveTowards(finishPush, 1f, dt * 0.5f);

            shownLead = Mathf.Lerp(shownLead, Mathf.Clamp(v.x * lead, 0f, maxLead), 1f - Mathf.Exp(-dt * 2f));
            Vector3 target = new Vector3(p.x + shownLead, p.y + height * (1f + 0.6f * air), 0f);
            if (!placed) { focus = target; placed = true; }
            focus.x = Mathf.Lerp(focus.x, target.x, 1f - Mathf.Exp(-dt * follow * 2f));
            focus.y = Mathf.Lerp(focus.y, target.y, 1f - Mathf.Exp(-dt * verticalFollow));

            float dist = distance * (1f + 0.25f * air) * (1f - 0.3f * finishPush);
            transform.position = focus + new Vector3(0f, 1.2f, -dist);
            transform.rotation = Quaternion.LookRotation(focus - transform.position + Vector3.up * -0.6f, Vector3.up);
            cam.fieldOfView = Mathf.Lerp(fov, airFov, air);
        }
    }
}
