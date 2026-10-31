using System.Collections.Generic;
namespace NeonSyndicate
{
    public sealed partial class WildsChunkL1
    {
        private bool _authoredL2;
        public LiveWildsMetricsL2 MetricsL2 { get; private set; }
        public void ExclusionsL2(HashSet<int> target) { target.UnionWith(_excluded); }
        /// <summary>Rebuild appearance only. The original collider object and mesh are transferred unchanged.</summary>
        public void SetArtL2(bool authored)
        {
            if (_authoredL2 == authored) return;
            if (MetricsL2 == null || MetricsL2.Eligible == 0) { _authoredL2 = authored; return; }
            bool previous = _authoredL2;
            _authoredL2 = authored;
            try { Rebuild(new WildsBuilder.Result(), false); }
            catch (System.Exception e)
            {
                _authoredL2 = previous;
                Log.Warn("LIVE_WILDS appearance rebuild refused, previous geometry retained: " + e.Message);
            }
        }
    }
}
