using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>Durable once-per-occurrence event receipts. Restoring a save never grants a reward.</summary>
    public sealed class CityRewardLedger04F
    {
        private readonly HashSet<string> _tokens = new HashSet<string>();
        public IEnumerable<string> Tokens { get { return _tokens; } }
        public int Count { get { return _tokens.Count; } }
        public bool TryClaim(string token)
        {
            return !string.IsNullOrEmpty(token) && _tokens.Add(token);
        }
        public void Clear() { _tokens.Clear(); }
        public void Restore(IEnumerable<string> tokens)
        {
            _tokens.Clear();
            if (tokens == null) return;
            foreach (string token in tokens) if (!string.IsNullOrEmpty(token)) _tokens.Add(token);
        }
    }
}
