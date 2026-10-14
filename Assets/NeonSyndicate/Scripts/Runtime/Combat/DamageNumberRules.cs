using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What a floating damage number shows (its colour and size).</summary>
    public enum DamageNumberKind
    {
        /// <summary>A plain hit on health: white.</summary>
        Normal = 0,
        /// <summary>A critical, weak point or headshot: yellow and bigger.</summary>
        Crit = 1,
        /// <summary>Damage soaked by armour plates: blue.</summary>
        Armor = 2,
        /// <summary>An elemental or crystal-power hit: the element's colour.</summary>
        Element = 3,
        /// <summary>A Resonance Burst: big and gold.</summary>
        Burst = 4
    }

    /// <summary>One floating number as the book keeps it.</summary>
    public struct DamageNumberEntry
    {
        public int Target;
        public DamageNumberKind Kind;
        public Element Element;
        public float Amount;
        /// <summary>Seconds since it appeared (refreshed a little while hits keep merging into it).</summary>
        public float Age;
        public float Life;
        /// <summary>Hits merged into it.</summary>
        public int Hits;
        /// <summary>Small sideways offset so numbers on one target do not sit on top of each other (-1 to 1).</summary>
        public float Lane;
        /// <summary>Where it appeared (above the target's head).</summary>
        public Vector3 World;
    }

    /// <summary>
    /// The floating damage numbers as plain data (tested offline): fast guns would bury the screen, so a hit on the same
    /// target with the same kind within MergeWindow seconds adds to the number already there instead of making a new
    /// one. Numbers rise and fade over their life; the oldest go when there are too many.
    /// </summary>
    public sealed class DamageNumberBook
    {
        public const float MergeWindow = 0.35f;
        public const int MaxEntries = 28;

        private readonly List<DamageNumberEntry> _list = new List<DamageNumberEntry>();
        private int _lane;

        public int Count { get { return _list.Count; } }
        public DamageNumberEntry this[int i] { get { return _list[i]; } }

        /// <summary>Seconds a number of this kind stays up.</summary>
        public static float LifeOf(DamageNumberKind k)
        {
            switch (k)
            {
                case DamageNumberKind.Burst: return 1.5f;
                case DamageNumberKind.Crit: return 1.1f;
                default: return 0.9f;
            }
        }

        /// <summary>Text size of a kind (UIKit points at 1080p); big hits grow a little.</summary>
        public static int SizeOf(DamageNumberKind k, float amount)
        {
            int b;
            switch (k)
            {
                case DamageNumberKind.Burst: b = 40; break;
                case DamageNumberKind.Crit: b = 30; break;
                case DamageNumberKind.Armor: b = 20; break;
                default: b = 22; break;
            }
            return b + Mathf.Clamp(Mathf.RoundToInt(Mathf.Log10(Mathf.Max(1f, amount)) * 2f), 0, 6);
        }

        /// <summary>Adds a hit: merges into a young number of the same target and kind, else starts a new one.</summary>
        public void Add(int target, DamageNumberKind kind, Element element, float amount, Vector3 world)
        {
            if (amount < 0.5f) return;
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                DamageNumberEntry e = _list[i];
                if (e.Target != target || e.Kind != kind || e.Element != element || e.Age > MergeWindow) continue;
                e.Amount += amount;
                e.Hits++;
                e.Age = Mathf.Min(e.Age, 0.12f);
                _list[i] = e;
                return;
            }
            _lane = (_lane + 1) % 5;
            _list.Add(new DamageNumberEntry { Target = target, Kind = kind, Element = element, Amount = amount, Age = 0f, Life = LifeOf(kind), Hits = 1, Lane = (_lane - 2) * 0.5f, World = world });
            while (_list.Count > MaxEntries) _list.RemoveAt(0);
        }

        /// <summary>Ages every number by dt and drops the finished ones.</summary>
        public void Tick(float dt)
        {
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                DamageNumberEntry e = _list[i];
                e.Age += dt;
                if (e.Age >= e.Life) _list.RemoveAt(i);
                else _list[i] = e;
            }
        }

        /// <summary>0 to 1 opacity of a number at its age (pops in, holds, fades out).</summary>
        public static float Alpha(DamageNumberEntry e)
        {
            float left = e.Life - e.Age;
            return Mathf.Clamp01(Mathf.Min(e.Age * 12f, left * 3f));
        }

        /// <summary>Metres a number has risen above where it appeared.</summary>
        public static float Rise(DamageNumberEntry e)
        {
            return e.Age * (e.Kind == DamageNumberKind.Burst ? 0.6f : 0.9f);
        }

        public void Clear()
        {
            _list.Clear();
            _lane = 0;
        }
    }
}
