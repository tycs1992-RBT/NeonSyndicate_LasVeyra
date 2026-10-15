using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Opt-in 04B habitat bounds on the existing brain, flight steering and animation. No second creature AI.</summary>
    public sealed partial class Creature
    {
        private bool _territoryHabitat;
        private float _territoryRoam;
        private bool _territoryAerial;

        public void ConfigureTerritoryHabitat(float radius, bool aerial)
        {
            _territoryHabitat = true;
            _territoryRoam = Mathf.Clamp(radius, 0f, aerial ? 9f : 3f);
            _territoryAerial = aerial;
        }

        private float HabitatRoamRadius
        {
            get { return _territoryHabitat ? (_territoryAerial ? 0f : _territoryRoam) : 1.25f; }
        }

        private bool TerritoryPatrolSteer(float dt, out Vector3 velocity)
        {
            velocity = Vector3.zero;
            if (!_territoryHabitat || !_territoryAerial || !CalmHabitatResident) return false;
            float radius = Mathf.Max(3f, _territoryRoam);
            float speed = Mathf.Min(5f, _sp != null ? _sp.FlySpeed * 0.35f : 4f);
            _orbitAngle += FlightMath.OrbitRate(radius, speed) * dt;
            // Home is the actual air spawn. Do not add the ordinary 14m cruise height a second time.
            Vector3 goal = FlightMath.OrbitPoint(Home, radius, _orbitAngle, Mathf.Sin(Time.time * 0.3f + Seed) * 0.6f);
            goal.y = Mathf.Max(goal.y, _airGround + 6f);
            velocity = Seek(goal, speed);
            return true;
        }
    }
}
