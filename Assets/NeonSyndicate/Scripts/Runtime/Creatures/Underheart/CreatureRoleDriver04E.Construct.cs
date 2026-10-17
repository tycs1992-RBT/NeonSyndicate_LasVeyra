using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The Repair Construct's role (Support): it keeps about 8 m away, scuttles off whenever the player comes within 6 m,
    /// and mends its allies. Tell: the weld arm unfolds (0.6 s) and a cyan beam links it to the most hurt ally in sight
    /// within 14 m, whose wounds sparkle as they close (6% of its health per second, for up to 4 s, never itself). Any hit
    /// breaks the beam for 1.5 s; losing sight of the ally ends it. A snapped weld arm ends the mending for good.
    /// </summary>
    public sealed partial class CreatureRoleDriver04E
    {
        public const float ConstructKeepRange = 8f;

        private CharacterBase _mendTarget;
        private float _mendT;
        private float _beamBrokenUntil;
        private float _nextPanicAt;
        private float _armUnfold;
        private float _mendFx;
        private float _mendPulse;
        private LineRenderer _beam;
        private static Material _beamMat;

        private static readonly Color BeamColor = new Color(0.35f, 0.95f, 1f, 0.9f);

        public static void ResetConstructStatics()
        {
            _beamMat = null;
        }

        private void ReadyConstruct()
        {
            _acc = UnderheartAccessories04E.AttachConstruct(Body);
            Body.RoleKeepRange(ConstructKeepRange);
            if (_beamMat == null) _beamMat = Mats.Additive("Weld Beam", BeamColor, UIKit.Circle);
            GameObject go = new GameObject("Weld Beam");
            go.transform.SetParent(Body.transform, false);
            _beam = go.AddComponent<LineRenderer>();
            _beam.sharedMaterial = _beamMat;
            _beam.positionCount = 2;
            _beam.useWorldSpace = true;
            _beam.startWidth = 0.08f;
            _beam.endWidth = 0.05f;
            _beam.startColor = BeamColor;
            _beam.endColor = BeamColor;
            _beam.shadowCastingMode = ShadowCastingMode.Off;
            _beam.receiveShadows = false;
            _beam.enabled = false;
        }

        private void ThinkConstruct(bool engaged)
        {
            if (!HuntingPlayer && !engaged)
            {
                EndMend(false);
                return;
            }
            Vector3 pp = PlayerGround;
            float d = MathUtil.FlatDistance(pp, Body.transform.position);
            if (d < UnderheartRoles04E.SupportPanicRange && Time.time >= _nextPanicAt && !Body.RoleAttacking && HuntingPlayer)
            {
                _nextPanicAt = Time.time + 3f;
                Body.RoleRetreat(pp, Def.RetreatSeconds);
            }
            if (PartBroken(PartEffect.NoAbility))
            {
                EndMend(false);
                return;
            }
            if (_mendTarget != null)
            {
                if (!MendValid(_mendTarget) || _mendT >= Def.ActiveSeconds) EndMend(true);
                return;
            }
            if (Time.time < _beamBrokenUntil || Time.time < _nextSignatureAt) return;
            CharacterBase best = PickMendTarget();
            if (best == null) return;
            _mendTarget = best;
            _mendT = 0f;
            _mendPulse = 0f;
            Underheart04ESounds.Play(UhSfx.RepairPulse, Body.ChestPosition, 0.7f, 1f);
        }

        /// <summary>The most hurt ally in sight within range (the encounter's members, or nearby hostile creatures).</summary>
        private CharacterBase PickMendTarget()
        {
            CharacterBase best = null;
            float bestFrac = 2f;
            Vector3 from = BeamStart;
            if (Arena != null)
            {
                for (int i = 0; i < Arena.Members.Count; i++) Consider(Arena.Members[i], from, ref best, ref bestFrac);
            }
            else
            {
                for (int i = 0; i < Creature.All.Count; i++)
                {
                    Creature c = Creature.All[i];
                    if (c == null || c.Species == null || UnderheartRoles04E.IsResident(c.Species.Id) || !c.IsHostileToPlayer) continue;
                    Consider(c, from, ref best, ref bestFrac);
                }
            }
            return best;
        }

        private void Consider(CharacterBase c, Vector3 from, ref CharacterBase best, ref float bestFrac)
        {
            if (c == null || c.Health == null) return;
            float frac = c.Health.MaxHealth > 0f ? c.Health.Health / c.Health.MaxHealth : 1f;
            float dist = Vector3.Distance(c.ChestPosition, Body.ChestPosition);
            if (!UnderheartRoles04E.MayMend(c == Body, c.IsDead, frac, dist, Def.RangeMax)) return;
            if (frac >= bestFrac) return;
            if (Physics.Linecast(from, c.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return;
            best = c;
            bestFrac = frac;
        }

        private bool MendValid(CharacterBase c)
        {
            if (c == null || c.IsDead || c.Health == null) return false;
            if (c.Health.Health >= c.Health.MaxHealth * 0.99f) return false;
            if (Vector3.Distance(c.ChestPosition, Body.ChestPosition) > Def.RangeMax + 2f) return false;
            return !Physics.Linecast(BeamStart, c.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        private Vector3 BeamStart
        {
            get { return _acc != null && _acc.Tip != null ? _acc.Tip.position : Body.ChestPosition; }
        }

        private void FrameConstruct(float dt)
        {
            bool mending = _mendTarget != null && !_mendTarget.IsDead;
            _armUnfold = Mathf.MoveTowards(_armUnfold, mending ? 1f : 0f, dt / Mathf.Max(0.1f, Def.TellSeconds));
            if (_acc != null)
            {
                if (_acc.Arm != null) _acc.Arm.localRotation = QMath.AX(Mathf.Lerp(30f, -30f, _armUnfold));
                _acc.SetGlow(mending ? 1.4f + 0.6f * Mathf.Sin(Time.time * 12f) : 1f);
            }
            bool beam = mending && _armUnfold >= 0.99f;
            if (_beam != null)
            {
                if (_beam.enabled != beam) _beam.enabled = beam;
                if (beam)
                {
                    _beam.SetPosition(0, BeamStart);
                    _beam.SetPosition(1, _mendTarget.ChestPosition);
                }
            }
            if (!beam) return;
            _mendTarget.Health.Heal(UnderheartRoles04E.MendPerSecond * _mendTarget.Health.MaxHealth * dt);
            _mendT += dt;
            _mendFx -= dt;
            if (_mendFx <= 0f && Game.Fx != null)
            {
                _mendFx = 0.35f;
                Game.Fx.CrystalBurst(_mendTarget.ChestPosition, BeamColor, 5, 0.5f);
            }
            _mendPulse -= dt;
            if (_mendPulse <= 0f)
            {
                _mendPulse = 1f;
                Underheart04ESounds.Play(UhSfx.RepairPulse, Body.ChestPosition, 0.45f, 1.1f);
            }
        }

        private void EndMend(bool cooldown)
        {
            _mendTarget = null;
            if (_beam != null && _beam.enabled) _beam.enabled = false;
            if (cooldown) _nextSignatureAt = Time.time + UnderheartRoles04E.Cooldown(Def, Random.value);
        }

        private void HurtConstruct(DamageInfo info)
        {
            if (Body.Health.LastApplied <= 0f || _mendTarget == null) return;
            EndMend(true);
            _beamBrokenUntil = Time.time + Def.PunishSeconds;
            Say("Weld beam broken", BeamColor);
        }

        private void StopConstruct()
        {
            EndMend(false);
        }
    }
}
