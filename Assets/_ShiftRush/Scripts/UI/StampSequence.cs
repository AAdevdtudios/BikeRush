using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShiftRush.UI
{
    /// <summary>
    /// Results that arrive like rubber stamps: each element drops in big and tilted, speeds up into the screen,
    /// slams down (the camera jolts) and settles with a little bounce, one after another. Plain class driven by
    /// a HUD's Update with unscaled time, so a paused or slowed game doesn't stall it.
    /// </summary>
    public class StampSequence
    {
        class Item
        {
            public RectTransform rt;
            public CanvasGroup cg;
            public float delay, shake, tilt;
            public Vector3 scale;
            public Quaternion rot;
            public bool hit;
        }

        const float Fall = 0.15f;
        readonly List<Item> items = new List<Item>();

        /// <summary>Called on each impact with the element's index (play a thud).</summary>
        public System.Action<int> Impact;

        /// <summary>Seconds until the last element has landed and settled.</summary>
        public float Duration { get; private set; }

        public void Add(Transform t, float delay, float shake = 0.22f, float tilt = -8f)
        {
            var rt = t as RectTransform;
            if (rt == null) return;
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            items.Add(new Item { rt = rt, cg = cg, delay = delay, shake = shake, tilt = tilt, scale = rt.localScale, rot = rt.localRotation });
            Duration = Mathf.Max(Duration, delay + Fall + 0.35f);
        }

        /// <summary>Everything hidden, ready to stamp.</summary>
        public void Hide() { foreach (var i in items) i.cg.alpha = 0f; }

        /// <summary>Drives every element for time t since the sequence started. True once all have settled.</summary>
        public bool Run(float t)
        {
            for (int n = 0; n < items.Count; n++)
            {
                var i = items[n];
                float a = t - i.delay;
                if (a < 0f) { i.cg.alpha = 0f; continue; }
                if (a < Fall)
                {
                    float f = a / Fall, e = f * f; // accelerating into the screen
                    i.cg.alpha = Mathf.Clamp01(f * 3f);
                    i.rt.localScale = i.scale * Mathf.Lerp(2.4f, 0.9f, e);
                    i.rt.localRotation = i.rot * Quaternion.Euler(0f, 0f, Mathf.Lerp(i.tilt * 2.2f, i.tilt * 0.15f, e));
                    continue;
                }
                if (!i.hit)
                {
                    i.hit = true;
                    CameraShake.Kick(i.shake);
                    Impact?.Invoke(n);
                }
                float s = a - Fall;
                i.cg.alpha = 1f;
                i.rt.localScale = i.scale * (1f - 0.1f * Mathf.Cos(s * 22f) * Mathf.Exp(-s * 9f));
                i.rt.localRotation = i.rot * Quaternion.Euler(0f, 0f, i.tilt * 0.15f * Mathf.Exp(-s * 4f));
            }
            return t >= Duration;
        }

        /// <summary>Jump to the end: everything landed.</summary>
        public void Finish()
        {
            foreach (var i in items) { i.hit = true; i.cg.alpha = 1f; i.rt.localScale = i.scale; i.rt.localRotation = i.rot; }
        }
    }

    /// <summary>
    /// A short jolt of the main camera, applied only while it renders (and taken off straight after), so it
    /// never fights whatever script moves the camera. Kick from anywhere; it dies away in a fraction of a second.
    /// </summary>
    public static class CameraShake
    {
        static float amount, lastTime;
        static bool hooked;
        static Camera applied;
        static Vector3 offset;

        public static void Kick(float strength)
        {
            amount = Mathf.Min(Mathf.Max(amount * Decay(), strength), 1f);
            lastTime = Time.unscaledTime;
            if (hooked) return;
            hooked = true;
            RenderPipelineManager.beginCameraRendering += Begin;
            RenderPipelineManager.endCameraRendering += End;
        }

        // The clock restarts with each play session while statics can survive it: never let "time since the
        // kick" go negative, or the decay explodes and the camera is thrown to infinity.
        static float Decay()
        {
            float since = Time.unscaledTime - lastTime;
            if (since < 0f) { amount = 0f; lastTime = Time.unscaledTime; return 0f; }
            return Mathf.Exp(-since * 9f);
        }

        static void Begin(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam != Camera.main || applied != null) return;
            float a = amount * Decay();
            if (a < 0.002f || float.IsNaN(a) || float.IsInfinity(a)) return;
            float t = Time.unscaledTime * 60f;
            offset = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * 2f * a;
            offset = cam.transform.rotation * offset;
            cam.transform.position += offset;
            applied = cam;
        }

        static void End(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam != applied) return;
            if (cam != null) cam.transform.position -= offset;
            applied = null;
        }
    }
}
