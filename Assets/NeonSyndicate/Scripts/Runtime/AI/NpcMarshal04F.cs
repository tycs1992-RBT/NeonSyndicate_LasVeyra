using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A command unit supports only existing officers, at most three small rally pulses. It never summons reinforcements.</summary>
    public sealed class NpcMarshal04F : MonoBehaviour
    {
        public NpcController Owner;
        private int _pulses;
        private float _nextPulse;
        private void Update()
        {
            if (!Game.IsPlaying || Owner == null || Owner.IsDead || Owner.CrystalRole04F != CrystalNpcRole04F.Marshal || Owner.State != NpcState.Fight || _pulses >= 3) return;
            if (Time.time < _nextPulse) return;
            _nextPulse = Time.time + 14f;
            _pulses++;
            foreach (NpcController officer in NpcController.All)
            {
                if (officer == null || officer.IsDead || officer.Health == null || officer == Owner) continue;
                if (officer.Role != NpcRole.Police && officer.Role != NpcRole.Swat) continue;
                if (MathUtil.FlatDistance(Owner.transform.position, officer.transform.position) > 12f) continue;
                if (Owner.CityActor04F == null && officer.Persistent) continue;
                if (Owner.CityActor04F != null && (officer.CityActor04F == null || officer.CityActor04F.Event != Owner.CityActor04F.Event || officer.CityActor04F.Side != Owner.CityActor04F.Side)) continue;
                officer.Poise = Mathf.Min(officer.MaxPoise, officer.Poise + 12f);
                if (Game.Fx != null) Game.Fx.Sparks(officer.ChestPosition, Vector3.up, 3);
            }
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.CrystalChime, Owner.ChestPosition, 0.55f, 0.65f, 40f);
            if (Game.Fx != null) Game.Fx.Sparks(Owner.ChestPosition, Vector3.up * 2f, 6);
        }
    }
}
