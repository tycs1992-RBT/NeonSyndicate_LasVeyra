using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// A pack or a swarm sharing one mind: when one member notices the player the whole group knows, pack members take
    /// flanking slots around the target while one attacks, a swarm flies as a flock around a shared point and holds a
    /// single attack token between all its mites, and losing the leader shakes the rest (they break and run sooner).
    /// </summary>
    public sealed class CreatureGroup
    {
        public readonly List<Creature> Members = new List<Creature>();
        public string SpeciesId;
        public Vector3 Home;
        public CharacterBase Target;
        /// <summary>The group is on the hunt (cleared again when its members have all calmed down).</summary>
        public bool Alerted;
        /// <summary>The seed of the shared swarm body (mites spawned later into this swarm wear the same one).</summary>
        public int BodySeed;
        public Creature Leader;
        public bool LeaderLost;
        public bool Swarm;
        /// <summary>Flock centre and mean velocity (updated once per frame).</summary>
        public Vector3 Center;
        public Vector3 Velocity;
        /// <summary>Member positions for flocking (valid for Count entries after Tick).</summary>
        public Vector3[] Positions = new Vector3[8];
        public int Count;
        public float OrbitPhase;
        /// <summary>Time of the swarm's next biting wave at the target.</summary>
        public float NextSwoop;
        /// <summary>End of the current biting wave (the swarm holds the attack token until then).</summary>
        public float WaveEnd;

        /// <summary>True while a biting wave is on.</summary>
        public bool WaveActive { get { return Time.time < WaveEnd; } }
        private int _frame = -1;
        private int _startCount;
        private bool _alerting;

        public void Add(Creature c)
        {
            if (c == null || Members.Contains(c)) return;
            Members.Add(c);
            c.Group = this;
            if (Leader == null) Leader = c;
            _startCount = Mathf.Max(_startCount, Members.Count);
        }

        public void Remove(Creature c)
        {
            Members.Remove(c);
            if (Leader == c) Leader = FirstAlive();
        }

        /// <summary>Living members right now.</summary>
        public int Alive
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Members.Count; i++) if (Members[i] != null && !Members[i].IsDead) n++;
                return n;
            }
        }

        /// <summary>Fraction of the group still standing.</summary>
        public float Strength { get { return _startCount > 0 ? Alive / (float)_startCount : 0f; } }

        public Creature FirstAlive()
        {
            for (int i = 0; i < Members.Count; i++) if (Members[i] != null && !Members[i].IsDead && Members[i].Ready) return Members[i];
            return null;
        }

        /// <summary>
        /// Everyone in the group turns on target, every time (members already on it ignore the call). A member that joins
        /// the hunt calls this again; the guard stops that call from going round the group a second time.
        /// </summary>
        public void Alert(CharacterBase target, Creature by)
        {
            if (target == null || _alerting) return;
            Target = target;
            Alerted = true;
            _alerting = true;
            try
            {
                for (int i = 0; i < Members.Count; i++)
                {
                    Creature m = Members[i];
                    if (m == null || m == by || m.IsDead) continue;
                    m.JoinHunt(target);
                }
            }
            finally
            {
                _alerting = false;
            }
        }

        /// <summary>A member went back to its calm life: once none is hunting, the group forgets its target.</summary>
        public void MemberCalmed()
        {
            for (int i = 0; i < Members.Count; i++)
            {
                Creature m = Members[i];
                if (m != null && !m.IsDead && m.Mode == CreatureMode.Hunt) return;
            }
            Alerted = false;
            Target = null;
        }

        /// <summary>The member that runs the swarm's biting waves: the first living one that is hunting.</summary>
        public Creature WaveLeader()
        {
            for (int i = 0; i < Members.Count; i++)
            {
                Creature m = Members[i];
                if (m != null && !m.IsDead && m.Ready && m.Mode == CreatureMode.Hunt) return m;
            }
            return null;
        }

        public void OnMemberDied(Creature c)
        {
            if (c == Leader)
            {
                LeaderLost = true;
                Leader = FirstAlive();
                if (Leader != null && !Swarm && Game.Hud != null && c.Target != null && c.Target.IsPlayer) Game.Hud.CombatText("The pack falters", new Color(1f, 0.85f, 0.5f));
            }
        }

        /// <summary>Recomputes the flock centre once per frame (any member may call it).</summary>
        public void Tick()
        {
            int f = Time.frameCount;
            if (f == _frame) return;
            _frame = f;
            if (Positions.Length < Members.Count) Positions = new Vector3[Members.Count + 4];
            Count = 0;
            Vector3 sum = Vector3.zero, vel = Vector3.zero;
            for (int i = 0; i < Members.Count; i++)
            {
                Creature m = Members[i];
                if (m == null || m.IsDead || !m.Ready) continue;
                Vector3 p = m.transform.position;
                Positions[Count++] = p;
                sum += p;
                vel += m.Velocity;
            }
            if (Count > 0)
            {
                Center = sum / Count;
                Velocity = vel / Count;
            }
            OrbitPhase += Time.deltaTime * 1.7f;
        }

        /// <summary>How many members are in the middle of an attack right now.</summary>
        public int Biting()
        {
            int n = 0;
            for (int i = 0; i < Members.Count; i++) if (Members[i] != null && !Members[i].IsDead && Members[i].AttackPhase >= 0) n++;
            return n;
        }

        /// <summary>A pack member's flanking slot around the target (angle in degrees from the target's facing).</summary>
        public float SlotAngle(Creature c)
        {
            int idx = Members.IndexOf(c);
            if (idx < 0) idx = 0;
            int n = Mathf.Max(1, Members.Count);
            return (idx + 0.5f) / n * 360f + 30f;
        }

        /// <summary>The member that holds the swarm's single attack token (the first living one).</summary>
        public Creature TokenHolder { get { return Swarm ? FirstAlive() : null; } }
    }
}
