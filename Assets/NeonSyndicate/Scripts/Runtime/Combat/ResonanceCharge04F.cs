using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>One bounded reservoir. No flight, heat, ammo, stamina or gadget state belongs here.</summary>
    public sealed class ResonanceCharge04F
    {
        public const float Capacity = 100f;
        public const float MeleeGain = 8f;
        public const float CounterGain = 14f;
        public const float VictoryGain = 12f;
        public const float HitInterval = 0.3f;
        public const float PerLifeMeleeCap = 32f;
        public const int TrackedLives = 512;
        private sealed class Life
        {
            public float Harvested;
            public float LastHit = -999f;
            public bool VictoryPaid;
        }
        private readonly HashSet<string> _cells = new HashSet<string>();
        public IEnumerable<string> CollectedCells { get { return _cells; } }
        private readonly Dictionary<int, Life> _lives = new Dictionary<int, Life>();
        public float Value { get; private set; } = Capacity;

        public static float Sanitize(float amount)
        {
            return float.IsNaN(amount) || float.IsInfinity(amount) ? Capacity : Math.Max(0f, Math.Min(Capacity, amount));
        }
        public void Restore(float amount, IEnumerable<string> collectedCells = null)
        {
            Value = Sanitize(amount); _lives.Clear(); _cells.Clear();
            if (collectedCells != null) foreach (string id in collectedCells) if (KnownCell(id)) _cells.Add(id);
        }
        public static bool KnownCell(string id) { return id == "dispatch.reserve" || id == "pressure.reserve" || id == "forge.reserve"; }
        public bool CellCollected(string id) { return _cells.Contains(id); }
        public float ClaimCell(string id)
        {
            if (!KnownCell(id) || Value >= Capacity || !_cells.Add(id)) return 0f;
            float old = Value; Value = Sanitize(Value + 30f); return Value - old;
        }
        public void CreditChain04N(float amount){if(amount>0f && !float.IsNaN(amount) && !float.IsInfinity(amount))Value=Sanitize(Value+Math.Min(6f,amount));}
        public bool CanSpend(float amount) { return amount >= 0f && !float.IsNaN(amount) && !float.IsInfinity(amount) && Value + 0.0001f >= amount; }
        public bool Spend(float amount)
        {
            if (!CanSpend(amount)) return false;
            Value = Math.Max(0f, Value - amount);
            return true;
        }
        public float Refill() { float gain = Capacity - Value; Value = Capacity; return gain; }

        /// <summary>Hostility must predate the hit. Damage must actually remove health or armour. Each life pays bounded gains once.</summary>
        public float Earn(int lifeId, float now, bool hostileBefore, bool protectedTarget, float actualDamage, bool melee, bool counter, bool killed)
        {
            if (lifeId == 0 || !hostileBefore || protectedTarget || !(actualDamage > 0f) || float.IsNaN(now) || float.IsInfinity(now)) return 0f;
            Life life;
            if (!_lives.TryGetValue(lifeId, out life))
            {
                // Do not evict paid lives and reopen an anti-farm token in a long session.
                if (_lives.Count >= TrackedLives) return 0f;
                life = new Life(); _lives.Add(lifeId, life);
            }
            float offered = 0f;
            if (melee && !life.VictoryPaid && now - life.LastHit >= HitInterval)
            {
                float part = Math.Min(counter ? CounterGain : MeleeGain, PerLifeMeleeCap - life.Harvested);
                offered += part; life.Harvested += part; life.LastHit = now;
            }
            if (killed && !life.VictoryPaid) { life.VictoryPaid = true; offered += VictoryGain; }
            float old = Value;
            Value = Sanitize(Value + offered);
            return Value - old;
        }
    }
}
