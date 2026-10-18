using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>An event's explicit combat membership. Bystanders and the player's bonded creatures are never members.</summary>
    public sealed class NpcCityActor04F : MonoBehaviour
    {
        public EventInstance Event;
        public NpcController Actor;
        public int Side;
        public Color Accent;
        public List<NpcController> Opponents;
        public float FightAt;
        public bool Retaliating;
        public bool StoodDown;
        public bool Contributed;
        public bool Finished { get { return Event == null || Event.Done; } }
        private float _reactAt;

        public bool MayDamage(CharacterBase target)
        {
            if (Finished || StoodDown || Event.Held || target == null || target.IsDead || Actor == null) return false;
            if (target.IsPlayer) return Actor.Target == target && CityCombatRules04F.OpposedEventTarget(false, Side, -1, true, Retaliating);
            NpcController other = target as NpcController;
            if (other == null) return false;
            NpcCityActor04F membership = other.CityActor04F;
            return membership != null && !membership.Finished && CityCombatRules04F.OpposedEventTarget(membership.Event == Event, Side, membership.Side, false, false);
        }

        public void Bind(EventInstance owner, NpcController actor, int side, List<NpcController> opponents, Color accent)
        {
            Event = owner;
            Actor = actor;
            Side = side;
            Opponents = opponents;
            Accent = accent;
            FightAt = owner.Age + CityCombatRules04F.SetupSeconds;
            actor.CityActor04F = this;
            actor.IgnoreThreats = true;
            actor.Health.Damaged += Damaged;
        }

        private void Damaged(DamageInfo info)
        {
            if (Event == null || Event.Done) return;
            if (Game.IsPlayer(info.Source))
            {
                // Direct attacks carry the normal crime semantics. Merely watching never sets this flag.
                Retaliating = true;
                Contributed = true;
                Event.PlayerAccepted = true;
                Actor.Target = Game.Player;
            }
        }

        private void OnDestroy()
        {
            if (Actor != null)
            {
                if (Actor.Health != null) Actor.Health.Damaged -= Damaged;
                if (Actor.CityActor04F == this) Actor.CityActor04F = null;
            }
        }

        /// <summary>Runs before role-based acquisition. Actual attacks still use the normal NPC fight and token paths.</summary>
        public bool Think()
        {
            if (Actor == null || Actor.IsDead) return true;
            if (Finished || StoodDown || Event.Held)
            {
                Actor.Target = null;
                Actor.Hostile = false;
                Actor.Weapons.CancelActions();
                CombatDirector.ReleaseMeleeToken(Actor);
                CombatDirector.ReleaseShotToken(Actor);
                Actor.GuardAt(Actor.transform.position);
                return true;
            }
            if (MathUtil.FlatDistance(Actor.transform.position, Event.Pos) > 28f)
            {
                Actor.Target = null;
                Actor.MoveTo(Event.Pos + new Vector3(Side == 0 ? -5f : 5f, 0f, 0f), true, null);
                return true;
            }
            if (Event.Age < FightAt)
            {
                Actor.Target = null;
                Actor.GuardAt(Actor.Home);
                return true;
            }
            CharacterBase target = Retaliating && Game.Player != null && !Game.Player.IsDead && MathUtil.FlatDistance(Actor.transform.position, Game.PlayerPosition) < 22f ? Game.Player : null;
            if (target == null && Opponents != null)
            {
                float distance = float.MaxValue;
                foreach (NpcController candidate in Opponents)
                {
                    if (candidate == null || candidate.IsDead || candidate.CityActor04F == null || candidate.CityActor04F.Finished) continue;
                    float d = MathUtil.FlatSqrDistance(Actor.transform.position, candidate.transform.position);
                    if (d < distance) { target = candidate; distance = d; }
                }
            }
            if (target == null) { Actor.Target = null; Actor.GuardAt(Actor.Home); return true; }
            Actor.Attack(target);
            if (Event.Age >= _reactAt)
            {
                _reactAt = Event.Age + 4f;
                foreach (NpcController resident in NpcController.All)
                {
                    if (resident == null || resident.Persistent || !string.IsNullOrEmpty(resident.MissionTag) || resident.IsDead) continue;
                    if (resident.Role != NpcRole.Civilian || MathUtil.FlatDistance(resident.transform.position, Event.Pos) > 25f) continue;
                    resident.Flee(Event.Pos, 8f);
                }
            }
            return true;
        }
    }
}
