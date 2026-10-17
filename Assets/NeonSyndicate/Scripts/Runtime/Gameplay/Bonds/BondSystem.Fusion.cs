using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The hand-off to Game.Fusion. CanFuse checks the bond side (active, Attuned or above, not downed, resting, busy,
    /// too hurt or left far behind). SetFused hides the free body through its own lifecycle (never a global cleanup) and
    /// brings it back beside the chassis on separation, where it waits a few seconds before hopping on (riding along)
    /// unless the player steps off. Snapshot and RestoreSnapshot carry health and the assist cooldown across the fused
    /// time with the no-refill rule: health never comes back higher and the cooldown never shorter than it would have
    /// been. Fused is never written to a save (saves fold it back to Following).
    /// </summary>
    public sealed partial class BondSystem
    {
        private struct Taken
        {
            public float Health;
            public float Cooldown;
            public float Time;
        }

        private readonly Dictionary<int, Taken> _snapshots = new Dictionary<int, Taken>();

        /// <summary>True when the companion can fuse now (rank, not downed, active and near).</summary>
        public bool CanFuse(int uid, out string reason)
        {
            reason = "";
            CompanionRecord r = Find(uid);
            if (r == null || !BondIds.Has(r.Archetype)) { reason = "You have no bonded companion for that."; return false; }
            string n = NameOf(r);
            if (Active != r) { reason = n + " is not your active companion. Switch on the BONDS page or at a care station."; return false; }
            if (!BondRules.CanFuseRank(r.Rank)) { reason = n + " must be " + BondRules.RankName(BondIds.FusionRank) + " first (now " + BondRules.RankName(r.Rank) + ")."; return false; }
            switch ((CompanionState)r.State)
            {
                case CompanionState.Downed: reason = n + " is down and resting (" + BondRules.Clock(r.RecoverIn) + ", or at once at a care station)."; return false;
                case CompanionState.Resting: reason = n + " is resting at home. Summon it from the BONDS page."; return false;
                case CompanionState.Fused: reason = n + " is already fused."; return false;
                case CompanionState.Assisting: reason = n + " is busy with your last command."; return false;
            }
            if (_rescue != null) { reason = "Finish the rescue first."; return false; }
            if (r.Health01 < 0.2f) { reason = n + " is too hurt to fuse. Let it rest or care for it first."; return false; }
            PlayerController pl = Game.Player;
            Vector3 pp = pl != null ? (pl.InVehicle && pl.CurrentVehicle != null ? pl.CurrentVehicle.transform.position : pl.transform.position) : Vector3.zero;
            if (_body == null && !_riding) { reason = n + " has not arrived yet."; return false; }
            if (_body != null && !_body.Hidden && pl != null && (_body.transform.position - pp).magnitude > 25f)
            {
                reason = r.State == (int)CompanionState.Waiting ? n + " is waiting where you left it. Step off and call it first." : n + " is too far away. Call it first.";
                return false;
            }
            return true;
        }

        /// <summary>Fusion hides (true) or restores (false) the free body. While fused the companion deals no damage itself.</summary>
        public void SetFused(int uid, bool fused)
        {
            CompanionRecord r = Find(uid);
            if (r == null) return;
            if (fused)
            {
                if (r.State == (int)CompanionState.Fused) return;
                if (Active == r)
                {
                    EndAssist();
                    EndBrace(false);
                }
                r.State = (int)CompanionState.Fused;
                _separationGrace = 0f;
                if (_body != null && Active == r) _body.SetHidden(true);
                Log.Info("Bonds: " + NameOf(r) + " fused.");
            }
            else
            {
                if (r.State != (int)CompanionState.Fused) return;
                r.State = (int)CompanionState.Following;
                if (_body != null && Active == r)
                {
                    if (PlaceBesideVehicleOrPlayer(r)) _separationGrace = 5f;
                    else _riding = true;
                }
                Log.Info("Bonds: " + NameOf(r) + " separated.");
            }
            ReportStatus();
        }

        public CompanionSnapshot Snapshot(int uid)
        {
            CompanionRecord r = Find(uid);
            float cd = CombatCooldownLeft(uid);
            float total = r != null ? BondRules.CombatCooldown(r.Archetype, r.Rank, r.Specialization) : 1f;
            CompanionSnapshot s = new CompanionSnapshot
            {
                Uid = uid,
                Health01 = r != null ? r.Health01 : 1f,
                AssistCooldown = cd,
                AssistCharge = Mathf.Clamp01(1f - cd / Mathf.Max(0.01f, total))
            };
            _snapshots[uid] = new Taken { Health = s.Health01, Cooldown = cd, Time = Time.time };
            return s;
        }

        public void RestoreSnapshot(CompanionSnapshot s)
        {
            CompanionRecord r = Find(s.Uid);
            if (r == null) return;
            Taken t;
            bool had = _snapshots.TryGetValue(s.Uid, out t);
            float health = BondRules.RestoredHealth(had ? t.Health : r.Health01, s.Health01);
            float cd = BondRules.RestoredCooldown(had ? t.Cooldown : CombatCooldownLeft(s.Uid), s.AssistCooldown, had ? Time.time - t.Time : 0f);
            _snapshots.Remove(s.Uid);
            SetCooldown(s.Uid, true, cd);
            if (health <= 0f && r.State != (int)CompanionState.Downed)
            {
                r.Health01 = 0f;
                r.State = (int)CompanionState.Downed;
                r.RecoverIn = BondRules.RecoverSeconds;
                if (Active == r) OnDowned(r);
                return;
            }
            r.Health01 = health;
        }

        private bool PlaceBesideVehicleOrPlayer(CompanionRecord r)
        {
            PlayerController pl = Game.Player;
            if (_body == null || pl == null) return false;
            Vector3 pos;
            string why;
            if (!CompanionController.FindSafePoint(pl, ProfileOf(r), out pos, out why))
            {
                Say(NameOf(r) + " separated but waits out of sight: " + why, 3f, false);
                return false;
            }
            Vector3 face = pl.transform.position - pos;
            _body.Teleport(pos, face.sqrMagnitude > 0.01f ? MathUtil.YawOf(new Vector3(face.x, 0f, face.z)) : 0f);
            _body.SetHidden(false);
            _body.SetDissolve(0f);
            _body.WaitPoint = pos;
            _body.Mode = CompanionMode.Wait;
            _body.PlayAction(CreatureAction.Howl, 1.2f, 0.35f, 0.7f, null);
            if (Game.Fx != null) Game.Fx.CrystalBurst(pos + Vector3.up * 0.5f, ProfileOf(r).Accent, 16, 0.6f);
            _riding = false;
            return true;
        }

        /// <summary>After a separation the body stands beside the chassis for a few seconds; it hops on when the player rides off.</summary>
        private bool InSeparationGrace(PlayerController pl, float dt)
        {
            if (_separationGrace <= 0f || _body == null || pl == null) return false;
            _separationGrace -= dt;
            Vector3 vp = pl.InVehicle && pl.CurrentVehicle != null ? pl.CurrentVehicle.transform.position : pl.transform.position;
            if (!pl.InVehicle || (vp - _body.transform.position).magnitude > 9f) _separationGrace = 0f;
            return _separationGrace > 0f;
        }
    }
}
