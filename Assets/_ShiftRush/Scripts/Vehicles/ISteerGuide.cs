using UnityEngine;

namespace ShiftRush.Vehicles
{
    /// <summary>
    /// Steering help from the track. Given where the bike is and the rider's steer (-1..1), returns the flat
    /// direction the bike should be heading. False = no help here (way off the trail, no track).
    /// </summary>
    public interface ISteerGuide
    {
        bool Aim(Vector3 position, float speed, float steer, out Vector3 direction);
    }
}
