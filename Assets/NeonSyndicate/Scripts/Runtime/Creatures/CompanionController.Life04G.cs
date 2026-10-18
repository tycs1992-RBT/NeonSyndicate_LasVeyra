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
            int mask = Layers.WorldMask | (1 << Layers.Vehicles) | (1 << Layers.VehicleShell);
            if (Physics.CheckSphere(origin, radius, mask, QueryTriggerInteraction.Ignore))
            { accepted = from; return false; }
            RaycastHit hit;
            if (!Physics.SphereCast(origin, radius, delta / distance, out hit, distance + 0.04f, mask, QueryTriggerInteraction.Ignore)) return true;
            accepted = from + delta / distance * Mathf.Max(0f, hit.distance - 0.05f);
            return false;
        }

        private void TickPersonality04G()
        {
            if (Mode == CompanionMode.Downed || Mode == CompanionMode.Rescue || ActionPlaying) return;
            SetAlert(Combat ? 0.8f : 0.12f);
            if (_clock < _idleLookAt04G || _vel.sqrMagnitude > 0.12f || Mode == CompanionMode.Goal) return;
            _idleLookAt04G = _clock + 5f + Mathf.Abs(Uid % 4);
            // A quiet sniff and head check use the existing rig rather than random lights or spawned effects.
            PlayerController player = Game.Player;
            if (player != null && !Combat)
            {
                SetLook(player.ChestPosition + player.transform.right * Mathf.Sin(_clock * 0.4f) * 2f);
                if (Game.Audio != null && Vector3.Distance(player.transform.position, transform.position) < 6f)
                    Game.Audio.PlayAt(SfxId.CrystalChime, HeadPosition, 0.08f, 0.55f, 9f);
            }
        }
    }
}
