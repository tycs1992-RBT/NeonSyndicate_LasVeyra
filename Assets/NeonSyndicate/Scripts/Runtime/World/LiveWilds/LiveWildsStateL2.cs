using System.Collections.Generic;
namespace NeonSyndicate
{
    /// <summary>Aggregation of actual chunk state, never an FPS or native acceptance claim.</summary>
    public static class LiveWildsStateL2
    {
        public static void ResetStatics() { }
        public static LiveWildsMetricsL2 Collect(IEnumerable<WildsChunkL1> chunks)
        {LiveWildsMetricsL2 sum=new LiveWildsMetricsL2();if(chunks!=null)foreach(WildsChunkL1 c in chunks)if(c!=null)sum.Add(c.MetricsL2);return sum;}
        public static string Summary(IEnumerable<WildsChunkL1> chunks,int pieces)
        {
            LiveWildsMetricsL2 sum=Collect(chunks);
            return "plan "+pieces+", Redspire candidates "+sum.Eligible+", replacements "+sum.Replaced+" (boulder "+sum.Boulders+", spire "+sum.Spires+", tree "+sum.Trees+"), renderers "+sum.Batches+", L1 exclusions "+sum.Excluded+". "+sum.Reason;
        }
    }
}
