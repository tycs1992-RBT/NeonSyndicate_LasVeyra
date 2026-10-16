using System;

namespace NeonSyndicate
{
    /// <summary>Checks the live service context again before calling its original transaction.</summary>
    public sealed class ServiceUseGate04C
    {
        private readonly Func<bool> _available;
        private readonly Action _use;
        private bool _invoking;

        public ServiceUseGate04C(Func<bool> available, Action use)
        {
            _available = available;
            _use = use;
        }

        public bool TryUse()
        {
            if (_invoking || _available == null || _use == null || !_available()) return false;
            _invoking = true;
            try { _use(); }
            finally { _invoking = false; }
            return true;
        }
    }
}
