using System.Collections.Generic;
using ShiftRush.Vehicles;
using UnityEngine;

namespace ShiftRush.Race
{
    /// <summary>
    /// A glowing strip on the trail: ride over it and you get a shove plus a few seconds of free boost pace.
    /// Works for the player and rivals alike, and rivals know where the pads are (<see cref="All"/>) and line up
    /// for them. The arrows on the strip scroll towards the rider so it reads from far off.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class BoostPad : MonoBehaviour
    {
        public static readonly List<BoostPad> All = new List<BoostPad>();

        [Tooltip("Instant speed added (m/s), capped at pad top speed.")]
        public float kick = 6f;
        [Tooltip("Seconds of boost pace after the pad.")]
        public float surge = 1.6f;
        [Tooltip("Distance along the race line (m) and offset across it (m, + = right); filled in by the course builder.")]
        public float distance, lateral;

        Renderer strip;
        static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
        MaterialPropertyBlock block;
        readonly Dictionary<BikeVehicleAdapter, float> lastHit = new Dictionary<BikeVehicleAdapter, float>();

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            strip = GetComponentInChildren<Renderer>();
            block = new MaterialPropertyBlock();
        }

        void Update()
        {
            if (strip == null) return;
            // Scroll the chevrons along the pad (texture runs along local +Z).
            block.SetVector(BaseMapST, new Vector4(1f, 3f, 0f, -Time.time * 2.2f));
            strip.SetPropertyBlock(block);
        }

        void OnTriggerEnter(Collider other)
        {
            var bike = other.GetComponentInParent<BikeVehicleAdapter>();
            if (bike == null) return;
            // One hit per pass (a bike has several colliders).
            if (lastHit.TryGetValue(bike, out float t) && Time.time - t < 1f) return;
            lastHit[bike] = Time.time;
            if (Vector3.Dot(bike.transform.forward, transform.forward) < 0.2f) return; // ridden backwards: nothing
            bike.PadBoost(transform.forward, kick, surge);
            Hit?.Invoke(bike, this);
        }

        /// <summary>Raised when a bike takes a pad (sound, HUD flash).</summary>
        public static event System.Action<BikeVehicleAdapter, BoostPad> Hit;
    }
}
