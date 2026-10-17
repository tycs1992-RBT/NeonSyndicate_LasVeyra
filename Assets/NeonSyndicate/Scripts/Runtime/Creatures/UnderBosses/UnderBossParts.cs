using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Marks one hit collider of an Underheart boss with the part it is (the fight reads it to scale the hit).</summary>
    public sealed class UBossPartTag : MonoBehaviour
    {
        public UBossPart Part;
        /// <summary>A per-boss index (which seam, which leg) for parts that matter individually.</summary>
        public int Index;
        public Collider Collider;
    }

    /// <summary>
    /// A small shootable fight object that is not the boss: the Matron's extraction lures and the Regent's furnace vents.
    /// Weapons, crystal shots and explosions find it through IDamageable; only hits the player's side dealt count (the
    /// player's own, a companion's or a fused vehicle's, all credited to the player). When its health runs out it calls
    /// Broken once. It can also be switched off by hand (an Interactable next to it calls Break).
    /// </summary>
    public sealed class UBossTarget : MonoBehaviour, IDamageable
    {
        public float MaxHealth = 150f;
        public float Health = 150f;
        public bool Armed;
        public string Label = "";
        public Action<UBossTarget> Broken;
        public Action<UBossTarget, float> Hit;

        public bool IsAlive { get { return Armed && Health > 0f; } }

        public void Arm(float health)
        {
            MaxHealth = Mathf.Max(1f, health);
            Health = MaxHealth;
            Armed = true;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive || !(info.Amount > 0f)) return;
            if (!info.FromPlayer) return;
            Health = Mathf.Max(0f, Health - info.Amount);
            if (Hit != null)
            {
                try { Hit(this, info.Amount); }
                catch (Exception e) { Log.Exception("Underheart boss target hit", e); }
            }
            if (Health <= 0f) Break();
        }

        /// <summary>Breaks the target now (shot down or shut off by hand). Calls Broken once.</summary>
        public void Break()
        {
            if (!Armed) return;
            Armed = false;
            Health = 0f;
            if (Broken == null) return;
            try { Broken(this); }
            catch (Exception e) { Log.Exception("Underheart boss target broken", e); }
        }
    }

    /// <summary>
    /// The announced, finite reinforcements of one fight or mechanism: every body spawns through the creature director
    /// with the owner's tag (hostile, persistent), counts against the declared AddLedger04E budget, and is counted dead
    /// only on a confirmed death (a body removed by anything else is simply gone, never a death). Dismiss removes the
    /// living ones with a puff when the owner ends.
    /// </summary>
    public sealed class UBossAddSquad
    {
        public readonly string Tag;
        public readonly AddLedger04E Ledger;
        private readonly List<CharacterBase> _bodies = new List<CharacterBase>();
        private readonly List<bool> _counted = new List<bool>();

        public UBossAddSquad(string tag, AddLedger04E ledger)
        {
            Tag = tag ?? "uboss04e_adds";
            Ledger = ledger;
        }

        /// <summary>Living bodies right now.</summary>
        public int Living
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _bodies.Count; i++) if (_bodies[i] != null && !_bodies[i].IsDead) n++;
                return n;
            }
        }

        public IList<CharacterBase> Bodies { get { return _bodies; } }

        /// <summary>Spawns one body if the ledger allows it; null when refused or the spawn failed.</summary>
        public CharacterBase Spawn(string species, Vector3 at, float yaw, float healthScale, float damageScale)
        {
            if (Ledger == null || Ledger.Allow(1) < 1 || Game.Creatures == null) return null;
            CreatureSpawnOptions o = new CreatureSpawnOptions();
            o.Tag = Tag;
            o.Hostile = true;
            o.Persistent = true;
            o.HealthScale = healthScale;
            o.DamageScale = damageScale;
            CharacterBase b = null;
            try { b = Game.Creatures.Spawn(species, at, yaw, o); }
            catch (Exception e) { Log.Exception("Underheart boss reinforcement " + species, e); }
            if (b == null) return null;
            Ledger.NoteSpawned(1);
            _bodies.Add(b);
            _counted.Add(false);
            return b;
        }

        /// <summary>Counts confirmed deaths into the ledger and forgets bodies that are gone.</summary>
        public void Tick()
        {
            for (int i = _bodies.Count - 1; i >= 0; i--)
            {
                CharacterBase b = _bodies[i];
                if (b != null && b.IsDead && !_counted[i])
                {
                    _counted[i] = true;
                    if (Ledger != null) Ledger.NoteDead(1);
                }
                if (b == null)
                {
                    _bodies.RemoveAt(i);
                    _counted.RemoveAt(i);
                }
            }
        }

        /// <summary>Living bodies within r (flat) of p and at about its height.</summary>
        public int CountNear(Vector3 p, float r)
        {
            int n = 0;
            float r2 = r * r;
            for (int i = 0; i < _bodies.Count; i++)
            {
                CharacterBase b = _bodies[i];
                if (b == null || b.IsDead) continue;
                Vector3 q = b.transform.position;
                if (Mathf.Abs(q.y - p.y) > 4f) continue;
                if (MathUtil.FlatSqrDistance(q, p) <= r2) n++;
            }
            return n;
        }

        /// <summary>Removes every living body of the squad (with a puff unless the session is resetting).</summary>
        public int Dismiss(bool puff)
        {
            int n = 0;
            for (int i = 0; i < _bodies.Count; i++)
            {
                CharacterBase b = _bodies[i];
                if (b == null || b.IsDead) continue;
                n++;
                if (puff && Game.Fx != null) Game.Fx.RockBurst(b.ChestPosition, new Color(1f, 0.55f, 0.2f), 8, 0.8f);
            }
            if (Game.Creatures != null)
            {
                try { Game.Creatures.DespawnTag(Tag); }
                catch (Exception e) { Log.Exception("Underheart boss reinforcements dismiss", e); }
            }
            _bodies.Clear();
            _counted.Clear();
            return n;
        }

        /// <summary>A spot for a reinforcement near want: on a floor about the reference height, standing room, a clear line back to from.</summary>
        public static bool Spot(Vector3 want, Vector3 from, float referenceY, out Vector3 spot)
        {
            for (int k = 0; k < 8; k++)
            {
                Vector3 p = want;
                if (k > 0)
                {
                    float a = k * 0.785f;
                    p += new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (1.5f + k * 0.4f);
                }
                Vector3 s;
                if (!UnderBossFx.StandSpot(p, referenceY, 3f, out s)) continue;
                if (Physics.Linecast(from + Vector3.up * 1.2f, s + Vector3.up * 1.2f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                spot = s;
                return true;
            }
            spot = want;
            return false;
        }
    }
}
