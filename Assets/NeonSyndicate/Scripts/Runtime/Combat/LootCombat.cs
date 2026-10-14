using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Shared loot gun effects that reach beyond one bullet: finding enemies around a point, element status effects
    /// (Volt arcs, Ember burning through CharacterHealth, Frost poise breaks and staggers, Void healing), chain
    /// lightning, explosive-round splash, the empty nova and the blast extras of loot launchers. Only foes are touched
    /// (enemies hostile to the player, and creatures), so a stray arc never becomes a crime.
    /// </summary>
    public static class LootCombat
    {
        private static float _splashFxAt;
        private static float _arcFxAt;
        private static readonly List<CharacterBase> _skip = new List<CharacterBase>();
        private static readonly List<CharacterBase> _near = new List<CharacterBase>();
        private static readonly List<CharacterBase> _work = new List<CharacterBase>();

        public static void ResetStatics()
        {
            _splashFxAt = 0f;
            _arcFxAt = 0f;
            _skip.Clear();
            _near.Clear();
            _work.Clear();
        }

        /// <summary>True when c is a living enemy of the shooter (the player's foes: hostile people and creatures).</summary>
        public static bool IsFoe(CharacterBase c, GameObject shooter)
        {
            if (c == null || c.IsDead) return false;
            if (shooter != null && (c.gameObject == shooter || c.transform.IsChildOf(shooter.transform))) return false;
            bool byPlayer = Game.Player != null && shooter == Game.Player.gameObject;
            if (!byPlayer) return c.IsPlayer;
            NpcController n = c as NpcController;
            if (n != null) return n.IsHostileToPlayer;
            Creature cr = c as Creature;
            return cr != null && cr.Targetable;
        }

        private static float BodyRadius(CharacterBase c)
        {
            Creature cr = c as Creature;
            return cr != null ? cr.Radius : 0.4f;
        }

        /// <summary>Foes whose body is within radius of pos (closest first is not guaranteed). The list is reused.</summary>
        public static List<CharacterBase> FoesAround(Vector3 pos, float radius, GameObject shooter)
        {
            _near.Clear();
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (!IsFoe(n, shooter)) continue;
                float r = radius + BodyRadius(n);
                if ((n.ChestPosition - pos).sqrMagnitude <= r * r) _near.Add(n);
            }
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature c = Creature.All[i];
                if (!IsFoe(c, shooter)) continue;
                float r = radius + BodyRadius(c);
                if ((c.ChestPosition - pos).sqrMagnitude <= r * r) _near.Add(c);
            }
            return _near;
        }

        /// <summary>The nearest foe to pos within radius that is not excluded (and not in skip), or null.</summary>
        public static CharacterBase NearestFoe(Vector3 pos, float radius, GameObject shooter, CharacterBase exclude, List<CharacterBase> skip)
        {
            CharacterBase best = null;
            float bd = float.MaxValue;
            List<CharacterBase> foes = FoesAround(pos, radius, shooter);
            for (int i = 0; i < foes.Count; i++)
            {
                CharacterBase c = foes[i];
                if (c == exclude || (skip != null && skip.Contains(c))) continue;
                float d = (c.ChestPosition - pos).sqrMagnitude;
                if (d < bd)
                {
                    bd = d;
                    best = c;
                }
            }
            return best;
        }

        /// <summary>The element's status effect on a struck foe. baseDamage scales the Volt arc and the Void heal.</summary>
        public static void ApplyElement(CharacterBase target, Element e, float power, float baseDamage, GameObject shooter)
        {
            if (target == null || target.IsDead || e == Element.None) return;
            Vector3 at = target.ChestPosition;
            Fx fx = Game.Fx;
            if (fx != null && Time.time - _arcFxAt > 0.05f)
            {
                _arcFxAt = Time.time;
                fx.Flash(at, GearSystem.ElementColor(e), 2.2f, 4f, 0.15f);
            }
            switch (e)
            {
                case Element.Volt:
                {
                    if (fx != null) fx.Sparks(at, Vector3.up * 2f, 8);
                    CharacterBase other = NearestFoe(at, 8f, shooter, target, null);
                    if (other != null)
                    {
                        if (fx != null) fx.Tracer(at, other.ChestPosition);
                        DamageInfo info = DamageInfo.Make(Mathf.Max(4f, baseDamage * 0.5f * power), other.ChestPosition, other.ChestPosition - at, shooter, DamageType.Power);
                        info.Poise = 15f;
                        other.Health.TakeDamage(info);
                    }
                    break;
                }
                case Element.Ember:
                    target.Health.Burning = Mathf.Max(target.Health.Burning, 2f + 2f * power);
                    if (fx != null) fx.FirePuff(at, 0.6f);
                    break;
                case Element.Frost:
                {
                    if (fx != null) fx.SnowBurst(at, 0.6f, 6);
                    NpcController n = target as NpcController;
                    if (n != null && !n.IsCreature)
                    {
                        n.Poise -= n.MaxPoise * 0.3f * power;
                        if (n.Poise <= 0f)
                        {
                            // A zero-damage poise hit lets the NPC's own rules stagger it.
                            DamageInfo st = DamageInfo.Make(0f, at, at - (shooter != null ? shooter.transform.position : at), shooter, DamageType.Melee);
                            st.Poise = 1f;
                            n.Health.TakeDamage(st);
                        }
                        break;
                    }
                    Creature cr = target as Creature;
                    if (cr != null) cr.DrainPoise(cr.MaxPoise * 0.3f * power);
                    else if (n != null) n.Poise -= n.MaxPoise * 0.3f * power;
                    break;
                }
                case Element.Void:
                    if (fx != null) fx.ShadowBurst(at, GearSystem.ElementColor(Element.Void), 6, 0.5f);
                    if (Game.Player != null && shooter == Game.Player.gameObject) Game.Player.Health.Heal(3f + baseDamage * 0.08f * power);
                    break;
            }
        }

        /// <summary>A bullet's element against a creature's weakness or resistance (bullets carry no element of their own).</summary>
        public static float ElementVsTarget(CharacterBase target, Element e)
        {
            if (e == Element.None || target == null) return 1f;
            CreatureSpecies sp = null;
            Creature c = target as Creature;
            if (c != null) sp = c.Species;
            NpcController n = target as NpcController;
            if (n != null) sp = n.CreatureSpecies;
            if (sp == null) return 1f;
            return CreatureBrainLogic.ElementMultiplier(e, sp.Weakness, sp.Resists);
        }

        /// <summary>Lightning that jumps from a struck foe (or a point) to up to jumps more foes.</summary>
        public static void Chain(CharacterBase first, Vector3 from, float damage, int jumps, float radius, GameObject shooter)
        {
            _skip.Clear();
            if (first != null) _skip.Add(first);
            Vector3 cur = first != null ? first.ChestPosition : from;
            for (int j = 0; j < jumps; j++)
            {
                CharacterBase next = NearestFoe(cur, radius, shooter, first, _skip);
                if (next == null) break;
                _skip.Add(next);
                Vector3 to = next.ChestPosition;
                if (Game.Fx != null)
                {
                    Game.Fx.Tracer(cur, to);
                    Game.Fx.Sparks(to, Vector3.up * 2f, 6);
                }
                DamageInfo info = DamageInfo.Make(damage, to, to - cur, shooter, DamageType.Power);
                info.Poise = 10f;
                next.Health.TakeDamage(info);
                cur = to;
            }
            _skip.Clear();
        }

        /// <summary>Explosive rounds: a small burst at the impact that hurts foes nearby (not the one already hit).</summary>
        public static void Splash(Vector3 pos, float radius, float damage, GameObject shooter, CharacterBase exclude)
        {
            if (Time.time - _splashFxAt > 0.07f)
            {
                _splashFxAt = Time.time;
                if (Game.Fx != null)
                {
                    Game.Fx.Flash(pos, new Color(1f, 0.6f, 0.25f), 2.5f, 4f, 0.12f);
                    Game.Fx.Sparks(pos, Vector3.up * 3f, 6);
                    Game.Fx.Smoke(pos, Vector3.up * 0.6f, 0.5f, 0.6f);
                }
                if (Game.Audio != null) Game.Audio.PlayVar(SfxId.ExplosionSmall, pos, 0.35f, 0.15f, 60f);
            }
            // Work on a copy: a hit can kill, and death handlers may search for foes again.
            _work.Clear();
            _work.AddRange(FoesAround(pos, radius, shooter));
            for (int i = _work.Count - 1; i >= 0; i--)
            {
                CharacterBase c = _work[i];
                if (c == exclude) continue;
                float fall = Mathf.Clamp01(1f - (c.ChestPosition - pos).magnitude / (radius + BodyRadius(c)));
                DamageInfo info = DamageInfo.Make(damage * (0.4f + 0.6f * fall), c.ChestPosition, c.ChestPosition - pos, shooter, DamageType.Bullet);
                info.Force = 3f;
                c.Health.TakeDamage(info);
            }
        }

        /// <summary>The empty nova: a ring of force around the shooter that knocks foes down and carries the gun's element.</summary>
        public static void Nova(Vector3 center, float radius, float damage, Element e, float power, GameObject shooter)
        {
            Color col = e != Element.None ? GearSystem.ElementColor(e) : new Color(1f, 0.85f, 0.5f);
            if (Game.Fx != null)
            {
                Game.Fx.Flash(center + Vector3.up, col, 6f, radius * 2f, 0.35f);
                for (int i = 0; i < 12; i++)
                {
                    float a = i / 12f * Mathf.PI * 2f;
                    Vector3 p = center + new Vector3(Mathf.Cos(a), 0.3f, Mathf.Sin(a)) * radius * 0.7f;
                    Game.Fx.Sparks(p, new Vector3(Mathf.Cos(a), 0.5f, Mathf.Sin(a)) * 4f, 3);
                }
            }
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Power, center, 0.8f, 0.8f, 60f);
            _work.Clear();
            _work.AddRange(FoesAround(center + Vector3.up, radius, shooter));
            for (int i = _work.Count - 1; i >= 0; i--)
            {
                CharacterBase c = _work[i];
                float fall = Mathf.Clamp01(1f - (c.ChestPosition - center).magnitude / (radius + BodyRadius(c)));
                DamageInfo info = DamageInfo.Make(damage * (0.5f + 0.5f * fall), c.ChestPosition, c.ChestPosition - center, shooter, DamageType.Explosion);
                info.Force = 8f;
                c.Health.TakeDamage(info);
                if (e != Element.None && e != Element.Volt) ApplyElement(c, e, power, damage, shooter);
            }
        }

        /// <summary>
        /// Extras after a loot launcher's blast: the element on everyone inside it, chain lightning to foes just outside,
        /// and life steal or a magazine refill from the direct hit.
        /// </summary>
        public static void OnBlast(Vector3 pos, WeaponDef d, GameObject owner, CharacterBase direct, bool directWasAlive)
        {
            if (d == null || !d.IsLoot) return;
            if (d.Element != Element.None)
            {
                if (d.Element == Element.Ember) FireArea.Spawn(pos, Mathf.Max(1.5f, d.BlastRadius * 0.4f), 4f, d.Damage * 0.12f, owner);
                _work.Clear();
                _work.AddRange(FoesAround(pos, d.BlastRadius, owner));
                for (int i = 0; i < _work.Count; i++) if (Random.value < Mathf.Max(0.35f, d.ElementChance)) ApplyElement(_work[i], d.Element, d.ElementPower, d.Damage * 0.3f, owner);
                _work.Clear();
            }
            if (d.HasTrait(LootTrait.ChainLightning)) Chain(null, pos, d.Damage * 0.35f, 2, d.BlastRadius + 8f, owner);
            if (direct != null && directWasAlive && owner != null)
            {
                if (d.HasTrait(LootTrait.LifeSteal) && Game.Player != null && owner == Game.Player.gameObject) Game.Player.Health.Heal(d.Damage * 0.08f);
                if (direct.IsDead && d.HasTrait(LootTrait.RefillOnKill))
                {
                    WeaponController wc = owner.GetComponent<WeaponController>();
                    if (wc != null) wc.LootRefill(d);
                }
            }
        }
    }
}
