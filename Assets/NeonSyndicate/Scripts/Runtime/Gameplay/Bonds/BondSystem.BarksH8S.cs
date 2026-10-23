using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S companion voice and judgement, inside the existing bond system: short subtitle barks for the key moments
    /// (an assist going off, a marked target, low health, downed, back on its feet, regrouping, a find), rate limited by
    /// CompanionBarkGateH8S and shown on the companion card's message lines; a hurt companion keeping the player between
    /// itself and the nearest threat; one regroup bark when a companion that fell far behind catches up; a single
    /// reminder when a waiting companion is left far away; and the player's current target (the foe they just hit)
    /// for U in a fight with no lock or aim. Nothing here deals damage, spends anything or changes an assist's numbers:
    /// assists keep their one owner (BondSystem.Assist) and the shared damage pipeline.
    /// </summary>
    public sealed partial class BondSystem
    {
        private readonly CompanionBarkGateH8S _barks = new CompanionBarkGateH8S();
        private RegroupTrackerH8S _regroup;
        private int _barkUid;
        private float _behaviourTickH8S;
        private bool _waitRemindedH8S;
        /// <summary>The combat assist was cooling down at the last tick (a ready cue plays on the change to ready).</summary>
        private bool _combatCoolingH8S;

        /// <summary>
        /// Voices a key moment as a subtitle bark on the card (when barks and subtitles are on and the rate limit
        /// allows). The companion's own sound for the moment already plays from the existing cue.
        /// </summary>
        private void Bark(CompanionBarkH8S kind)
        {
            CompanionRecord r = Active;
            if (r == null || !Settings.CompanionBarksH8S || !Settings.Subtitles) return;
            if (_barkUid != r.Uid)
            {
                _barks.Reset();
                _barkUid = r.Uid;
            }
            int variant;
            if (!_barks.TryFire(kind, Time.unscaledTime, out variant)) return;
            string line = CompanionBarkLinesH8S.Line(r.Archetype, kind, variant);
            if (line.Length == 0) return;
            AddBark(NameOf(r), line, ProfileOf(r).Accent);
        }

        /// <summary>One bark line on the card: replaces any earlier bark, keeps at most two lines in all.</summary>
        private void AddBark(string speaker, string line, Color accent)
        {
            for (int i = _messages.Count - 1; i >= 0; i--) if (_messages[i].Bark) _messages.RemoveAt(i);
            string text = "<color=#" + ColorUtility.ToHtmlStringRGB(accent) + "><b>" + speaker + ":</b></color> <i>" + line + "</i>";
            _messages.Add(new Message { Text = text, Left = 3.4f, Warn = false, Bark = true });
            while (_messages.Count > 2) _messages.RemoveAt(0);
        }

        /// <summary>Wears health and voices the drop below the low threshold (one bark per crossing).</summary>
        private void NoteHealthDropH8S(float before01, float after01)
        {
            if (CompanionSpacingRulesH8S.CrossedLow(before01, after01)) Bark(CompanionBarkH8S.LowHealth);
        }

        /// <summary>
        /// Four times a second: shelter while hurt in a fight (the body keeps the player between itself and the
        /// nearest hostile), the regroup bark after falling far behind, and one reminder when a waiting companion is
        /// left far away.
        /// </summary>
        private void TickBehaviourH8S(float dt)
        {
            _behaviourTickH8S -= dt;
            if (_behaviourTickH8S > 0f) return;
            _behaviourTickH8S = 0.25f;
            CompanionRecord r = Active;
            PlayerController pl = Game.Player;
            if (r == null || pl == null || _body == null || _body.Hidden || pl.InVehicle)
            {
                if (_body != null) _body.ShelterH8S = false;
                _regroup.Reset();
                _combatCoolingH8S = false;
                return;
            }
            Vector3 pp = pl.transform.position;
            bool following = r.State == (int)CompanionState.Following;
            // Ready cue: in a fight, the moment the combat assist comes off cooldown the body glows and chimes softly.
            bool cooling = CombatCooldownLeft(r.Uid) > 0f;
            if (_combatCoolingH8S && !cooling && CombatDirector.InCombat && BondRules.CanCombatAssist(r.Rank) && r.State != (int)CompanionState.Downed)
            {
                _body.Glow(ReducedEffectsH8S ? 0.6f : 1.2f);
                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.CrystalChime, _body.HeadPosition, 0.12f, r.Archetype == BondIds.AshWarden ? 0.5f : (r.Archetype == BondIds.StormSkimmer ? 1.1f : 0.85f), 14f);
            }
            _combatCoolingH8S = cooling;
            Vector3 threat = Vector3.zero;
            bool shelter = following && CompanionSpacingRulesH8S.ShouldShelter(r.Health01, CombatDirector.InCombat) && NearestThreatH8S(pp, out threat);
            _body.ShelterH8S = shelter;
            if (shelter) _body.ShelterFromH8S = threat;
            float d = MathUtil.FlatDistance(pp, _body.transform.position);
            if (following && !_riding)
            {
                if (_regroup.Update(d, ProfileOf(r).FollowDistance + 2.5f)) Bark(CompanionBarkH8S.Regrouping);
            }
            else _regroup.Reset();
            if (r.State == (int)CompanionState.Waiting)
            {
                if (!_waitRemindedH8S && d > CompanionSpacingRulesH8S.WaitReminderDistance)
                {
                    _waitRemindedH8S = true;
                    Say(NameOf(r) + " is still waiting where you left it. Tap N to call it on.", 5f, false);
                }
            }
            else _waitRemindedH8S = false;
        }

        /// <summary>The nearest living hostile within 22 m of the player (the shared foe list, no allocation).</summary>
        private static bool NearestThreatH8S(Vector3 pp, out Vector3 at)
        {
            at = Vector3.zero;
            PlayerController pl = Game.Player;
            if (pl == null) return false;
            List<CharacterBase> foes = LootCombat.FoesAround(pp, 22f, pl.gameObject);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < foes.Count; i++)
            {
                CharacterBase c = foes[i];
                if (!IsHostileTarget(c)) continue;
                float d = (c.transform.position - pp).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                at = c.transform.position;
                found = true;
            }
            return found;
        }

        /// <summary>
        /// The player's current target when there is no lock or aim: the hostile the player hit most recently (within
        /// CompanionTargetRulesH8S.EngagedWindow seconds) inside assist range, the closer one on a tie. Null when none.
        /// </summary>
        private static CharacterBase EngagedTargetH8S(PlayerController pl)
        {
            if (pl == null) return null;
            Vector3 pp = pl.transform.position;
            List<CharacterBase> foes = LootCombat.FoesAround(pp, BondRules.AssistRange, pl.gameObject);
            CharacterBase best = null;
            float bestHit = 0f, bestDist = 0f;
            float now = Time.time;
            for (int i = 0; i < foes.Count; i++)
            {
                CharacterBase c = foes[i];
                if (!IsHostileTarget(c) || c.Health == null || c.Health.LastAttacker != pl.gameObject) continue;
                float hit = c.Health.LastDamageTime;
                float dist = Vector3.Distance(pp, c.transform.position);
                if (!CompanionTargetRulesH8S.Engaged(hit, now, dist, BondRules.AssistRange)) continue;
                if (best != null && !CompanionTargetRulesH8S.Better(hit, dist, bestHit, bestDist)) continue;
                best = c;
                bestHit = hit;
                bestDist = dist;
            }
            return best;
        }

        /// <summary>Clears the H8S runtime state with the rest of the bond runtime (new game, load, reset).</summary>
        private void ClearBehaviourH8S()
        {
            _barks.Reset();
            _regroup.Reset();
            _barkUid = 0;
            _behaviourTickH8S = 0f;
            _waitRemindedH8S = false;
            _combatCoolingH8S = false;
            ClearCardH8S();
        }
    }
}
