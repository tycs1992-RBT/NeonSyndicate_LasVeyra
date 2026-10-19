using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class CompanionController
    {
        /// <summary>A nearest-hit sweep, including at the start, protects every follow, hop and assist step.</summary>
        private bool SweepStep04G(Vector3 from, Vector3 wanted, out Vector3 accepted)
        {
            accepted = wanted;
            Vector3 delta = wanted - from;
            float distance = delta.magnitude;
            if (distance < 0.0001f) return true;
            float radius = Mathf.Clamp(Profile.BodyRadius * 0.6f, 0.16f, 0.38f);
            float height = Mathf.Max(radius + 0.08f, Profile.BodyHeight * 0.5f);
            Vector3 origin = from + Vector3.up * height;
            int mask = KingdomCombatRules04N.CompanionObstacleMask;
            if (Physics.CheckSphere(origin, radius, mask, QueryTriggerInteraction.Ignore))
            { accepted = from; return false; }
            RaycastHit hit;
            if (!Physics.SphereCast(origin, radius, delta / distance, out hit, distance + 0.04f, mask, QueryTriggerInteraction.Ignore)) return true;
            accepted = from + delta / distance * Mathf.Max(0f, hit.distance - 0.05f);
            return false;
        }

        private void TickPersonality04G()
        {
            // The event handler owns a UID 0 body's look and alert; never replace its rescue or combat direction.
            if (Uid <= 0 || Mode == CompanionMode.Downed || Mode == CompanionMode.Rescue || ActionPlaying || Mode == CompanionMode.Goal) return;
            SetAlert(Combat ? 0.8f : Archetype==BondIds.StormSkimmer?.25f:Archetype==BondIds.AshWarden?.06f:.12f);
            if (Combat || _vel.sqrMagnitude > 0.12f) { _idleLookUntil04G = 0f; return; }
            if (_clock >= _idleLookAt04G)
            {
                _idleLookAt04G = _clock + 5f + Mathf.Abs(Uid % 4);
                PlayerController player = Game.Player;
                if (player != null)
                {
                    _idleLookPoint04G = player.ChestPosition + player.transform.right * Mathf.Sin(_clock * 0.4f) * 2f;
                    _idleLookUntil04G = _clock + 0.9f;
                    if (Game.Audio != null && Vector3.Distance(player.transform.position, transform.position) < 6f)
                        Game.Audio.PlayAt(SfxId.CrystalChime, HeadPosition, 0.08f,
                            Archetype==BondIds.AshWarden?.4f:Archetype==BondIds.StormSkimmer?.9f:.65f, 9f);
                }
            }
            // Run after normal steering and retain the brief glance, rather than replacing it again in the same frame.
            if (_clock < _idleLookUntil04G) SetLook(_idleLookPoint04G);
        }
    }
}
