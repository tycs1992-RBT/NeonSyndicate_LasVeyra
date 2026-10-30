// Pure zone-order checks against the real managed game assembly. This is not a Unity playtest.
using System;
using System.Collections;
using System.Reflection;

public static class ZoneHarnessWR02H
{
    public static int Main(string[] args)
    {
        try
        {
            Assembly game = Assembly.LoadFrom(args[0]);
            Type placement = game.GetType("NeonSyndicate.HeartCompositionPlacementWR02B", true);
            Type planner = game.GetType("NeonSyndicate.HeartPlanFR01", true);
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(placement);
            IList input = (IList)Activator.CreateInstance(listType);
            FieldInfo zone = placement.GetField("zone"), id = placement.GetField("stableId");
            foreach (string name in new[] { "home-1", "home-2", "home-3", "grotto-1", "grotto-2", "wood-1" })
            {
                object item = Activator.CreateInstance(placement);
                zone.SetValue(item, name.Split('-')[0]); id.SetValue(item, name); input.Add(item);
            }
            MethodInfo interleave = planner.GetMethod("InterleaveCompositionZonesWR02H");
            IList actual = (IList)interleave.Invoke(null, new object[] { input });
            string[] expected = { "home-1", "grotto-1", "wood-1", "home-2", "grotto-2", "home-3" };
            if (actual.Count != expected.Length) throw new Exception("Wrong count");
            for (int i = 0; i < expected.Length; i++)
                if ((string)id.GetValue(actual[i]) != expected[i]) throw new Exception("Wrong order at " + i);
            IList empty = (IList)interleave.Invoke(null, new object[] { null });
            if (empty.Count != 0 || input.Count != 6) throw new Exception("Null or input preservation failed");
            Console.WriteLine("8 zone ordering and preservation checks passed. Native Unity NOT RUN.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
