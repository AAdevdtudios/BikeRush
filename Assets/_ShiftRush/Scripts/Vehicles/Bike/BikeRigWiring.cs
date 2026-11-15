using System.Collections.Generic;
using System.Reflection;
using SBPScripts;
using UnityEngine;

namespace ShiftRush.Vehicles
{
    /// <summary>
    /// The bike pack was made for one bike in a scene: its rider animation, suspension, sounds and ragdoll each
    /// grab "the first BicycleController" they find in their Start. With a grid of bikes every rider then
    /// animated off one bike, and after a restart that bike could be a fresh one whose rb wasn't set yet
    /// (UnassignedReferenceException). This sets rb as soon as the bike wakes, and keeps every one of those
    /// scripts pointed at its own bike (their Start can run late, when a rider is switched on).
    /// Done from our side so the pack's files stay untouched.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class BikeRigWiring : MonoBehaviour
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        struct Link { public Component target; public FieldInfo field; public Object value; }
        readonly List<Link> links = new List<Link>();

        void Awake()
        {
            var own = GetComponent<BicycleController>();
            if (own == null) { enabled = false; return; }
            if (own.rb == null) own.rb = own.GetComponent<Rigidbody>();
            var status = GetComponent<BicycleStatus>();
            var rider = own.GetComponentInChildren<CyclistAnimController>(true);

            foreach (var c in GetComponentsInChildren<CyclistAnimController>(true))
            {
                Add(c, "bicycleController", own);
                Add(c, "bicycleStatus", status);
            }
            foreach (var c in GetComponentsInChildren<SuspensionManager>(true)) Add(c, "bicycleController", own);
            foreach (var c in GetComponentsInChildren<BicycleSounds>(true)) Add(c, "bicycleController", own);
            foreach (var c in GetComponentsInChildren<RagdollJointImitation>(true))
            {
                Add(c, "bicycleController", own);
                if (rider != null) Add(c, "CharacterCopyPosition", rider.gameObject);
            }
            Apply();
        }

        void Start() { Apply(); }
        void LateUpdate() { Apply(); }

        void Add(Component target, string field, Object value)
        {
            if (target == null || value == null) return;
            var f = target.GetType().GetField(field, Any);
            if (f != null && f.FieldType.IsInstanceOfType(value)) links.Add(new Link { target = target, field = f, value = value });
        }

        void Apply()
        {
            for (int i = 0; i < links.Count; i++)
            {
                var l = links[i];
                if (l.target == null) continue;
                if (!ReferenceEquals(l.field.GetValue(l.target), l.value)) l.field.SetValue(l.target, l.value);
            }
        }
    }
}
